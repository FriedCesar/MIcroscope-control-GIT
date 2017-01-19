using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Drawing;
using System.IO.Ports;
using System.Media;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text;
using System.Net;
using System.ComponentModel;


//********************* Move Instuction and response command set *********************
//
//      Data Protocol:
//
//      ___________________________________________________________________________
//      |      ||            ||         ||          ||      ||            ||      |
//      | 0XAA || Session ID || Command || Motor ID || DATA || Session ID || 0XAA |
//      |      ||            ||         ||          ||      ||            ||      |
//      """""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""""
//
//
//      
//          Header 2 bytes
//              1st byte: Start Identifier byte (0XAA = ¬)
//              2nd byte: ASCII Encoded session ID (0-127)
//
//          End 2 bytes
//              1st byte: ASCII Encoded session ID (0-127)
//              2nd byte: End Identifier byte (0XAA = ¬)
//
//          Motor ID
//              Identifier for main or auxiliar motor (@ or ~)
//
//          Command, Data and response
//              Command byte: Instruction
//              Data: Depending on function
//              Response: Motor ID + session ID + Received (Extra)
//
//              P: Movement request (Managed on MoveStage function)
//                  Data: 2 bytes
//                      1st byte: Position High byte
//                      2nd byte: Position Low byte
//                  Received: MF
//              I: Request board data (Step, cycle and time; saved on board's EEPROM)
//                  Received: IF
//                  Extra: 8 bytes
//                      1st byte: Step High byte
//                      2nd byte: Step Low byte
//                      3rd byte: Position High Byte
//                      4th byte: Position Low Byte
//                      5th byte: Time High Byte
//                      6th byte: Time Low Byte
//                      7st byte: Auxiliar Step High byte
//                      8nd byte: Auxiliar Step Low byte
//              O: Set origin request
//                  Received: OF
//              S: Movement backward cycle request (Managed on MoveStage function)
//                  Data: 2 bytes
//                      1st byte: Position High byte
//                      2nd byte: Position Low byte
//                  Received: SF
//                  *Extra: 1 byte
//                      *1st byte: Cycle
//              Z: Movement foward cycle request (Managed on MoveStage function)
//                  Data: 2 bytes
//                      1st byte: Position High byte
//                      2nd byte: Position Low byte
//                  Received: SF
//                  *Extra: 1 byte
//                      *1st byte: Cycle
//              V: Save memory request (Data on TextBoxes)
//                  Data: 8 bytes
//                      1st byte: Step High byte
//                      2nd byte: Step Low byte
//                      3rd byte: Position High Byte
//                      4th byte: Position Low Byte
//                      5th byte: Time High Byte
//                      6th byte: Time Low Byte
//                      7st byte: Auxiliar Step High byte
//                      8nd byte: Auxiliar Step Low byte
//                  Received: VF
//              Q: Stage movement speed 
//                  Data: 1 byte
//                      1st byte: Speed (8N encoding [Extended ASCII])
//                  Received: QF
//              U: Complete step selected
//                  Received: UF
//              W: uStep selected
//                  Received: WF
//              F: Forward direction selected
//                  Received: FF
//              R: Reverse direction selected
//                  Received: RF
//              A: Connect Motor
//                  Received: AF
//              D: Disconnect Motor
//                  Received: DF
//              L: Move Focus Servo
//                  Data: 1 byte
//                      1st byte: Position
//                  Received: LF                            
//***********************************************************************************

namespace Microscope_Control
{

    public delegate void InstructionHandler(object sender, InstructionEventArgs e);
    public class InstructionEventArgs : EventArgs
    {
        public string ConStat { get; set; }
        public string Motor { get; set; }
        public char ID { get; set; }
    }

    public class Board
    {
        public class StepMotor
        {
            public bool stepCheck = false;
            public char ID;
            public int Pos = 0;                                        // Position verifier
            public int PosRef = 0;                                     // Position reference
            public int Cycle = 0;
            public string StepVal;
            public string CycleVal;
            public string TimeVal;
        }

        public class ServoMotor
        {

        }


        //***********************************************************************
        Random rnd = new Random();                          // Random session iniciator
        public byte[] session;                                     // Byte session identifier
        public byte[] sessionRx;                                   // Byte session echo
        public byte[] byteRead = new byte[12];                     // Receiver byte manager
        public bool PortSel = false;                               // Retrieves information of board connection
        public bool ConSuc = false;                                // Succesful connection flag
        public bool Busy = false;                                  // Activity monitoring flag
        public byte[] TxString;                                    // Data transmision string (Send this)
        public string RxString;                                    // Data received string
        public int conTO = 0;                                      // Timeout connection by attempts

        public StepMotor MainMotor = new StepMotor();
        public StepMotor AuxMotor = new StepMotor();
        public SerialPort PortCOM = new SerialPort();

        private int errorcount = 0;        

        public event InstructionHandler Instruction;
        InstructionEventArgs args = new InstructionEventArgs();        
        protected virtual void OnInstruction(InstructionEventArgs e)
        {
            Instruction?.Invoke(this, e);
        }


        public Board()
        {
            PortCOM.DataReceived += PortCOM_DataReceived;
            MainMotor.ID = '@';
            AuxMotor.ID = '~';
        }

        private void SendData(byte[] message)
        {
            if (PortCOM.IsOpen)
            {
                byte[] start = new byte[] { Convert.ToByte('¬'), session[0] };
                byte[] tail = new byte[] { session[0], Convert.ToByte('¬') };
                byte[] sendthis = start.Concat(message).Concat(tail).ToArray();

                string mySession = Encoding.ASCII.GetString(session);
                TxString = sendthis;
                PortCOM.Write(sendthis, 0, sendthis.Length);
                ;
            }
        }

        public void StartSerial()
        {
            if (PortCOM.IsOpen)
                PortCOM.Close();

            session = new byte[] { Convert.ToByte(rnd.Next(1, 128)) };  // Generates a session number byte
            PortCOM.Open();                                             // Opens Port
            SendData(Encoding.ASCII.GetBytes("@COMREQU"));

        }

        public void StopSerial(object sender, EventArgs e)
        {
            SendData(Encoding.ASCII.GetBytes("@DISCONNECT"));                                                        // Send Disconnection request (board's led will blink three times)
            PortCOM.Dispose();
            PortCOM.Close();                                                                    // Close Port and reset flags
            ConSuc = false;
            PortSel = false;
            RxString = "";
            Busy = false;
            StatSender("disconnect",'¬');
        }

        private void Connect(object sender, EventArgs e)                                        // Manages on connection actions. This routine has been designed in order to avoid communnication errors (Tested on errors, the normal behavior should not have any)
        {
            if (RxString.Contains("COMSTART"))                                                      // on communication request, "COMSTART" is the identifier generated on the board. This instruction comes with an extra byte, session, which is used along the process to verify proper work.
            {
                sessionRx = new byte[] { Convert.ToByte(RxString.ElementAt(RxString.Length - 1)) }; // Extracts session byte from command
                if (BitConverter.ToString(sessionRx) == BitConverter.ToString(session))             // Compares session, if succesful, then connect
                {
                    ConSuc = true;
                    Activate(MainMotor);
                    StatSender("connected", '¬');
                }
            }

            if (RxString.Contains("DISCONNECT"))                                                    // Disconnect request received TODO: Check disconnection on error
            {
                StopSerial(sender, e);
            }

            if ((conTO < 100) & !ConSuc)                                                            // Manages connection timeout, if connection is not succesful, it will reinitiate connection protocol
            {
                PortCOM.DiscardInBuffer();
                PortCOM.DiscardOutBuffer();
                conTO += 1;
                PortSel = true;
                if (conTO == 100)                                                           // On timeout (100 attempts) display error
                {
                    conTO = 101;
                    StatSender("failed", '¬');
                    SendData(Encoding.ASCII.GetBytes("@COMERROR"));                                                 // Sends error request
                }
                else
                {
                    string mySession = Encoding.ASCII.GetString(session);
                    SendData(Encoding.ASCII.GetBytes("@COMREQU"));

                    StatSender("error" + conTO.ToString("D2"), '¬');

                }

            }
        }

        private void PortCOM_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Array.Resize(ref byteRead, PortCOM.BytesToRead);
            PortCOM.Read(byteRead, 0, PortCOM.BytesToRead);
            RxString = Encoding.UTF8.GetString(byteRead);
            if (RxString == "@#@")                                                              // Error message, sends last instruction
            {
                PortCOM.Write(TxString, 0, TxString.Length);
                Busy = false;
                errorcount += 1;
                if (errorcount == 10)
                {
                    Thread.Sleep(10);
                    errorcount = 0;
                    StatSender("insFailed", '¬');
                }
            }
            else
            {
                if (!ConSuc)                                                                            // Activates connection routine if no connection is stablished
                {
                    Connect(sender, e);
                }
                else
                {
                    if (RxString.Length >= 4)
                    {
                        if (RxString.Substring(0, 2) == ("@" + Encoding.ASCII.GetString(session)))
                            ComInstruction(ref MainMotor);                                           // Manages connection//deconnection error report
                        if (RxString.Substring(0, 2) == ("~" + Encoding.ASCII.GetString(session)))
                            ComInstruction(ref AuxMotor);
                    }
                    else
                    {
                        PortCOM.Write(TxString, 0, TxString.Length);
                        Busy = false;
                    }
                }
            }
            if (PortCOM.IsOpen)
                PortCOM.DiscardInBuffer();
        }

        public void ReqInfo()
        {
            Busy = true;
            SendData(Encoding.ASCII.GetBytes("I@"));
        }

        public void SaveData(string Step, string Cycle, string Time, string AuxStep)
        {
            Busy = true;
            byte[] byteStep = BitConverter.GetBytes(Convert.ToInt16(Step));
            byte[] byteCycle = BitConverter.GetBytes(Convert.ToInt16(Cycle));
            byte[] byteTime = BitConverter.GetBytes(Convert.ToInt16(Time));
            byte[] byteStepAux = BitConverter.GetBytes(Convert.ToInt16(AuxStep));
            byte[] sendthis = new byte[] { Convert.ToByte('V'), Convert.ToByte('@'), byteStep[0], byteStep[1], byteCycle[0], byteCycle[1], byteTime[0], byteTime[1], byteStepAux[0], byteStepAux[1], 0X0A };
            string message = Encoding.ASCII.GetString(sendthis);
            SendData(sendthis);
        }

        public void MoveStage(ref StepMotor thisMotor, int step, char type)
        {
            Busy = true;
            thisMotor.Pos = step;

            byte[] bytePos = BitConverter.GetBytes(step);
            byte[] sendthis = new byte[] { Convert.ToByte(type), Convert.ToByte(thisMotor.ID), bytePos[0], bytePos[1] };
            string message = Encoding.ASCII.GetString(sendthis);
            StatSender("Moving", thisMotor.ID);
            SendData(sendthis);
        }

        public void SetOrigin(ref StepMotor thisMotor)
        {
            Busy = true;                                                                            // Sets busy flag
            SendData(Encoding.ASCII.GetBytes("O" + thisMotor.ID));
        }

        public void ChangeSpeed(ref StepMotor thisMotor, int speed)
        {
            Busy = true;
            byte[] byteSpeed = BitConverter.GetBytes(speed);
            byte[] sendthis = new byte[] { Convert.ToByte('Q'), Convert.ToByte(thisMotor.ID), byteSpeed[0]};
            string message = Encoding.ASCII.GetString(sendthis);
            SendData(sendthis);
        }

        public void uStep(ref StepMotor thisMotor, bool Checked)
        {
            Busy = true;                                                                            // Sets busy flag
            thisMotor.stepCheck = Checked;
            if (thisMotor.stepCheck)
            {
                SendData(Encoding.ASCII.GetBytes("W" + thisMotor.ID));
            }
            else
            {
                SendData(Encoding.ASCII.GetBytes("U" + thisMotor.ID));
            }
        }

        public void ChangeDirection(ref StepMotor thisMotor, bool Checked)
        {
            Busy = true;                                                                            // Sets busy flag
            thisMotor.stepCheck = Checked;
            if (thisMotor.stepCheck)
            {
                SendData(Encoding.ASCII.GetBytes("R" + thisMotor.ID));
            }
            else
            {
                SendData(Encoding.ASCII.GetBytes("F" + thisMotor.ID));
            }
        }
        
        public void Activate(StepMotor thisMotor)
        {
            Busy = true;
            SendData(Encoding.ASCII.GetBytes("A" + thisMotor.ID));
        }

        public void Deactivate(StepMotor thisMotor)
        {
            Busy = true;
            SendData(Encoding.ASCII.GetBytes("D" + thisMotor.ID));
        }

        public void MoveServo(int position)
        {
            Busy = true;
            byte[] sendthis = new byte[] { Convert.ToByte('L'), Convert.ToByte('~'), Convert.ToByte(position) };
            string message = Encoding.ASCII.GetString(sendthis);
            SendData(sendthis);
        }

        private void StatSender(string status, char id)
        {
            args.ID = id;
            args.ConStat = status;
            if (id == '@')
                args.Motor = "Main";
            if (id == '~')
                args.Motor = "Auxiliar";
            OnInstruction(args);
        }
        
        private void ComInstruction(ref StepMotor myMotor)                                 // Manages received instructions from board (and actions on request)
        {
            bool receivedAction = false;
            string lookup = "";
            string command = "";
            if (RxString.Length >= 4)                                                               // Reads connection encoding and instruction
            {
                lookup = RxString.Substring(0, 2);
                command = RxString.Substring(2, 2);
            }
            switch (command)                                                                    // Reads command and checks action (or none)
            {
                case "MF":                                                                      // Move Finished (Answers to 'P' request)
                                                                                                //textBox1.Text = (Arduino.MainMotor.Pos + ", " + Arduino.MainMotor.PosRef);
                    receivedAction = true;
                    if (myMotor.Pos == myMotor.PosRef)                                                          // Check if position is up-to-date
                    {
                        Busy = false;
                        StatSender("MoveFinished", myMotor.ID);
                        break;
                    }
                    StatSender("MoveIncomplete", myMotor.ID);                                              // Sends movement request to board
                    break;
                case "IF":                                                                      // Information received
                    if (byteRead.Length < 12)
                    {
                        Busy = true;
                        receivedAction = false;
                        break;
                    }
                    receivedAction = true;
                    Busy = false;                                        // Decode and allocate data
                    MainMotor.StepVal = BitConverter.ToUInt16(byteRead, 4).ToString();
                    MainMotor.CycleVal = BitConverter.ToUInt16(byteRead, 6).ToString();
                    MainMotor.TimeVal = BitConverter.ToUInt16(byteRead, 8).ToString();
                    AuxMotor.StepVal = BitConverter.ToUInt16(byteRead, 10).ToString();
                    StatSender("DataInfo", myMotor.ID);
                    break;
                case "OF":                                                                      // Origin stablished
                    receivedAction = true;
                    Busy = false;
                    myMotor.Pos = 0;
                    myMotor.PosRef = 0;
                    myMotor.Cycle = 0;
                    StatSender("Origin", myMotor.ID);
                    break;
                case "SF":                                                                      // Cycle completed (Then sends board request for stablishing origin)
                    receivedAction = true;
                    Busy = false;
                    myMotor.Pos = 0;
                    myMotor.PosRef = 0;
                    StatSender("Cycle", myMotor.ID);
                    break;
                case "VF":                                                                      // Save completed
                    receivedAction = true;
                    Busy = false;
                    StatSender("DataSaved", myMotor.ID);
                    break;
                case "LF":
                    receivedAction = true;
                    Busy = false;
                    StatSender("ServoMoved", myMotor.ID);
                    break;
                case "AF":
                    receivedAction = true;
                    Busy = false;
                    StatSender("Activated", myMotor.ID);
                    break;
                case "RF":                                                                      // Completed reverse direction selection
                case "FF":                                                                      // Completed forward direction selection
                case "QF":                                                                      // Completed speed selection
                case "UF":                                                                      // Completed normal step selection
                case "WF":                                                                      // Completed uStep selection
                case "DF":
                    receivedAction = true;
                    Busy = false;
                    break;
                default:
                    break;
            }
            if (!receivedAction)                                                                    // If no correct response from board is received, send again board request
            {
                PortCOM.Write(TxString, 0, TxString.Length);
            }
        }


    }




}
