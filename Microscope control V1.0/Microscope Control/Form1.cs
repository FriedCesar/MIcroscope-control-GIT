////////////////*   Program for automatic movement control of a microscope (Stage) via step motor and image automatic capture (Using Sony DSC-QX10) */////////////
//
// Program for automatic movement control of a microscope (Stage) via step motor and image automatic capture (Using Sony DSC-QX10)
// 
// César Augusto Hernández Espitia
// ca.hernandez11@uniandes.edu.co
//
// V1.0      November/2016
// Program designed to be connected with an ARDUINO MEGA
// Designed to be connected with a Sony DSC-QX10 camera (It uses the Sony's Remote Camera API SDK; small changes can be implemented to extend range)
// Uses two step motors (Using A4988 MotorDriver)
// 
// 
// Notes:
//          Camera MUST be connected to PC before attempting to connect to the program (This program lacks a discovery device method for the camera)
//          Version still as a prototype, Be careful not to overload the programm with orders (Be gentle)
//          Bugs might be present, this program has not been thoughtfully tested
//          This program is designed to work altogether with an ARDUINO board, thus, the ARDUINO code for the used board is necessary
//          Unlike the predecessor, no IR shutter is anabled (YET)
//
//
// Camera Remote API by Sony
//
//
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Data;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace Microscope_Control
{
    public partial class Form1 : Form
    {


        // The following Variables are related to the camera behavior

        // Type definition of Camera related variables

        Camera SonyQX10 = new Camera();
        int i = 0;                                              // Multipropose counter
        string RootPath;                                        // Stores and manages the main folder
        string PicPath;                                         // Stores and manages the images folder
        private static object locker = new object();            // Locker, used to securely manage data (image stream for liveview)


        // The following Variables are related to the board managing and communication

        // Type definition of Stage related variables

        Board Arduino = new Board();
        Random rnd = new Random();                              // Random session iniciator
        byte[] session;                                         // Byte session identifier
        byte[] byteRead = new byte[12];                         // Receiver byte manager
        

        // The following Variables are related to the automated observation

        // Type definition of Automated observation

        bool Auto = false;                                      // Automated movement active flag
        bool BoardData = false;                                 // Data change flag
        bool Calibrated = false;                                // Calibration routine flag
        bool unmanaged = false;                                 // Unmanaged capture Flag
        bool onCapture = false;                                 // Active capture flag   
        bool onCalibration = false;                             // Active calibration flag        
        bool onMove = false;                                    // Movement finished flag
        bool onSave = false;                                    // Image saved flag
        int myFrame = 0;                                        // Frame counter
        int myImg = 0;                                          // Image counter
        int frameCount = 0;                                     // Frame verifier
        int TotalFrames;                                        // Frame number store variable
        int TotalTime;                                          // Time number store variable
        int[] Auxiliar;                                         // Auxiliar motor position array for calibrated routine
        new int[] Focus;                                            // Focus(Servo) motor position array for calibrated routine
        string request;
        string response;


        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;            // Allow cross thread calls
            ImgGuide.BackColor = Color.Transparent;             // Loads transparent color for image guide
            ImgGuide.Parent = ImgLiveview;                      // Sets liveviewimage as image guide parent
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            BConnectionCBox.Items.Add("Port selection");                                                    // Visualization for serial ports comboBox
            BConnectionCBox.SelectedIndex = 0;
            SonyQX10.ConnectEvent += SonyQX10_ConnectEvent;
            Arduino.Instruction += Arduino_Instruction;
            //************************* Create root folder path for file saving ***************************
            RootPath = ("C:\\Observation\\" + DateTime.Now.ToString("yyMMdd"));                             // Creates root storage file (C://observation//(Date)
            i = 1;
            while (Directory.Exists(RootPath))                                                              // Check requested directory exists, if so, an extra number is added
            {
                RootPath = ("C:\\Observation\\" + DateTime.Now.ToString("yyMMdd") + ("_") + i.ToString("D2"));
                i += 1;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)                   // On close, zoom camera out, disconnect board
        {
            if (SonyQX10.CamConStatus)
                SonyQX10.CamResponse = SonyQX10.SendRequest("actZoom", "\"out\",\"start\"");
            if (Arduino.PortCOM.IsOpen == true)
                Arduino.StopSerial(sender, e);
        }

        private void WriteReport(string WriteThis)                                              // Writes data to report file (on root file)
        {
            if (!Directory.Exists(RootPath))                                                            // Check requested directory exists, if not, creates it
            {
                DirectoryInfo di = Directory.CreateDirectory(RootPath);
            }
            if (!File.Exists(RootPath + "\\Report.txt"))
            {
                using (StreamWriter sw = File.CreateText(RootPath + "\\Report.txt"))
                {
                    sw.WriteLineAsync("OBSERVATION REPORT\r\n" + DateTime.Now.ToString("dddd MMMM dd yyyy, hh:mm:ss tt") + "\r\n");
                }
            }
            using (StreamWriter sw = File.AppendText(RootPath + "\\Report.txt"))
                sw.WriteLineAsync(WriteThis);

        }

        // The following code is (Mostly) related to the managing of the Camera
        //      TODO:

        private void ConnectBtn_Click(object sender, EventArgs e)                               // Manages the discovery routine to connect with camera DSC-QX10 (Must be connected to PC WiFi)
        {
            if (!SonyQX10.CamConStatus)
            {
                ConnectionTxt.Visible = true;
                ConnectBtn.Enabled = false;
                SonyQX10.Connect.RunWorkerAsync();                                              // Send action request to camera host to stop liveview
            }
            else
            {
                ConnectBtn.Enabled = false;
                if (SonyQX10.FlagLvw)
                {
                    ImgLiveview.Visible = false;
                    SonyQX10.FlagLvw = false;
                    SonyQX10.LiveView.CancelAsync();
                    SonyQX10.CamResponse = SonyQX10.SendRequest("stopLiveview", "");            // Send action request to camera host to stop liveview
                    guideChkBtn.Enabled = false;
                    SonyQX10.imgStream.Close();
                    SonyQX10.imgReader.Close();
                }
                //**************** Control Visualization *************
                StartBtn.Enabled = false;
                ManageChkBtn.Enabled = false;
                ConnectBtn.Text = ("Connect Camera");
                ConnectionTxt.Visible = false;
                ConnectionTxt.Text = "";
                foreach (Control control in CameraPanel.Controls)
                {
                    control.Enabled = false;
                    if (control is CheckBox)
                    {
                        if (control.Name == "resolutionChkBtn")
                            ((CheckBox)control).Checked = true;
                        else
                            ((CheckBox)control).Checked = false;
                    }

                }
                SonyQX10.Disconnect.RunWorkerAsync();
            }
        }

        private void LiveviewBtn_Click(object sender, EventArgs e)                              // Manages beginning/end of liveview
        {
            if (!SonyQX10.FlagLvw)
            {
                //************************ Start liveview Background Worker (Send HTTP GET request, calls Liveview Background worker)
                SonyQX10.FlagLvw = true;
                ImgLiveview.Visible = true;
                SonyQX10.CamResponse = SonyQX10.SendRequest("startLiveview", "");               // Send action request to camera host to start liveview
                SonyQX10.lvwURL = SonyQX10.ReadRequestJson(SonyQX10.CamResponse, 0);            // Setup the URL for the liveview download
                WebRequest lvwRequest = WebRequest.Create(SonyQX10.lvwURL);                     // Create a request using the camera liveview URL, send HTTP GET request
                lvwRequest.Method = "GET";
                lvwRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                SonyQX10.imgStream = lvwRequest.GetResponse().GetResponseStream();              // Setup and get the request stream response
                SonyQX10.imgReader = new StreamReader(SonyQX10.imgStream);
                if (SonyQX10.LiveView.IsBusy != true)
                    SonyQX10.LiveView.RunWorkerAsync();
                guideChkBtn.Enabled = true;
            }
            else
            {
                ImgLiveview.Visible = false;
                SonyQX10.FlagLvw = false;
                SonyQX10.LiveView.CancelAsync();
                SonyQX10.CamResponse = SonyQX10.SendRequest("stopLiveview", "");                // Send action request to camera host to stop liveview
                guideChkBtn.Enabled = false;
                SonyQX10.imgStream.Close();
                SonyQX10.imgReader.Close();
            }
        }

        private void BShutterBtn_Click(object sender, EventArgs e)                              // Starts a shutter routine, calls Background Worker
        {
            if (SonyQX10.imgCount == 0)
            {
                if (!Directory.Exists(RootPath))                                                // Check requested directory exists, if not, creates it
                {
                    DirectoryInfo di = Directory.CreateDirectory(RootPath);
                }
            }

            SonyQX10.imgCount += 1;
            SonyQX10.SavePath = RootPath;
            SonyQX10.SaveName = ("P" + SonyQX10.imgCount.ToString("D4") + ".jpg");
            BShutterBtn.Enabled = false;
            BStateLbl.Text = "Taking picture...";
            SonyQX10.TakePicture.RunWorkerAsync();
            WriteReport("\r\nPicture " + SonyQX10.SaveName + "\r\nPicture shot at " + DateTime.Now.ToString("hh:mm:ss tt"));
        }

        private void guideRefreshBtn_Click(object sender, EventArgs e)                          // Loads image from live view to be frozen and displayed as a guide frame
        {
            ImgGuide.Location = new Point(0, 0);                                                // Ensures the reference image frame is in place
            Bitmap referenceImg;
            lock (locker)                                                                       // Calls lock on objects (necessary for avoiding issues on image load)
                referenceImg = new Bitmap(ImgLiveview.Image);
            TImage(referenceImg, 60);
        }

        private void guideChkBtn_CheckedChanged(object sender, EventArgs e)                     // Visualize frozen image to use it as a guide in liveview
        {
            if (guideChkBtn.Checked)
            {
                ImgGuide.Visible = true;
                guideRefreshBtn.Enabled = true;
            }
            else
            {
                ImgGuide.Visible = false;
                guideRefreshBtn.Enabled = false;
            }

        }

        private void resolutionChkBtn_CheckedChanged(object sender, EventArgs e)                // Changes generated image resolution
        {
            if (resolutionChkBtn.Checked)
            {
                SonyQX10.CamResponse = SonyQX10.SendRequest("setPostviewImageSize", "\"Original\"");
            }
            else
            {
                SonyQX10.CamResponse = SonyQX10.SendRequest("setPostviewImageSize", "\"2M\"");
            }
        }

        private void HPShutterChkBtn_CheckedChanged(object sender, EventArgs e)                 // Activates Half-Press Shutter action
        {
            if (HPShutterChkBtn.Checked)
            {
                SonyQX10.CamResponse = SonyQX10.SendRequest("actHalfPressShutter");
            }
            else
            {
                SonyQX10.CamResponse = SonyQX10.SendRequest("cancelHalfPressShutter");
            }
        }

        private void CommentBtn_Click(object sender, EventArgs e)                               // Adds an anytime comment on Report
        {
            string comment = PromptDialog.ShowDialog("Please type your comment:", "");
            WriteReport("\r\nComment (" + DateTime.Now.ToString("hh:mm:ss tt") + "): " + comment);
        }

        private void TImage(Bitmap referenceImg, int opacity)                                   // Loads a transparent image on the image guide picturebox
        {

            Bitmap transparentImg = new Bitmap(referenceImg.Width, referenceImg.Height);        // Aquires Image from Liveview
            Graphics tempG = Graphics.FromImage(referenceImg);
            Color c = Color.Transparent;
            Color v = Color.Transparent;
            for (int x = 0; x < referenceImg.Width; x++)                                        // Sweeps image pixels to change opacity
            {
                for (int y = 0; y < referenceImg.Height; y++)
                {
                    c = referenceImg.GetPixel(x, y);
                    v = Color.FromArgb(opacity, c.R, c.G, c.B);
                    transparentImg.SetPixel(x, y, v);
                }
            }
            tempG.DrawImage(transparentImg, Point.Empty);                                       // Loads Tranparent(ed) image on ImgGuide
            ImgGuide.Image = transparentImg;
        }

        void SonyQX10_ConnectEvent(object sender, ConnectEventArgs e)                           // Manages the events on cammera response
        {
            switch (e.ConnectionState)
            {
                case "UDP":
                    ConnectionTxt.Text = ("Status\r\n\r\nUDP-Socket setup finished...\r\n");
                    ;
                    break;
                case "MSEARCH":
                    ConnectionTxt.AppendText("M-Search sent\r\n");
                    break;
                case "WAIT":
                    ConnectionTxt.AppendText("█");
                    break;
                case "CONNECTED":
                    ConnectionTxt.AppendText("\r\nConnection established\n");
                    break;
                case "SUCCESS":
                    ConnectionTxt.AppendText("\r\n\rConnection successful =)  \n");
                    getEventTxt.Text = SonyQX10.CamResponse;
                    //**************** Loads transparent logo to guide image (Done here to serve as a sleep function)
                    TImage(new Bitmap(ImgLogo.Image), 13);
                    ImgGuide.Location = new Point(0, 0);
                    //**************** Control Visualization *************
                    ConnectionTxt.Text = "";
                    ConnectionTxt.Visible = false;
                    foreach (Control control in CameraPanel.Controls)
                    {
                        if ((control.Name != "guideChkBtn") & (control.Name != "guideRefreshBtn"))
                            control.Enabled = true;
                        else
                            control.Enabled = false;
                    }
                    ConnectBtn.Text = ("Disconnect Camera");
                    if (Arduino.ConSuc)
                    {
                        StartBtn.Enabled = true;
                        ManageChkBtn.Enabled = true;
                    }
                    break;
                case "NOTCONNECTED":
                    ConnectBtn.Enabled = true;
                    ConnectionTxt.AppendText("\r\n\r\nFailed: Connection TimedOut =(  \n");
                    break;
                case "DISCONNECT":
                    ConnectBtn.Enabled = true;
                    break;
                case "LIVEVIEW":
                    if (SonyQX10.bmpImage != null)
                    {
                        lock (locker)
                            ImgLiveview.Image = SonyQX10.bmpImage;
                    }
                    break;
                case "PICTURE":
                    BStateLbl.Text = ("Saving picture\r\n" + SonyQX10.SavePath + "\\" + SonyQX10.SaveName);
                    if (Auto & onCapture)
                    {
                        if (myFrame == TotalFrames)                                                             // Frames, completed
                        {
                            onMove = true;
                            frameCount = TotalFrames;
                            Invoke(new EventHandler(ManageFrames));
                            break;
                        }
                        else
                if (calibrationChkBtn.Checked)
                            CalibratedAutomation("capture", "move");
                        else
                            Automation("capture", "move");
                    }

                    break;
                case "IMGSAVED":
                    BStateLbl.Text = ("Image saved.");
                    if (!Auto)
                    {
                        BShutterBtn.Enabled = true;
                        if (NoteChkBtn.Checked)
                        {
                            string comment = PromptDialog.ShowDialog("Please type the comments on this picture:", "");
                            WriteReport("Note: " + comment);
                        }
                    }
                    else
                    {
                        onSave = true;
                        Invoke(new EventHandler(ManageFrames));
                    }
                    break;

                default:
                    ConnectionTxt.AppendText(e.ConnectionState);
                    break;
            }
        }

        
        // These events are provided for test purposes only Any release: please leave these as NOT AVAILABLE (Or not visible)
        //      TODO:


        private void getEventBtn_Click(object sender, EventArgs e)                              // Test Button (Not visible) Requests Events to camera
        {
            //*************** Shows JSON as string ******************************************
            //string json = SonyQX10.SendRequest("getEvent", "false", "1.1");
            ////var myjson = JsonConvert.DeserializeObject<string>(json);//JsonConvert.DeserializeObject<RootGetEvent>(json);
            //textBox1.Text = (json);
            ////************* Reads JSON format (Returns camera status) *********************
            string json = SonyQX10.SendRequest("getEvent", "false", "1.1");
            var myjson = JsonConvert.DeserializeObject<Camera.RootGetEvent>(json);
            textBox1.Text = ("");
            ////************* Visualizes JSON response **************************************
            for (i = 0; i < myjson.result.Count; i++)
            {
                if ((myjson.result[i]) != null)
                {
                    textBox1.AppendText("\r\n" + i + ")\r\n");
                    textBox1.AppendText(myjson.result[i].ToString());
                }
            }
            //************* Visualizes Camera status **************************************
            textBox2.Text = SonyQX10.ReadRequestJson(json, 10, 0, "numberOfRecordableImages"); //myjson.result[1]["cameraStatus"].ToString();
        }

        private void TestBtn_Click(object sender, EventArgs e)                                  // Test Button (Not available) <INSERT YOUR TEST CODE HERE>
        {
            //************ Take picture ************************************
            //string CamResponse = SonyQX10.SendRequest("actTakePicture", "");

            //************ Check received info from board ******************
            //BStateLbl.Text = RxString;

            //************ Available functions *****************************
            //string json = SonyQX10.SendRequest("getAvailableApiList", " ", "1.0");
            //textBox1.Text = json;

            //************ Storage information *****************************
            //string json = SonyQX10.SendRequest("getStorageInformation", " ", "1.0");
            //textBox1.Text = json;

            //************ Zoom information ********************************
            //string json = SonyQX10.SendRequest("getEvent", "false", "1.1");
            //textBox2.Text = Convert.ToInt32(SonyQX10.ReadRequestJson(json, 2, "zoomPosition")).ToString();
            //************ Promt Dialog ************************************
            //string Prompt = PromptDialog.ShowDialog("Hello","There");
            //textBox2.Text = Prompt;


        }

        private void pictureBox3_LoadCompleted(object sender, AsyncCompletedEventArgs e)        // Test image, load finished (Not loaded)
        {
            //if (OnCapture)
            //{
            //    BStateLbl.Text = (BStateLbl.Text + ("\nImage capture finished"));
            //    onSave = true;
            //    ManageFrames();
            //}
        }

        private void LiveviewTmr_Tick(object sender, EventArgs e)                               // (Replaced by Background Worker) Timer, refreshes liveview image
        {
            //{
            //    using (var memstream = new MemoryStream())
            //    {
            //        imgData = new List<byte>();
            //        buffer = new byte[520];
            //        bufferAux = new byte[4];
            //        payloadType = 0;
            //        imgSize = 0;
            //        frameNo = -1;
            //        paddingSize = 0;

            //        GetHeader:                                                          // Retrieves a byte(s) from the stream to check if it corresponds to Sony header construction

            //        // Common Header (8 Bytes)
            //        //buffer = new byte[520];
            //        imgReader.BaseStream.Read(buffer, 0, 1);                            // Seeks for start byte
            //        var start = buffer[0];
            //        if (start != 0xff)
            //            goto GetHeader;

            //        //buffer = new byte[520];
            //        imgReader.BaseStream.Read(buffer, 0, 1);                            // Stores payload Type
            //        payloadType = (buffer[0]);
            //        if (!((payloadType == 1) || (payloadType == 2)))
            //            goto GetHeader;

            //        //buffer = new byte[520];
            //        imgReader.BaseStream.Read(buffer, 0, 2);                            // Stores Frame Number depending Payload type
            //        if (payloadType == 1)
            //            frameNo = BitConverter.ToUInt16(buffer, 0);

            //        imgReader.BaseStream.Read(buffer, 0, 4);                            // Discards expected Time stamp

            //        // Payload header (128 bytes)
            //        //buffer = new byte[520];
            //        imgReader.BaseStream.Read(buffer, 0, 4);
            //        if (!((buffer[0] == 0x24) & (buffer[1] == 0x35) & (buffer[2] == 0x68) & (buffer[3] == 0x79)))
            //            goto GetHeader;                                                 // If the start code does not correspond to fixed code (0x24, 0x35, 0x68, 0x79), starts over

            //        //bufferAux = new byte[4];
            //        imgReader.BaseStream.Read(bufferAux, 0, 4);
            //        paddingSize = bufferAux[3];
            //        bufferAux[3] = bufferAux[2];
            //        bufferAux[2] = bufferAux[1];
            //        bufferAux[1] = bufferAux[0];
            //        bufferAux[0] = 0;
            //        Array.Reverse(bufferAux);
            //        imgSize = BitConverter.ToInt32(bufferAux, 0);                       // Reads and translates Data stream size

            //        if (payloadType == 1)                                               // Case JPEG data
            //        {
            //            imgReader.BaseStream.Read(buffer, 0, 120);
            //            while (imgData.Count < imgSize)
            //            {
            //                //buffer = new byte[520];
            //                imgReader.BaseStream.Read(buffer, 0, 1);
            //                imgData.Add(buffer[0]);
            //            }
            //        }

            //        //getEventTxt.AppendText("Image size: " + imgData.Count.ToString());
            //        MemoryStream stream = new MemoryStream(imgData.ToArray());
            //        BinaryReader reader = new BinaryReader(stream);
            //        Bitmap bmpImage = (Bitmap)Image.FromStream(stream);

            //        if (ImgLiveview.Image != null)
            //            ImgLiveview.Image.Dispose();

            //        ImgLiveview.Image = bmpImage;
            //    }
            //}
        }

        
        // The following code is (Mostly) related to the managing of the Board
        //              TODO: 

            
        private void comboBox1_DropDown(object sender, EventArgs e)                             // Sniffs for serial ports connected to the computer (Arduino connects vias Serial)
        {
            if (!Arduino.PortCOM.IsOpen)
            {
                string[] ports = SerialPort.GetPortNames();                                     // Sniffs for connected ports
                BConnectionCBox.Items.Clear();                                                  // Cleans previous data in Combobox
                BConnectionCBox.Items.Add("Port selection");
                BConnectionCBox.SelectedIndex = 0;
                foreach (string port in ports)                                                  // Adds available ports to the Combobox's list
                {
                    BConnectionCBox.Items.Add(port);
                }
            }
        }

        private void BConnectionCBox_SelectedIndexChanged(object sender, EventArgs e)           // Enables connection button on serial type port selection (i.e. if a serial port is selecten in the combo box)
        {
            if (!Arduino.PortCOM.IsOpen)
            {
                if (BConnectionCBox.Text.Contains("COM"))
                {
                    BConnectBtn.Enabled = true;
                    Arduino.PortCOM.PortName = BConnectionCBox.Text;
                    Arduino.PortCOM.BaudRate = 115200;
                }
                else
                {
                    BConnectBtn.Enabled = false;
                    Arduino.PortCOM.PortName = " ";
                }
            }
        }

        private void BConnectionCBox_TextUpdate(object sender, EventArgs e)
        {
            if ((BConnectionCBox.Text == "RESET") && (Arduino.PortCOM.IsOpen))
            {
                Arduino.StopSerial(sender, e);
                BConnectionCBox.Items.Clear();                                                          // Cleans previous data in Combobox
                BConnectionCBox.Items.Add("Port selection");
                BConnectionCBox.SelectedIndex = 0;
            }
        }

        private void BConnectBtn_Click(object sender, EventArgs e)                              // Starts connection routine (No error handling)
        {
            Arduino.conTO = 0;
            BStateLbl.Text = ("Status");
            if (Arduino.PortCOM.PortName.Contains("COM"))                                       // Allows action if a valid COM port is connected/selected
            {
                if (!Arduino.PortCOM.IsOpen)                                                    // If port is closed, and a valid serial port is selected, allow connection
                {
                    Arduino.StartSerial();
                    BConnectBtn.Enabled = false;
                    session = Arduino.session;
                    // Used to monitor the COMREQU command
                    //
                    //getEventTxt.Text = Arduino.TxString;
                    //getEventTxt.AppendText(BitConverter.ToString(Arduino.session));
                }
                else                                                                            // If port is open, close port (Manages controller labels)
                {
                    Arduino.StopSerial(sender, e);
                }
            }
        }

        private void BSaveBtn_Click(object sender, EventArgs e)                                 // Saves data on calibration
        {
            Arduino.SaveData(BStepTxt.Text, BCycleTxt.Text, BTimeTxt.Text, BAStepTxt.Text);
        }

        private void BStepTB_Scroll(object sender, EventArgs e)
        {
            Arduino.MainMotor.PosRef = BStepTB.Value;                                                                 // Stores user position of the Trackbar, this is the position reference to verify the stage movement
            textBox2.Text = BStepTB.Value.ToString();
            if (!Arduino.Busy)                                                                              // Send data in execution timeif busy flag is false (When position is not fully attained, the program will check board reported position and stored position and send the difference)
            {
                Arduino.Busy = true;                                                                        // Sets busy flag
                BStepTBLbl.Text = ("Step: " + Arduino.MainMotor.PosRef);                                              // Update position on visualization
                Arduino.MainMotor.Pos = BStepTB.Value;
                Arduino.MoveStage(ref Arduino.MainMotor, BStepTB.Value, 'P');                                                               // Request stage movement (managed by MoveStage function)
            }
        }

        private void BStepMinBtn_Click(object sender, EventArgs e)                              // Sends board request and sets current Trackbar position as Origin
        {
            Arduino.SetOrigin(ref Arduino.MainMotor);                                                       // Send Reset position board request
        }

        private void BStepMaxBtn_Click(object sender, EventArgs e)                              // Sets current Trackbar position as Max Step position
        {
            BStepTB.Maximum = BStepTB.Value;                                                        // Retrieves Trackbar current position
            BStepMaxLbl.Text = ("Max: " + BStepTB.Maximum);                                         // Updates position visualization
            BStateLbl.Text = ("Main motor maximum position\nSET");
        }

        private void BStepMax1Btn_Click(object sender, EventArgs e)                             // Diminishes step Max step on Trackbar
        {
            if (BStepTB.Maximum > 0)                                                        // No negative Value is accepted
            {
                if (BStepTB.Maximum == BStepTB.Value)                                       // In case the scroll value is at maximum value, then move scroll accordingly
                {
                    BStepTB.Value = BStepTB.Value - 1;                                      // Diminishes scroll value
                    BStepTB_Scroll(sender, e);                                              // NOT THE BEST PRACTICE: It calls the scroll "Scroll" action (Moves the motor and adjusts the form objects)
                }
                BStepTB.Maximum = BStepTB.Maximum - 1;                                      // Retrieves Trackbar current position
                BStepMaxLbl.Text = ("Max: " + BStepTB.Maximum);                             // Updates position visualization
                BStateLbl.Text = ("Main motor maximum position\nCHANGED");
            }
            else
            {
                BStepMax1Btn.Enabled = false;
            }
        }

        private void BStepMax2Btn_Click(object sender, EventArgs e)                             // Allows bigger step Max step on Trackbar
        {
            BStepMax1Btn.Enabled = true;
            BStepTB.Maximum = BStepTB.Maximum + 1;
            BStepMaxLbl.Text = ("Max: " + BStepTB.Maximum);
            BStateLbl.Text = ("Main motor maximum position\nSET");
        }

        private void BCycle1Btn_Click(object sender, EventArgs e)                               // Sends board request to move a complete cycle (Backwards)
        {
            if (!Arduino.Busy)
            {
                Arduino.Busy = true;                                                                        // Sets busy flag
                if (BCycleCountLbl.Text == "1")                                                     // Allow only positive movement
                {
                    BCycle1Btn.Enabled = false;
                }
                Arduino.MainMotor.Cycle -= 1;
                //Arduino.MainMotor.PosRef = Convert.ToInt32(BStepTxt.Text);
                BCycleCountLbl.Text = Arduino.MainMotor.Cycle.ToString();
                Arduino.MoveStage(ref Arduino.MainMotor, Convert.ToInt32(BStepTxt.Text), 'S');                                     // Request cycle movement though MoveStage function
            }
        }

        private void BCycle2Btn_Click(object sender, EventArgs e)                               // Sends board request to move a complete cycle (Foward)
        {
            if (!Arduino.Busy)
            {
                Arduino.Busy = true;                                                                        // Sets busy flag
                if (BCycleCountLbl.Text == "0")                                                     // Enables for positive movement
                {
                    BCycle1Btn.Enabled = true;
                }
                Arduino.MainMotor.Cycle += 1;
                //Arduino.MainMotor.PosRef = Convert.ToInt32(BStepTxt.Text);
                BCycleCountLbl.Text = Arduino.MainMotor.Cycle.ToString();
                Arduino.MoveStage(ref Arduino.MainMotor, Convert.ToInt32(BStepTxt.Text), 'Z');                                     // Request cycle movement though MoveStage function
            }
        }

        private void BCycleSetBtn_Click(object sender, EventArgs e)                             // Updates step setup to current step count
        {
            BCycleTxt.Text = BCycleCountLbl.Text;
            BStateLbl.Text = ("Main motor cycle\nSET");
        }

        private void BStepSetBtn_Click(object sender, EventArgs e)                              // Updates step setup to current Trackbar position
        {
            BStateLbl.Text = ("Main motor number of steps\nSET");
            BStepTxt.Text = BStepTB.Value.ToString();
        }

        private void BSpeedTB_Scroll(object sender, EventArgs e)                                // Sends board request for changing stage moving speed on execution time
        {
            if (!Arduino.Busy)
            {
                Arduino.ChangeSpeed(ref Arduino.MainMotor, BSpeedTB.Value);
            }
        }

        private void uStepChkBtn_CheckedChanged(object sender, EventArgs e)                     // Sends board request for uStep activation (Format and sends request depending case activation/deactivation)
        {
            Arduino.uStep(ref Arduino.MainMotor, uStepChkBtn.Checked);
            //if (ConSuc)
            //{
            //    Busy = true;
            //    if (uStepChkBtn.Checked)
            //    {
            //        TxString = ("@" + Encoding.ASCII.GetString(session) + "W");
            //    }
            //    else
            //    {
            //        TxString = ("@" + Encoding.ASCII.GetString(session) + "U");
            //    }
            //    serialPort1.WriteLine(TxString);
            //}
        }

        private void reverseChkBtn_CheckedChanged(object sender, EventArgs e)                   // Sends board request for reverse activation (Format and sends request depending case forward/backwards)
        {
            Arduino.ChangeDirection(ref Arduino.MainMotor, reverseChkBtn.Checked);
            //if (ConSuc)
            //{
            //    Busy = true;
            //    if (reverseChkBtn.Checked)
            //    {
            //        TxString = ("@" + Encoding.ASCII.GetString(session) + "R");
            //    }
            //    else
            //    {
            //        TxString = ("@" + Encoding.ASCII.GetString(session) + "F");
            //    }
            //    serialPort1.WriteLine(TxString);
            //}
        }


        //***************** Auxiliary motor ******************************
        //              TODO: Organize comments


        private void BMAuxChkBtn_CheckedChanged(object sender, EventArgs e)                     // Toggles activation state of the Auxiliary motor
        {
            if (Arduino.ConSuc)
            {
                if (BMAuxChkBtn.Checked)
                {
                    Arduino.Activate(Arduino.AuxMotor);
                    BoardAuxPanel.Visible = true;
                    BoardAuxPanel.Enabled = true;
                    CalibrationBtn.Enabled = true;
                    //foreach (Control control in BoardAuxPanel.Controls)
                    //{
                    //    control.Enabled = true;
                    //}
                }
                else
                {
                    Arduino.Deactivate(Arduino.AuxMotor);
                    BoardAuxPanel.Visible = false;
                    BoardAuxPanel.Enabled = false;
                    CalibrationBtn.Enabled = false;
                    Arduino.AuxMotor.Cycle = 0;
                    BASpeedTB.Value = 3;                                                                     // Manages form layout (Disable microscope control buttons) TODO: Find a more ellegant way to do this
                    BAStepTB.Value = 0;
                    BAStepTB.Maximum = 100;
                    BAStepTBLbl.Text = ("Step:");
                    BACycleCountLbl.Text = ("0");

                    //foreach (Control control in BoardAuxPanel.Controls)
                    //{
                    //    control.Enabled = false;
                    //}
                }
            }
        }

        private void BAStepTB_Scroll(object sender, EventArgs e)
        {
            Arduino.AuxMotor.PosRef = BAStepTB.Value;                                                                 // Stores user position of the Trackbar, this is the position reference to verify the stage movement
            textBox2.Text = BAStepTB.Value.ToString();
            if (!Arduino.Busy)                                                                              // Send data in execution timeif busy flag is false (When position is not fully attained, the program will check board reported position and stored position and send the difference)
            {
                Arduino.Busy = true;                                                                        // Sets busy flag
                BAStepTBLbl.Text = ("Step: " + Arduino.AuxMotor.PosRef);                                              // Update position on visualization
                Arduino.AuxMotor.Pos = BAStepTB.Value;
                Arduino.MoveStage(ref Arduino.AuxMotor, BAStepTB.Value, 'P');                                                               // Request stage movement (managed by MoveStage function)
            }
        }

        private void BAStepMinBtn_Click(object sender, EventArgs e)                              // Sends board request and sets current Trackbar position as Origin
        {
            Arduino.SetOrigin(ref Arduino.AuxMotor);                                                      // Send Reset position board request
        }

        private void BAStepMaxBtn_Click(object sender, EventArgs e)                              // Sets current Trackbar position as Max Step position
        {
            BAStepTB.Maximum = BAStepTB.Value;                                                        // Retrieves Trackbar current position
            BAStepMaxLbl.Text = ("Max: " + BAStepTB.Maximum);                                         // Updates position visualization
            BStateLbl.Text = ("Auxiliar motor maximum position\nSET");                                      // Updates position visualization
        }

        private void BAStepMax1Btn_Click(object sender, EventArgs e)                             // Diminishes step Max step on Trackbar
        {
            if (BAStepTB.Maximum > 0)                                                        // No negative Value is accepted
            {
                if (BAStepTB.Maximum == BAStepTB.Value)                                       // In case the scroll value is at maximum value, then move scroll accordingly
                {
                    BAStepTB.Value = BAStepTB.Value - 1;                                      // Diminishes scroll value
                    BAStepTB_Scroll(sender, e);                                              // NOT THE BEST PRACTICE: It calls the scroll "Scroll" action (Moves the motor and adjusts the form objects)
                }
                BAStepTB.Maximum = BAStepTB.Maximum - 1;                                      // Retrieves Trackbar current position
                BAStepMaxLbl.Text = ("Max: " + BStepTB.Maximum);                             // Updates position visualization
                BStateLbl.Text = ("Auxiliar motor maximum position\nCHANGED");
            }
            else
            {
                BAStepMax1Btn.Enabled = false;
            }
        }

        private void BAStepMax2Btn_Click(object sender, EventArgs e)                             // Allows bigger step Max step on Trackbar
        {
            BAStepMax1Btn.Enabled = true;
            BAStepTB.Maximum = BAStepTB.Maximum + 1;
            BAStepMaxLbl.Text = ("Max: " + BAStepTB.Maximum);
            BStateLbl.Text = ("Auxiliar motor maximum position\nSET");
        }

        private void BACycle1Btn_Click(object sender, EventArgs e)                               // Sends board request to move a complete cycle (Backwards)
        {
            if (!Arduino.Busy)
            {
                Arduino.Busy = true;                                                                        // Sets busy flag
                if (BACycleCountLbl.Text == "1")                                                     // Allow only positive movement
                {
                    BACycle1Btn.Enabled = false;
                }
                Arduino.AuxMotor.Cycle -= 1;
                BACycleCountLbl.Text = Arduino.AuxMotor.Cycle.ToString();
                Arduino.MoveStage(ref Arduino.AuxMotor, Convert.ToInt32(BAStepTxt.Text), 'S');                                     // Request cycle movement though MoveStage function
            }
        }

        private void BACycle2Btn_Click(object sender, EventArgs e)                               // Sends board request to move a complete cycle (Foward)
        {
            if (!Arduino.Busy)
            {
                Arduino.Busy = true;                                                                        // Sets busy flag
                if (BACycleCountLbl.Text == "0")                                                     // Enables for positive movement
                {
                    BACycle1Btn.Enabled = true;
                }
                Arduino.AuxMotor.Cycle += 1;
                BACycleCountLbl.Text = Arduino.AuxMotor.Cycle.ToString();
                Arduino.MoveStage(ref Arduino.AuxMotor, Convert.ToInt32(BAStepTxt.Text), 'Z');                                     // Request cycle movement though MoveStage function
            }
        }

        private void BAStepSetBtn_Click(object sender, EventArgs e)                              // Updates step setup to current Trackbar position
        {
            BStateLbl.Text = ("Auxiliar motor number of steps\nSET");
            BAStepTxt.Text = BAStepTB.Value.ToString();
        }

        private void BASpeedTB_Scroll(object sender, EventArgs e)                                // Sends board request for changing stage moving speed on execution time
        {
            if (!Arduino.Busy)
            {
                Arduino.ChangeSpeed(ref Arduino.AuxMotor, BASpeedTB.Value);
            }
        }

        private void AuStepChkBtn_CheckedChanged(object sender, EventArgs e)                     // Sends board request for uStep activation (Format and sends request depending case activation/deactivation)
        {
            Arduino.uStep(ref Arduino.AuxMotor, AuStepChkBtn.Checked);
        }

        private void AreverseChkBtn_CheckedChanged(object sender, EventArgs e)                   // Sends board request for reverse activation (Format and sends request depending case forward/backwards)
        {
            Arduino.ChangeDirection(ref Arduino.AuxMotor, AreverseChkBtn.Checked);
        }



        private void Arduino_Instruction(object sender, InstructionEventArgs e)                 // Received the processed information from the boardand deploys action
        {
            //Used to monitor the processed Communication request from board
            //
            //textBox1.Text = textBox1.Text + "\r\nConStat: " + e.ConStat + "\r\nTxInst: " + Arduino.TxString + "\r\nRxInst: " + Arduino.RxString + "\r\n";

            response = ("escape");
            if (e.ConStat.Contains("error"))
            {
                string attempt = e.ConStat.Substring(5, (e.ConStat.Length - 5));
                BStateLbl.Text = ("Status\nAttempts: " + attempt);
                getEventTxt.Text = BitConverter.ToString(Arduino.TxString);
                getEventTxt.AppendText(BitConverter.ToString(Arduino.session));
            }
            switch (e.ConStat)
            {
                case "insFailed":
                    BStateLbl.Text = "Instruction error";
                    break;
                case "connected":
                    BStateLbl.Text = (BStateLbl.Text + "\nSession ID: " + BitConverter.ToString(Arduino.sessionRx));
                    Invoke(new EventHandler(Connected));
                    Arduino.ReqInfo();
                    break;
                case "failed":
                    BStateLbl.Text = (BStateLbl.Text + "\nConnection Failed.\nTry to reconnect to the board...");
                    break;
                case "disconnect":
                    Invoke(new EventHandler(Disconnected));
                    break;
                case "Moving":
                    BStateLbl.Text = ("Moving " + e.Motor + " motor...");
                    break;
                case "MoveFinished":
                    BStateLbl.Text += ("\nMove Finished");
                    if (calibrationChkBtn.Checked)
                        response = "next";
                    break;
                case "MoveIncomplete":
                    if (e.ID == '@')
                    {
                        BStepTBLbl.Text = ("Step: " + Arduino.MainMotor.PosRef);                                      // Moves stage if position has not been reached (particularly useful when movement is slow)
                        Arduino.MoveStage(ref Arduino.MainMotor, BStepTB.Value, 'P');
                    }
                    if (e.ID == '~')
                    {
                        BAStepTBLbl.Text = ("Step: " + Arduino.AuxMotor.PosRef);                                      // Moves stage if position has not been reached (particularly useful when movement is slow)
                        Arduino.MoveStage(ref Arduino.AuxMotor, BAStepTB.Value, 'P');
                    }
                    break;
                case "DataInfo":
                    BStepTxt.Text = Arduino.MainMotor.StepVal;
                    BCycleTxt.Text = Arduino.MainMotor.CycleVal;
                    BTimeTxt.Text = Arduino.MainMotor.TimeVal;
                    BAStepTxt.Text = Arduino.AuxMotor.StepVal;
                    BoardData = true;
                    BStateLbl.Text = (BStateLbl.Text + ("\nData retrieved from board."));
                    if (onCalibration)
                    {
                        request = "calibrate";
                        response = "start";
                    }
                    break;
                case "Origin":
                    BStateLbl.Text = ("Current " + e.Motor + " motor position set as origin");
                    if (e.ID == '@')
                    {
                        BStepTB.Value = 0;
                        BStepTBLbl.Text = ("Step: 0");
                        BCycleCountLbl.Text = "0";
                        BCycle1Btn.Enabled = false;
                        response = ("originAux");

                    }
                    if (e.ID == '~')
                    {
                        BAStepTB.Value = 0;
                        BAStepTBLbl.Text = ("Step: 0");
                        BACycleCountLbl.Text = "0";
                        BACycle1Btn.Enabled = false;
                        response = ("none");
                    }
                    break;
                case "Cycle":
                    BStateLbl.Text = (e.Motor + " motor step finished");
                    if (e.ID == '@')
                    {
                        BStepTB.Value = 0;
                        BStepTBLbl.Text = ("Step: 0");
                        if (BMAuxChkBtn.Checked)
                            response = ("moveAux");
                        else
                            response = ("next");
                    }
                    if (e.ID == '~')
                    {
                        BAStepTB.Value = 0;
                        BAStepTBLbl.Text = ("Step: 0");
                        response = ("next");
                    }
                    break;
                case "DataSaved":
                    BStateLbl.Text = ("Data Saved to board.");
                    if (Auto)
                        response = "folders";
                    if (onCalibration)
                    {
                        request = "calibrate";
                        response = "start";
                    }
                    break;
                case "ServoMoved":
                    BStateLbl.Text = ("Focus servo-motor moved.");
                    response = "servo";
                    break;
                case "Activated":
                    BStateLbl.Text = (e.Motor + " motor activated");
                    response = "activated";
                    break;
            }
            if (Auto & !calibrationChkBtn.Checked)
                Automation(request, response);
            if (Auto & calibrationChkBtn.Checked)
                CalibratedAutomation(request, response);
            if (onCalibration)
                Calibration(request, response);
        }

        private void Connected(object sender, EventArgs e)                                      // Manages on connection actions. This routine has been designed in order to avoid communnication errors (Tested on errors, the normal behavior should not have any)
        {

            BStateLbl.Text = (BStateLbl.Text + "\nPort: " + Arduino.PortCOM.PortName.ToString() + "\n" + Arduino.PortCOM.BaudRate.ToString() + " bps\nConnection successful!!! :)");

            // Form Object visualization routine
            session = Arduino.session;
            BConnectBtn.Enabled = true;
            //foreach (Control control in BoardPanel.Controls)
            //{
            //    control.Enabled = true;
            //}
            FocusPanel.Visible = true;
            BoardPanel.Enabled = true;
            if (SonyQX10.CamConStatus)
            {
                StartBtn.Enabled = true;
                ManageChkBtn.Enabled = true;
                focusTB.Visible = true;
                focusLbl.Visible = true;
            }

            if (Arduino.PortCOM.IsOpen)                                                             // On success, manages control label and request information (step, cycle and time), if not, sends error msg
            {
                BConnectBtn.Text = ("Disconnect Board");
            }
            else
            {
                MessageBox.Show("COM Port error");
            }
        }

        private void Disconnected(object sender, EventArgs e)                                   // Disconnection routine
        {
            // Form Object visualization routine
            BStateLbl.Text = ("Disconnected...");
            BConnectBtn.Text = ("Connect Board");
            BConnectBtn.Enabled = true;
            BoardAuxPanel.Visible = false;
            BoardPanel.Enabled = false;
            foreach (Control control in BoardPanel.Controls)
            {
                //control.Enabled = false;
                if (control is CheckBox)
                    ((CheckBox)control).Checked = false;
                if (control is TextBox)
                    ((TextBox)control).Text = ("");
            }

            Arduino.MainMotor.Cycle = 0;
            BSpeedTB.Value = 3;                                                                     // Manages form layout (Disable microscope control buttons) TODO: Find a more ellegant way to do this
            BStepTB.Value = 0;
            BStepTB.Maximum = 100;
            BStepTBLbl.Text = ("Step:");
            BCycleCountLbl.Text = ("0");
            Arduino.AuxMotor.Cycle = 0;
            BoardAuxPanel.Enabled = false;
            BASpeedTB.Value = 3;                                                                     // Manages form layout (Disable microscope control buttons) TODO: Find a more ellegant way to do this
            BAStepTB.Value = 0;
            BAStepTB.Maximum = 100;
            BAStepTBLbl.Text = ("Step:");
            BACycleCountLbl.Text = ("0");
            focusTB.Value = 0;
            Auto = false;
            Calibrated = false;
            onCapture = false;

            StartBtn.Enabled = false;
            ManageChkBtn.Enabled = false;
            FocusPanel.Visible = false;
        }


        // The following code is (Mostly) related to Automated observation
        //      TODO:
        //              - There's a bug in the waiting time: As motors can be moved the first picture might be lost... however, for the second picture (frame1) they will be in possition again
        //              - Improve action visualization
        //              - Direction should not be changed during Automated routines... it might result on unespected behaviors


        private void ProtectControls(ref Panel thisPanel, bool show)
        {
            string[] protectedControl = { "TB", "Img", "StepChkBtn", "reverseChkBtn", "Max1Btn", "Max2Btn", "StepMaxLbl" };
            foreach (Control control in thisPanel.Controls)
            {
                if (!(protectedControl.Any(control.Name.Contains)))
                    control.Enabled = show;
            }
            Update();
        }

        private void Automation(string instruction, string guide = "none")
        {
            if (Auto)
            {
                request = instruction;
                if (guide == "escape")
                    instruction = "escape";
                switch (instruction)
                {
                    case "escape":
                        break;
                    case "start":
                        switch (guide)
                        {
                            case "none":
                                Invoke(new EventHandler(AutoSyncrhonize));
                                break;
                            case "origin":
                                Arduino.SetOrigin(ref Arduino.MainMotor);
                                break;
                            case "originAux":
                                Arduino.SetOrigin(ref Arduino.AuxMotor);
                                break;
                            case "save":
                                Arduino.SaveData(BStepTxt.Text, BCycleTxt.Text, BTimeTxt.Text, BAStepTxt.Text);
                                break;
                            case "folders":
                                Invoke(new EventHandler(CreateFolders));
                                break;
                            default:
                                break;
                        }
                        break;
                    case "capture":
                        switch (guide)
                        {
                            case "start":
                                if (myFrame == 0)
                                {
                                    Invoke(new EventHandler(checktimer));
                                }
                                onSave = false;
                                onMove = false;
                                onCapture = true;
                                BoardPanel.Enabled = false;
                                BoardAuxPanel.Enabled = false;
                                CameraPanel.Enabled = false;
                                FocusPanel.Enabled = false;
                                Invoke(new EventHandler(TakePictue));
                                break;
                            case "move":
                                Arduino.MoveStage(ref Arduino.MainMotor, Convert.ToInt32(BStepTxt.Text), 'Z');
                                break;
                            case "moveAux":
                                Arduino.MoveStage(ref Arduino.AuxMotor, Convert.ToInt32(BAStepTxt.Text), 'Z');
                                break;
                            case "next":
                                onMove = true;
                                Invoke(new EventHandler(ManageFrames));
                                break;
                            default:
                                break;
                        }
                        break;
                    case "complete":
                        switch (guide)
                        {
                            case "move":
                                Arduino.MoveStage(ref Arduino.MainMotor, Convert.ToInt32(BStepTxt.Text) * TotalFrames, 'S');
                                break;
                            case "moveAux":
                                Arduino.MoveStage(ref Arduino.AuxMotor, Convert.ToInt32(BAStepTxt.Text) * TotalFrames, 'S');
                                break;
                            case "calibrated":
                                Arduino.MoveServo(Focus[0]);
                                break;
                            case "next":
                                myImg += 1;
                                myFrame = 0;
                                BStateLbl.Text += ("\nCycle completed");
                                BoardPanel.Enabled = true;
                                BoardAuxPanel.Enabled = true;
                                CameraPanel.Enabled = true;
                                FocusPanel.Enabled = true;
                                break;
                            default:
                                break;
                        }
                        break;
                }
            }
        }

        private void Calibration(string instruction, string guide = "none")
        {
            if (onCalibration)
            {
                request = instruction;
                if (guide == "escape")
                    instruction = "escape";
                switch (instruction)
                {
                    case "escape":
                        break;
                    case "start":
                        ProtectControls(ref BoardAuxPanel, false);
                        BoardPanel.Enabled = false;
                        BoardAuxPanel.Enabled = false;
                        FocusPanel.Enabled = false;
                        CameraPanel.Enabled = false;
                        CapturePanel.Enabled = false;
                        calibrationChkBtn.Enabled = false;
                        myFrame = 0;
                        switch (guide)
                        {
                            case "none":
                                Array.Resize(ref Focus, Convert.ToInt32(BCycleTxt.Text) + 1);
                                Array.Resize(ref Auxiliar, Convert.ToInt32(BCycleTxt.Text) + 1);
                                Focus[0] = focusTB.Value;
                                Auxiliar[0] = 0;
                                if (BoardData)
                                    Arduino.ReqInfo();
                                else
                                    Arduino.SaveData(BStepTxt.Text, BCycleTxt.Text, BTimeTxt.Text, BAStepTxt.Text);
                                break;
                            case "origin":
                                Arduino.SetOrigin(ref Arduino.MainMotor);
                                break;
                            case "originAux":
                                Arduino.SetOrigin(ref Arduino.AuxMotor);
                                break;
                            default:
                                break;
                        }
                        break;
                    case "calibrate":
                        switch (guide)
                        {
                            case "start":
                                myFrame += 1;
                                Arduino.MoveStage(ref Arduino.MainMotor, Convert.ToInt32(BStepTxt.Text), 'Z');
                                break;
                            case "moveAux":
                                CalibrationBtn.Text = ("SET (" + myFrame.ToString() + ")");
                                BoardAuxPanel.Enabled = true;
                                FocusPanel.Enabled = true;
                                break;
                        }
                        break;
                    case "complete":
                        switch (guide)
                        {
                            case "move":
                                Arduino.MoveServo(Focus[0]);
                                break;
                            case "servo":
                                Arduino.MoveStage(ref Arduino.MainMotor, Convert.ToInt32(BStepTxt.Text) * TotalFrames, 'S');
                                break;
                            case "moveAux":
                                Arduino.MoveStage(ref Arduino.AuxMotor, 0, 'Z');
                                break;
                            case "next":
                                onCalibration = false;
                                myFrame = 0;
                                Calibrated = true;
                                CalibrationBtn.Text = "Calibrate";
                                calibrationChkBtn.Enabled = true;
                                calibrationChkBtn.Checked = true;
                                break;
                            default:
                                break;
                        }
                        break;
                }
            }
        }

        private void CalibratedAutomation(string instruction, string guide = "none")
        {
            if (Auto)
            {
                request = instruction;
                if (guide == "escape")
                    instruction = "escape";
                switch (instruction)
                {
                    case "escape":
                        break;
                    case "start":
                        switch (guide)
                        {
                            case "none":
                                Arduino.MoveServo(Focus[0]);
                                break;
                            case "servo":
                                Invoke(new EventHandler(AutoSyncrhonize));
                                break;
                            case "origin":
                                Arduino.SetOrigin(ref Arduino.MainMotor);
                                break;
                            case "originAux":
                                Arduino.SetOrigin(ref Arduino.AuxMotor);
                                break;
                            case "save":
                                Arduino.SaveData(BStepTxt.Text, BCycleTxt.Text, BTimeTxt.Text, BAStepTxt.Text);
                                break;
                            case "folders":
                                Invoke(new EventHandler(CreateFolders));
                                break;
                            default:
                                break;
                        }
                        break;
                    case "capture":
                        switch (guide)
                        {
                            case "start":
                                if (myFrame == 0)
                                {
                                    Invoke(new EventHandler(checktimer));
                                }
                                onSave = false;
                                onMove = false;
                                onCapture = true;
                                BoardPanel.Enabled = false;
                                BoardAuxPanel.Enabled = false;
                                CameraPanel.Enabled = false;
                                Invoke(new EventHandler(TakePictue));
                                break;
                            case "move":
                                Arduino.MoveServo(Focus[myFrame + 1]);
                                break;
                            case "servo":
                                Arduino.MoveStage(ref Arduino.MainMotor, Convert.ToInt32(BStepTxt.Text), 'Z');
                                break;
                            case "moveAux":
                                Arduino.AuxMotor.PosRef = Auxiliar[myFrame + 1];
                                Arduino.MoveStage(ref Arduino.AuxMotor, Auxiliar[myFrame + 1], 'P');
                                break;
                            case "next":
                                onMove = true;
                                Invoke(new EventHandler(ManageFrames));
                                break;
                            default:
                                break;
                        }
                        break;
                    case "complete":
                        switch (guide)
                        {
                            case "move":
                                Arduino.MoveServo(Focus[0]);
                                break;
                            case "servo":
                                Arduino.MoveStage(ref Arduino.MainMotor, Convert.ToInt32(BStepTxt.Text) * TotalFrames, 'S');
                                break;
                            case "moveAux":
                                Arduino.AuxMotor.PosRef = 0;
                                Arduino.MoveStage(ref Arduino.AuxMotor, 0, 'P');
                                break;
                            case "next":
                                myImg += 1;
                                myFrame = 0;
                                BStateLbl.Text += ("\nCycle completed");
                                BoardPanel.Enabled = true;
                                BoardAuxPanel.Enabled = true;
                                CameraPanel.Enabled = true;
                                break;
                            default:
                                break;
                        }
                        break;
                }
            }
        }

        private void checktimer(object sender, EventArgs e)
        {
            IntervalTmr.Enabled = true;
            timer1.Enabled = true;
            progressBar1.Visible = true;
        }

        private void AutoSyncrhonize(object sender, EventArgs e)
        {
            StartBtn.Text = "STOP";
            BStateLbl.Text = ("Synchronizing Configuration...");
            BoardPanel.Enabled = false;
            BoardAuxPanel.Enabled = false;
            CameraPanel.Enabled = false;
            WriteReport("Automated observation started at: " + DateTime.Now.ToString("hh:mm:ss tt") + "\r\nSteps per cycle: " + BStepTxt.Text + "\r\nNumber of cycles: " + BCycleTxt.Text + "\r\nTime interval (Seconds): " + BTimeTxt.Text);
            if (BMAuxChkBtn.Checked)
                WriteReport("Auxiliar motor: Enabled");
            if (Calibrated)
            {
                WriteReport("Calibration values: Enabled");
            }
            else
            {
                Array.Resize(ref Focus, Convert.ToInt32(BCycleTxt.Text) + 1);
                Array.Resize(ref Auxiliar, Convert.ToInt32(BCycleTxt.Text) + 1);
                for (i = 0; i <= Convert.ToInt32(BCycleTxt.Text); i++)
                {
                    Focus[i] = 0;
                    Auxiliar[i] = 0;
                }
            }
            if (calibrationChkBtn.Checked)
                WriteReport("*Calibrated observation*");
            TotalFrames = Convert.ToInt32(BCycleTxt.Text);
            TotalTime = Convert.ToInt32(BTimeTxt.Text) * 1000;
            timer1.Interval = TotalTime / 100;
            IntervalTmr.Interval = TotalTime;
            progressBar1.Value = 0;
            myFrame = 0;
            myImg = 0;
            if (calibrationChkBtn.Checked)
                CalibratedAutomation("start", "save");
            else
                Automation("start", "save");
        }

        private void CreateFolders(object sender, EventArgs e)
        {
            PicPath = (RootPath + "\\Session" + BitConverter.ToString(session));
            i = 1;
            while (Directory.Exists(PicPath))                                                            // Check requested directory exists, if not, creates it
            {
                PicPath = (RootPath + "\\Session" + BitConverter.ToString(session) + ("_") + i.ToString("D2"));
                i += 1;
            }
            if (!Directory.Exists(PicPath))
            {
                DirectoryInfo di = Directory.CreateDirectory(PicPath);
                BStateLbl.Text = (BStateLbl.Text + ("\nCreating Folders..."));
            }
            for (i = 0; i <= Convert.ToInt32(BCycleTxt.Text); i++)
            {
                if (!Directory.Exists(PicPath + "\\Frame" + i.ToString("D4")))
                {
                    DirectoryInfo di = Directory.CreateDirectory(PicPath + "\\Frame" + i.ToString("D4"));
                }
            }
            BStateLbl.Text = (BStateLbl.Text + ("\nAwaiting for capture"));
            myFrame = 0;
            myImg = 0;


            if (unmanaged)
            {
                if (calibrationChkBtn.Checked)
                    CalibratedAutomation("capture", "start");
                else
                    Automation("capture", "start");
            }
            else
                captureBtn.Enabled = true;
        }

        private void TakePictue(object sender, EventArgs e)
        {
            BStateLbl.Text = ("Frame: " + myFrame.ToString() + " Cycle: " + myImg.ToString() + ("\nCapturing..."));
            SonyQX10.SaveName = (("S") + BitConverter.ToString(session) + ("F") + myFrame.ToString("D3") + ("P") + myImg.ToString("D3") + ".jpg");
            SonyQX10.SavePath = (PicPath + "\\Frame" + myFrame.ToString("D4"));
            SonyQX10.TakePicture.RunWorkerAsync();
        }

        private void ManageFrames(object sender, EventArgs e)
        {
            if (onMove & onSave)
            {
                if (myFrame < TotalFrames)
                {
                    myFrame += 1;
                    onMove = false;
                    onSave = false;
                    BStateLbl.Text = (BStateLbl.Text + ("\nAwaiting for capture"));
                    if (unmanaged)
                    {
                        if (calibrationChkBtn.Checked)
                            CalibratedAutomation("capture", "start");
                        else
                            Automation("capture", "start");
                    }
                    else
                    {
                        BoardPanel.Enabled = true;
                        BoardAuxPanel.Enabled = true;
                        CameraPanel.Enabled = true;
                        onCapture = false;
                    }
                }
                else
                {
                    onMove = false;
                    onSave = false;
                    onCapture = false;
                    captureBtn.Enabled = false;
                    if (calibrationChkBtn.Checked)
                        CalibratedAutomation("complete", "move");
                    else
                        Automation("complete", "move");
                }
            }
        }


        private void StartBtn_Click(object sender, EventArgs e)
        {
            if (!Auto)
            {
                if ((BStepTB.Value != 0) | (BAStepTB.Value != 0) | (Convert.ToInt32(BCycleCountLbl.Text) != 0) | (Convert.ToInt32(BACycleCountLbl.Text) != 0))
                {
                    var result = MessageBox.Show("Current possition will be set as origin\r\nDo you want to continue?", "Confirm Automatic Observation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        Auto = true;
                        ProtectControls(ref BoardPanel, false);
                        ProtectControls(ref BoardAuxPanel, false);
                        if (calibrationChkBtn.Checked)
                            CalibratedAutomation("start", "origin");
                        else
                            Automation("start", "origin");
                    }
                    else
                        BStateLbl.Text = ("Automated observation cancelled\r\nSet Origin manually and procceed");
                }
                else
                {
                    Auto = true;
                    ProtectControls(ref BoardPanel, false);
                    ProtectControls(ref BoardAuxPanel, false);
                    if (calibrationChkBtn.Checked)
                        CalibratedAutomation("start");
                    else
                        Automation("start");
                }
            }
            else
            {
                Auto = false;
                ProtectControls(ref BoardPanel, true);
                ProtectControls(ref BoardAuxPanel, true);
                StartBtn.Text = "START";
                BoardPanel.Enabled = true;
                BoardAuxPanel.Enabled = true;
                CameraPanel.Enabled = true;
                IntervalTmr.Enabled = false;
                timer1.Enabled = false;
                progressBar1.Visible = false;
                BStateLbl.Text = ("Automated observation stopped\r\nIf any instruction is pending\r\nit will be executed");
                WriteReport("\r\nAutomated observation stopped at: " + DateTime.Now.ToString("hh:mm:ss tt"));
            }
        }

        private void captureBtn_Click(object sender, EventArgs e)
        {
            if (!onCapture)
            {
                BoardPanel.Enabled = false;
                BoardAuxPanel.Enabled = false;
                CameraPanel.Enabled = false;
                onCapture = true;
                if (calibrationChkBtn.Checked)
                    CalibratedAutomation("capture", "start");
                else
                    Automation("capture", "start");
            }
        }

        private void ManageChkBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (ManageChkBtn.Checked)
                unmanaged = true;
            else
                unmanaged = false;
        }

        private void IntervalTmr_Tick(object sender, EventArgs e)
        {
            IntervalTmr.Enabled = false;
            timer1.Enabled = false;
            progressBar1.Visible = false;
            progressBar1.Value = 0;
            if (!unmanaged)
            {
                captureBtn.Enabled = true;
                SystemSounds.Exclamation.Play();
            }
            else
            {
                SystemSounds.Beep.Play();
                onCapture = true;
                Automation("capture", "start");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)                                    // Progress bar timer (Manages time visualization)
        {
            progressBar1.Value += 1;
        }

        private void focusTB_Scroll(object sender, EventArgs e)
        {
            if (!Arduino.Busy)
            {
                Arduino.MoveServo(focusTB.Value);
            }
        }

        private void CalibrationBtn_Click(object sender, EventArgs e)
        {
            if (!onCalibration)
            {
                if (!BoardData)
                {
                    var result = MessageBox.Show("The current properties are not saved.\r\nDo you want to save the current configuration?\r\n(If you select NO, the last saved configuration will be loaded.)", "Current configuration not saved", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.No)
                        BoardData = true;                    
                }
                TotalFrames = Convert.ToInt32(BCycleTxt.Text);
                if ((BStepTB.Value != 0) | (BAStepTB.Value != 0) | (Convert.ToInt32(BCycleCountLbl.Text) != 0) | (Convert.ToInt32(BACycleCountLbl.Text) != 0))
                {
                    var result = MessageBox.Show("Current possition will be set as origin\r\nDo you want to continue?", "Confirm calibration", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (result == DialogResult.Yes)
                    {
                        onCalibration = true;
                        Calibration("start", "origin");
                    }
                    else
                        BStateLbl.Text = ("Calibration cancelled\r\nSet Origin manually and procceed");
                }
                else
                {
                    onCalibration = true;
                    Calibration("start");
                }
            }
            else
            {
                Focus[myFrame] = focusTB.Value;
                Auxiliar[myFrame] = BAStepTB.Value;

                if (myFrame < TotalFrames)
                    Calibration("calibrate", "start");
                else
                    Calibration("complete", "move");
            }
        }

        private void calibrationChkBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (calibrationChkBtn.Checked)
            {
                ProtectControls(ref BoardPanel, false);
                ProtectControls(ref BoardAuxPanel, false);
                BoardPanel.Enabled = true;
                BoardAuxPanel.Enabled = true;
                FocusPanel.Enabled = true;
                CameraPanel.Enabled = true;
                CapturePanel.Enabled = true;
                CalibrationBtn.Enabled = false;
            }
            else
            {
                ProtectControls(ref BoardPanel, true);
                ProtectControls(ref BoardAuxPanel, true);
                CalibrationBtn.Enabled = true;

            }

        }

        private void BStepTxt_TextChanged(object sender, EventArgs e)
        {
            BoardData = false;
            Calibrated = false;
            calibrationChkBtn.Enabled = false;
        }

        private void BCycleTxt_TextChanged(object sender, EventArgs e)
        {
            BoardData = false;
            Calibrated = false;
            calibrationChkBtn.Enabled = false;
        }

        private void BTimeTxt_TextChanged(object sender, EventArgs e)
        {
            BoardData = false;
            Calibrated = false;
            calibrationChkBtn.Enabled = false;
        }

        private void BAStepTxt_TextChanged(object sender, EventArgs e)
        {
            BoardData = false;
            Calibrated = false;
            calibrationChkBtn.Enabled = false;
        }
    }

}


