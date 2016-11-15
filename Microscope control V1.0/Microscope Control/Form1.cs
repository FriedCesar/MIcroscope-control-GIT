////////////////*   Program for automatic movement control of a microscope (Stage) via step motor and image automatic capture (Using Sony DSC-QX10) */////////////
//
// Program for automatic movement control of a microscope (Stage) via step motor and image automatic capture (Using Sony DSC-QX10)
// 
// César Augusto Hernández Espitia
// ca.hernandez11@uniandes.edu.co
//
// V1.0      October/2016
// This program is a heritage from Calibration MMS
// Program designed to be connected with an ARDUINO MEGA
// Designed to be connected with a Sony DSC-QX10 camera (It uses the Sony's Remote Camera API SDK; small changes can be implemented to extend range)
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
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using AForge.Video;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Microscope_Control
{
    public partial class Form1 : Form
    {

        // The following Events are related to the camera behavior

        // Type definition of Camera related variables

        List<byte> imgData = new List<byte>();      // Byte list for storing image data
        Stream imgStream;                           // Data stream for image aquisition (Liveview)
        StreamReader imgReader;                     // Stream reader for image data (Liveview)
        int i = 0;                                  // Multipropose counter
        int imgSize = 0;                            // Image size for data retrieval (Liveview)
        int frameNo = 0;                            // Frame No. (Liveview)
        int paddingSize = 0;                        // Padding size (Liveview)
        bool FlagLvw = false;                       // Flag to retrieve action on liveview event                   
        bool CamConStatus = false;                  // Camera connection flag
        byte[] buffer = new byte[520];              // Data buffer for liveview
        byte[] bufferAux = new byte[4];             // Data auxiliar buffer for liveview
        byte payloadType = 0;                       // Stores the payload type from liveview stream
        string CamResponse = "";                    // Retrieves the camera response when any action is invoked
        string lvwURL = "";                         // Stores camera URL for liveview
        bool timeout = false;

        // The following Events are related to the board managing and communication

        // Type definition of Stage related variables


        Random rnd = new Random();                          // Random session iniciator
        byte[] session;                                     // Byte session identifier
        byte[] sessionRx;                                   // Byte session echo
        byte[] pos1;
        //byte[] pos2;
        //byte[] pos3;
        bool PortSel = false;                               // Retrieves information of board connection
        bool ConSuc = false;                                // Succesful connection flag
        bool Busy = false;                                  // Activity monitoring flag
        string TxString;                                    // Data transmision string (Send this)
        string RxString;                                    // Data received string
        int conTO = 0;                                      // Timeout connection by attempts
        int Pos = 0;                                        // Position verifier
        int PosRef = 0;                                     // Position reference
        int Cycle = 0;                                      // Cycle verifier
        int picCount = 0;


        public Form1()
        {
            InitializeComponent();
            ImgGuide.BackColor = Color.Transparent;
            ImgGuide.Parent = ImgLiveview;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync();
            BConnectionCBox.Items.Add("Port selection");
            BConnectionCBox.SelectedIndex = 0;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)               // On close, zoom camera out, disconnect board
        {
            if (CamConStatus)
                CamResponse = SendRequest("actZoom", "\"out\",\"start\"");
            if (serialPort1.IsOpen == true)
            {
                Invoke(new EventHandler(Disconnect));
            }
        }


        // The following code is (Mostly) related to the managing of the Camera


        private void ConnectBtn_Click(object sender, EventArgs e)                           // Manages the discovery routine to connect with camera DSC-QX10 (Must be connected to PC WiFi)
        {
            try
            {
                ConnectionTxt.Visible = true;
                ConnectBtn.Enabled = false;
                //Thread.Sleep(200);

                // Setup Client/Host Endpoints and communication socket
                IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Any, 60000);                                        // Creates Endpoint to connect with system client
                IPEndPoint MulticastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);                // Creates Endpoint to connect with camera host (Multicast messages reserved address, Sony SDK)
                Socket UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);          // Creates Socket for managing network communication
                UdpSocket.Bind(LocalEndPoint);                                                                          // Asociates Local socket to external host (Camera)
                ConnectionTxt.Text = ("Status\r\nUDP-Socket setup finished...\r\n");

                // Sends discovery request to camera host (SSDP M-SEARCH)
                string SearchString = "M-SEARCH * HTTP/1.1\r\nHOST:239.255.255.250:1900\r\nMAN:\"ssdp:discover\"\r\nMX:2\r\nST:urn:schemas-sony-com:service:ScalarWebAPI:1\r\n\r\n";
                // SSDP M-SEARCH request (SONY SDK) string
                UdpSocket.SendTo(Encoding.UTF8.GetBytes(SearchString), SocketFlags.None, MulticastEndPoint);            // Sends M-SEARCH request (8-bit Unicode) UNICAST
                ConnectionTxt.AppendText("M-Search sent\r\n");

                // Receives discovery response from camera UNICAST (TimedOut on 30 secs)
                byte[] ReceiveBuffer = new byte[64000];
                int ReceivedBytes = 0;
                Thread TimeoutThread = new Thread(ThreadProc);
                TimeoutThread.Start();
                while (TimeoutThread.IsAlive)                                                                                            // Received Buffered response
                {
                    if (UdpSocket.Available > 0)
                    {
                        ReceivedBytes = UdpSocket.Receive(ReceiveBuffer, SocketFlags.None);

                        if (ReceivedBytes > 0)
                        {
                            ConnectionTxt.AppendText(Encoding.UTF8.GetString(ReceiveBuffer, 0, ReceivedBytes));
                            CamConStatus = true;
                        }
                        break;
                    }
                }
                if (CamConStatus)
                {
                    // Loads transparent logo to guide image (Done here to serve as a sleep function)
                    Bitmap referenceImg = new Bitmap(ImgLogo.Image);
                    Bitmap transparentImg = new Bitmap(ImgLogo.Image.Width, ImgLogo.Image.Height);
                    Graphics tempG = Graphics.FromImage(referenceImg);
                    Color c = Color.Transparent;
                    Color v = Color.Transparent;
                    for (int x = 0; x < ImgLogo.Image.Width; x++)
                    {
                        for (int y = 0; y < ImgLogo.Image.Height; y++)
                        {
                            c = referenceImg.GetPixel(x, y);
                            v = Color.FromArgb(13, c.R, c.G, c.B);
                            transparentImg.SetPixel(x, y, v);
                        }
                    }
                    tempG.DrawImage(transparentImg, Point.Empty);
                    ImgGuide.Image = transparentImg;

                    ConnectionTxt.Visible = false;
                    ConnectionTxt.Text = "";
                    LiveviewBtn.Enabled = true;
                    CamResponse = SendRequest("setPostviewImageSize", "\"Original\"");
                    CamResponse = SendRequest("actZoom", "\"in\",\"start\"");
                    getEventTxt.Text = CamResponse;

                    ConnectionTxt.AppendText("Connection successful =)  \r\n");
                    if (ConSuc)
                    {
                        BShutterBtn.Enabled = true;
                        StartBtn.Enabled = true;
                        ManageChkBtn.Enabled = true;
                    }
                }
                else
                {
                    ConnectBtn.Enabled = true;
                    ConnectionTxt.AppendText("Connection TimedOut =(  \r\n");
                    UdpSocket.Close();
                }
            }
            catch (Exception ex)
            {
                ConnectionTxt.Text = ex.Message;
            }
        }

        private void LiveviewBtn_Click(object sender, EventArgs e)                          // Manages beginning/end of liveview
        {
            if (!FlagLvw)
            {
                ImgLiveview.Visible = true;
                FlagLvw = true;
                CamResponse = SendRequest("startLiveview", "");                 // Send action request to camera host to start liveview
                ConnectionTxt.AppendText(CamResponse + "\r\n");
                lvwURL = CamResponse.Substring(19).Split('\"').FirstOrDefault();// Setup the URL for the liveview download
                WebRequest lvwRequest = WebRequest.Create(lvwURL);                          // Create a request using the camera liveview URL, send HTTP GET request
                lvwRequest.Method = "GET";
                lvwRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                imgStream = lvwRequest.GetResponse().GetResponseStream();                   // Setup and get the request stream response
                imgReader = new StreamReader(imgStream);
                if (backgroundWorker1.IsBusy != true)
                    backgroundWorker1.RunWorkerAsync();
                // Start liveview Timer (Send HTTP GET request)
                guideChkBtn.Enabled = true;
            }
            else
            {
                ImgLiveview.Visible = false;
                FlagLvw = false;
                CamResponse = SendRequest("stopLiveview", "");                  // Send action request to camera host to stop liveview
                ConnectionTxt.AppendText(CamResponse + "\r\n");
                backgroundWorker1.CancelAsync();
                guideChkBtn.Enabled = false;
                imgStream.Close();
                imgReader.Close();
            }
        }

        private void LiveviewTmr_Tick(object sender, EventArgs e)                           // Timer, refreshes liveview image
        {
            {
                using (var memstream = new MemoryStream())
                {
                    imgData = new List<byte>();
                    buffer = new byte[520];
                    bufferAux = new byte[4];
                    payloadType = 0;
                    imgSize = 0;
                    frameNo = -1;
                    paddingSize = 0;

                    GetHeader:                                                          // Retrieves a byte(s) from the stream to check if it corresponds to Sony header construction

                    // Common Header (8 Bytes)
                    //buffer = new byte[520];
                    imgReader.BaseStream.Read(buffer, 0, 1);                            // Seeks for start byte
                    var start = buffer[0];
                    if (start != 0xff)
                        goto GetHeader;

                    //buffer = new byte[520];
                    imgReader.BaseStream.Read(buffer, 0, 1);                            // Stores payload Type
                    payloadType = (buffer[0]);
                    if (!((payloadType == 1) || (payloadType == 2)))
                        goto GetHeader;

                    //buffer = new byte[520];
                    imgReader.BaseStream.Read(buffer, 0, 2);                            // Stores Frame Number depending Payload type
                    if (payloadType == 1)
                        frameNo = BitConverter.ToUInt16(buffer, 0);

                    imgReader.BaseStream.Read(buffer, 0, 4);                            // Discards expected Time stamp

                    // Payload header (128 bytes)
                    //buffer = new byte[520];
                    imgReader.BaseStream.Read(buffer, 0, 4);
                    if (!((buffer[0] == 0x24) & (buffer[1] == 0x35) & (buffer[2] == 0x68) & (buffer[3] == 0x79)))
                        goto GetHeader;                                                 // If the start code does not correspond to fixed code (0x24, 0x35, 0x68, 0x79), starts over

                    //bufferAux = new byte[4];
                    imgReader.BaseStream.Read(bufferAux, 0, 4);
                    paddingSize = bufferAux[3];
                    bufferAux[3] = bufferAux[2];
                    bufferAux[2] = bufferAux[1];
                    bufferAux[1] = bufferAux[0];
                    bufferAux[0] = 0;
                    Array.Reverse(bufferAux);
                    imgSize = BitConverter.ToInt32(bufferAux, 0);                       // Reads and translates Data stream size

                    if (payloadType == 1)                                               // Case JPEG data
                    {
                        imgReader.BaseStream.Read(buffer, 0, 120);
                        while (imgData.Count < imgSize)
                        {
                            //buffer = new byte[520];
                            imgReader.BaseStream.Read(buffer, 0, 1);
                            imgData.Add(buffer[0]);
                        }
                    }

                    //getEventTxt.AppendText("Image size: " + imgData.Count.ToString());
                    MemoryStream stream = new MemoryStream(imgData.ToArray());
                    BinaryReader reader = new BinaryReader(stream);
                    Bitmap bmpImage = (Bitmap)Image.FromStream(stream);

                    if (ImgLiveview.Image != null)
                        ImgLiveview.Image.Dispose();

                    ImgLiveview.Image = bmpImage;
                }
            }
        }

        private void guideChkBtn_CheckedChanged(object sender, EventArgs e)                 // Visualize frozen image to use it as a guide in liveview
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
        private static Object locker = new Object();

        private void guideRefreshBtn_Click(object sender, EventArgs e)                      // Loads image from live view to be frozen and displayed as a guide frame
        {
            ImgGuide.Location = new Point(0, 0);                                                    // Ensures the reference image frame is in place
            Bitmap referenceImg;
            lock (locker)
                referenceImg = new Bitmap(ImgLiveview.Image);
            Bitmap transparentImg = new Bitmap(referenceImg.Width, referenceImg.Height);
            Graphics tempG = Graphics.FromImage(referenceImg);
            Color c = Color.Black;
            Color v = Color.Black;
            for (int x = 0; x < referenceImg.Width; x++)
            {
                for (int y = 0; y < referenceImg.Height; y++)
                {
                    c = referenceImg.GetPixel(x, y);
                    v = Color.FromArgb(50, c.R, c.G, c.B);
                    transparentImg.SetPixel(x, y, v);
                }
            }
            tempG.DrawImage(transparentImg, Point.Empty);
            ImgGuide.Image = transparentImg;
        }

        private void pictureBox3_LoadCompleted(object sender, AsyncCompletedEventArgs e)    // Waits for Loaded image to be saved
        {
            if (OnCapture)
            {
                BStateLbl.Text = (BStateLbl.Text + ("\nImage capture finished"));
                onSave = true;
                ManageFrames();
            }
        }

        private string SendRequest(string action, string param)                             // Gives format to the action request, manages sending request and receiving response. Output: Response string
        {
            string responseF;
            try
            {
                // Create POST data and convert it to a byte array (Set the ContentType property of the WebRequest to an 8-bit Unicode)
                string postData = "{\"method\": \"" + action + "\",\"params\": [" + param + "],\"id\": 1,\"version\": \"1.0\"}";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                // Send action request
                WebRequest request = WebRequest.Create("http://10.0.0.1:10000/sony/camera ");                       // Create a request using the camera Action list URL
                request.Method = "POST";
                request.ContentType = "application/json; charset=utf-8";                                            // Set the Method property of the request to POST
                request.ContentLength = byteArray.Length;                                                           // Set the ContentLength property of the WebRequest
                Stream dataStream = request.GetRequestStream();                                                     // Get the request stream
                dataStream.Write(byteArray, 0, byteArray.Length);                                                   // Write the data to the request stream
                dataStream.Close();                                                                                 // Close the Stream object

                // Receive camera (Host) response
                WebResponse response = request.GetResponse();                                                       // Display the status
                //ConnectionTxt.AppendText(((HttpWebResponse)response).StatusDescription);
                dataStream = response.GetResponseStream();                                                          // Open the stream using a StreamReader for easy access
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                //ConnectionTxt.AppendText(responseFromServer);
                //var fot = responseFromServer.Substring(20).Split('\"').FirstOrDefault();

                // Close Objects
                reader.Close();                                                                                     // Closes reader, stream object and response
                dataStream.Close();
                response.Close();
                responseF = responseFromServer;
            }
            catch (Exception e)
            {
                ConnectionTxt.Text = e.Message;
                responseF = "";
            }
            return responseF;
        }
        private static void ThreadProc()
        {
            Thread.Sleep(20000);
        }

        // These events are provided for test purposes only Any release: please leave these as NOT VISIBLE

        private void getEventBtn_Click(object sender, EventArgs e)                          // Test Button (Not visible) Requests Events to camera
        {
            //JSON.net
            getEventTxt.Text = "";
            CamResponse = SendRequest("getEvent", "true");                              // These are my formating steps, please do not look at them... it is actually a very bad coding
            CamResponse = CamResponse.Replace("\",\"", "\r\n\t");
            CamResponse = CamResponse.Replace("\":\"", ": ");
            CamResponse = CamResponse.Replace("{\"", "{\r\n");
            CamResponse = CamResponse.Replace("\":[", ":\r\n\t");
            CamResponse = CamResponse.Replace("{\"", "{\r\n");
            CamResponse = CamResponse.Replace("\"],\"", "\r\n\t\t");
            CamResponse = CamResponse.Replace(",\"", "\r\n\t");
            CamResponse = CamResponse.Replace("\"", "");
            CamResponse = CamResponse.Replace("{", "");
            CamResponse = CamResponse.Replace("{", "");
            CamResponse = CamResponse.Replace(",", "\r\n");
            getEventTxt.AppendText(CamResponse);
        }

        private void TestBtn_Click(object sender, EventArgs e)                              // Test Button (Not available) <INSERT YOUR TEST CODE HERE>
        {
            BStateLbl.Text = RxString;

            //*******************

            //string posAux = Convert.ToString(BStepTB.Value, 2);
            //string posAux2 = posAux;
            //int lendif = 21 - posAux.Length;
            //for (i = 0; i < lendif; i++)
            //    posAux = '0'+posAux;
            //pos1 = new byte[] { Convert.ToByte(posAux.Substring(0, 7), 2) };
            //pos2 = new byte[] { Convert.ToByte(posAux.Substring(7, 7), 2) };
            //pos3 = new byte[] { Convert.ToByte(posAux.Substring(14,7), 2)};
            //;
            //BCycleTxt.Text = (BitConverter.ToString(pos1) + BitConverter.ToString(pos2) + BitConverter.ToString(pos3));

            //********************

            //CamResponse = SendRequest("actTakePicture", "");
            //string imgURL = CamResponse.Substring(20).Split('\"').FirstOrDefault();
            //ImgAux.ImageLocation = imgURL;
            //timer1.Enabled = !timer1.Enabled;
        }

        private void timer1_Tick(object sender, EventArgs e)                                // Test timer (Not available) <INSERT YOUR TEST CODE HERE>
        {
            if (i >= 1)
            {
                ImgLiveview.Image.Dispose();
            }
            //pictureBox1.Image = Image.FromFile(@"C:\Users\TOSHIBA\Documents\Archivos doctorado\2016-2\Observación\Time Lapse\Final\P" + string.Format("{0:00}", i) + ".png");
            FileStream fs = new FileStream(@"C:\Users\TOSHIBA\Documents\Archivos doctorado\2016-2\Observación\Time Lapse\Final\P" + string.Format("{0:00}", i) + ".png", FileMode.Open, FileAccess.Read);
            ImgLiveview.Image = Bitmap.FromStream(fs);
            fs.Close();
            i++;
            if (i == 65)
            {
                i = 0;
                timer1.Enabled = false;
            }
        }

        //

        private void comboBox1_DropDown(object sender, EventArgs e)                                     // Sniffs for serial ports connected to the computer (Arduino connects vias Serial)
        {
            string[] ports = SerialPort.GetPortNames();                 // Sniffs for connected ports
            BConnectionCBox.Items.Clear();                              // Cleans previous data in Combobox
            BConnectionCBox.Items.Add("Port selection");
            BConnectionCBox.SelectedIndex = 0;
            foreach (string port in ports)                              // Adds available ports to the Combobox's list
            {
                BConnectionCBox.Items.Add(port);
            }
        }

        private void BConnectionCBox_SelectedIndexChanged(object sender, EventArgs e)                   // Enables connection button on serial type port selection (i.e. if a serial port is selecten in the combo box)
        {
            if (BConnectionCBox.Text.Contains("COM"))
                BConnectBtn.Enabled = true;
            else
                BConnectBtn.Enabled = false;
        }

        private void BConnectBtn_Click(object sender, EventArgs e)                                      // Starts connection routine (No error handling)
        {
            conTO = 0;
            BStateLbl.Text = ("Status");
            if (BConnectionCBox.Text.Contains("COM") && !PortSel)                   // Allows connection if a valid COM port is connected and sets a flag for port selected
            {
                PortSel = true;
            }
            if (PortSel)
            {
                if (serialPort1.IsOpen)                                             // If port is open, close port (Manages controller labels)
                {
                    BStateLbl.Text = ("Disconnected...");
                    Invoke(new EventHandler(Disconnect));
                    BConnectBtn.Text = ("Connect Board");
                }
                if (!serialPort1.IsOpen & PortSel)                                  // If port is closed, and a valid serial port is selected, allow connection
                {
                    serialPort1.PortName = BConnectionCBox.Text;                    // Configurates the serial port
                    serialPort1.BaudRate = 57600;

                    session = new byte[] { Convert.ToByte(rnd.Next(1, 128)) };      // Generates a session number byte
                    TxString = ("COMREQU" + Encoding.ASCII.GetString(session));     // Constructs the conection request instruction

                    getEventTxt.Text = TxString;                                    // Used to monitor the COMREQU command
                    getEventTxt.AppendText(BitConverter.ToString(session));

                    serialPort1.Open();                                             // Opens Port
                    serialPort1.WriteLine("");                                      // Wakeup call
                    serialPort1.WriteLine(TxString);                                // Sends Connection Request
                }
            }
        }

        private void BShutterBtn_Click(object sender, EventArgs e)
        {
            ShutterBW.RunWorkerAsync();
        }

        private void BSaveBtn_Click(object sender, EventArgs e)
        {
            Busy = true;
            TxString = ("@" + Encoding.ASCII.GetString(session) + "V" + BStepTxt.Text + "s" + BCycleTxt.Text + "c" + BTimeTxt.Text + "t");
            serialPort1.WriteLine(TxString);
        }

        private void BStepTB_Scroll(object sender, EventArgs e)
        {
            PosRef = BStepTB.Value;                                                           // Almacena la posición de usuario del trackBar, esta es la referencia para verificar el movimiento de la plataforma
            ;
            if (!Busy)                                                                          // Envía el dato de posición en tiempo de ejecución si la bandera Busy está inactiva
            {
                Busy = true;
                BStepTBLbl.Text = ("Step: " + PosRef);
                MoveStage(BStepTB.Value, 'P');

                //BStepTBLbl.Text = ("Step: " + PosRef);
                //Pos = BStepTB.Value;
                //string posAux = Convert.ToString(Pos, 2);
                //int lendif = 21 - posAux.Length;
                //for (i = 0; i < lendif; i++)
                //    posAux = '0' + posAux;
                //pos3 = new byte[] { Convert.ToByte(posAux.Substring(0, 7), 2) };
                //pos2 = new byte[] { Convert.ToByte(posAux.Substring(7, 7), 2) };
                //pos1 = new byte[] { Convert.ToByte(posAux.Substring(14, 7), 2) };
                //TxString = ("@" + Encoding.ASCII.GetString(session) + "P" + Encoding.ASCII.GetString(pos1) + Encoding.ASCII.GetString(pos2) + Encoding.ASCII.GetString(pos3));
                //BStateLbl.Text = ("Moving...");
                //serialPort1.WriteLine(TxString);
            }
            else
            {
                Pos = BStepTB.Value;
            }
        }

        private void BStepMinBtn_Click(object sender, EventArgs e)
        {
            Busy = true;
            TxString = ("@" + Encoding.ASCII.GetString(session) + "O");
            Pos = 0;
            PosRef = 0;
            BStepTB.Value = 0;
            BStepTBLbl.Text = ("Step: 0");
            serialPort1.WriteLine(TxString);
        }

        private void BStepMaxBtn_Click(object sender, EventArgs e)
        {
            BStepTB.Maximum = BStepTB.Value;
            BStepMaxLbl.Text = ("Max: " + BStepTB.Maximum);
        }

        private void BStepSetBtn_Click(object sender, EventArgs e)
        {
            BStepTxt.Text = BStepTB.Value.ToString();
        }

        private void BCycle1Btn_Click(object sender, EventArgs e)
        {
            if (!Busy)
            {
                Busy = true;
                if (BCycleCountLbl.Text == "1")
                {
                    BCycle1Btn.Enabled = false;
                }
                Cycle = Cycle - 1;
                BCycleCountLbl.Text = Cycle.ToString();
                MoveStage(Convert.ToInt32(BStepTxt.Text), 'S');
                //string cycleAux = Convert.ToString(Convert.ToInt32(BStepTxt.Text), 2);
                //int lendif = 21 - cycleAux.Length;
                //for (i = 0; i < lendif; i++)
                //    cycleAux = '0' + cycleAux;
                //pos3 = new byte[] { Convert.ToByte(cycleAux.Substring(0, 7), 2) };
                //pos2 = new byte[] { Convert.ToByte(cycleAux.Substring(7, 7), 2) };
                //pos1 = new byte[] { Convert.ToByte(cycleAux.Substring(14, 7), 2) };
                ////BCycleTxt.Text = (BitConverter.ToString(pos1) + BitConverter.ToString(pos2) + BitConverter.ToString(pos3));
                //TxString = ("@" + Encoding.ASCII.GetString(session) + "S" + Encoding.ASCII.GetString(pos1) + Encoding.ASCII.GetString(pos2) + Encoding.ASCII.GetString(pos3));
                //BStateLbl.Text = ("Moving...");
                //serialPort1.WriteLine(TxString);
            }
        }

        private void BCycle2Btn_Click(object sender, EventArgs e)
        {
            if (!Busy)
            {
                Busy = true;
                if (BCycleCountLbl.Text == "0")
                {
                    BCycle1Btn.Enabled = true;
                }
                Cycle = Cycle + 1;
                BCycleCountLbl.Text = Cycle.ToString();
                MoveStage(Convert.ToInt32(BStepTxt.Text), 'Z');
                //string cycleAux = Convert.ToString(Convert.ToInt32(BStepTxt.Text), 2);
                //int lendif = 21 - cycleAux.Length;
                //for (i = 0; i < lendif; i++)
                //    cycleAux = '0' + cycleAux;
                //pos3 = new byte[] { Convert.ToByte(cycleAux.Substring(0, 7), 2) };
                //pos2 = new byte[] { Convert.ToByte(cycleAux.Substring(7, 7), 2) };
                //pos1 = new byte[] { Convert.ToByte(cycleAux.Substring(14, 7), 2) };
                ////BCycleTxt.Text = (BitConverter.ToString(pos1) + BitConverter.ToString(pos2) + BitConverter.ToString(pos3));
                //TxString = ("@" + Encoding.ASCII.GetString(session) + "Z" + Encoding.ASCII.GetString(pos1) + Encoding.ASCII.GetString(pos2) + Encoding.ASCII.GetString(pos3));
                //BStateLbl.Text = ("Moving...");
                //serialPort1.WriteLine(TxString);
            }
        }

        private void BCycleSetBtn_Click(object sender, EventArgs e)
        {
            BCycleTxt.Text = BCycleCountLbl.Text;
        }

        private void BStepMax1Btn_Click(object sender, EventArgs e)
        {
            BStepTB.Maximum = BStepTB.Maximum - 1;
            BStepMaxLbl.Text = ("Max: " + BStepTB.Maximum);
        }

        private void BStepMax2Btn_Click(object sender, EventArgs e)
        {
            BStepTB.Maximum = BStepTB.Maximum + 1;
            BStepMaxLbl.Text = ("Max: " + BStepTB.Maximum);
        }

        private void BSpeedTB_Scroll(object sender, EventArgs e)
        {                                                           // Almacena la posición de usuario del trackBar, esta es la referencia para verificar el movimiento de la plataforma
            if (!Busy)                                                                          // Envía el dato de posición en tiempo de ejecución si la bandera Busy está inactiva
            {
                string posAux = Convert.ToString(BSpeedTB.Value, 2);
                int lendif = 7 - posAux.Length;
                for (i = 0; i < lendif; i++)
                    posAux = '0' + posAux;
                pos1 = new byte[] { Convert.ToByte(posAux.Substring(0, 7), 2) };
                TxString = ("@" + Encoding.ASCII.GetString(session) + "Q" + Encoding.ASCII.GetString(pos1));
                Busy = true;
                serialPort1.WriteLine(TxString);
            }
        }

        private void uStepChkBtn_CheckedChanged(object sender, EventArgs e)
        {
            Busy = true;
            if (uStepChkBtn.Checked)
            {
                TxString = ("@" + Encoding.ASCII.GetString(session) + "W");
            }
            else
            {
                TxString = ("@" + Encoding.ASCII.GetString(session) + "U");
            }
            serialPort1.WriteLine(TxString);
        }

        private void reverseChkBtn_CheckedChanged(object sender, EventArgs e)
        {
            Busy = true;
            if (reverseChkBtn.Checked)
            {
                TxString = ("@" + Encoding.ASCII.GetString(session) + "R");
            }
            else
            {
                TxString = ("@" + Encoding.ASCII.GetString(session) + "F");
            }
            serialPort1.WriteLine(TxString);
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)             // Actions on connection, manages received requests
        {
            RxString = serialPort1.ReadExisting();
            if (!ConSuc)                                                    // Activates connection routine if no connection is stablished
            {

                Invoke(new EventHandler(Connect));
            }
            else
            {
                Invoke(new EventHandler(ComInstruction));                // Maneja informe de Error y desconexión
            }
            serialPort1.DiscardInBuffer();
        }

        private void Connect(object sender, EventArgs e)                                                // Manages on connection actions. This routine has been designed in order to avoid communnication errors (Tested on errors, the normal behavior should not have any)
        {
            if (RxString.Contains("COMSTART"))                                                          // on communication request, "COMSTART" is the identifier generated on the board. This instruction comes with an extra byte, session, which is used along the process to verify proper work.
            {
                sessionRx = new byte[] { Convert.ToByte(RxString.ElementAt(RxString.Length - 1)) };     // Extracts session byte from command
                if (sessionRx != null)
                {
                    BStateLbl.Text = (BStateLbl.Text + "\nSession ID: " + BitConverter.ToString(sessionRx));
                }
                if (BitConverter.ToString(sessionRx) == BitConverter.ToString(session))                 // Compares session, if succesful, then connect
                {
                    BStateLbl.Text = (BStateLbl.Text + "\nPort: " + serialPort1.PortName.ToString() + "\n" + serialPort1.BaudRate.ToString() + " bps\nConnection successful!!! :)");
                    ConSuc = true;
                    BSaveBtn.Enabled = true;
                    BStepMinBtn.Enabled = true;
                    BStepMaxBtn.Enabled = true;
                    BStepSetBtn.Enabled = true;
                    BCycle2Btn.Enabled = true;
                    BCycleSetBtn.Enabled = true;
                    if (CamConStatus)
                    {
                        BShutterBtn.Enabled = true;
                        StartBtn.Enabled = true;
                        ManageChkBtn.Enabled = true;
                    }
                    BStepMax1Btn.Enabled = true;
                    BStepMax2Btn.Enabled = true;
                    BSpeedTB.Enabled = true;
                    BStepTB.Enabled = true;
                    BStepLbl.Enabled = true;
                    BCycleLbl.Enabled = true;
                    BTimeLbl.Enabled = true;
                    BCycleCountLbl.Enabled = true;
                    BSpeedTBLbl.Enabled = true;
                    BStepTBLbl.Enabled = true;
                    BStepMaxLbl.Enabled = true;
                    BStepTxt.Enabled = true;
                    BCycleTxt.Enabled = true;
                    BTimeTxt.Enabled = true;
                    uStepChkBtn.Enabled = true;
                    reverseChkBtn.Enabled = true;
                }
                if (serialPort1.IsOpen)                                         // On success, manages control label, if not, sends error msg
                {
                    BConnectBtn.Text = ("Disconnect Board");
                    TxString = ("@" + Encoding.ASCII.GetString(session) + "I");
                    serialPort1.WriteLine(TxString);
                }
                else
                {
                    MessageBox.Show("COM Port error");
                }
            }

            if (RxString.Contains("DISCONNECT"))                                // Disconnect request received TODO: Check disconnection on error
            {
                //ConSuc = false;
                Invoke(new EventHandler(Disconnect));
            }

            if ((conTO < 100) & !ConSuc)                                        // Manages connection timeout, if connection is not succesful, it will reinitiate connection protocol
            {
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                conTO += 1;
                PortSel = true;
                session = new byte[] { Convert.ToByte(rnd.Next(1, 128)) };      // Reconstruct comunication request (session number regenerated)
                TxString = ("COMREQU" + Encoding.ASCII.GetString(session));
                BStateLbl.Text = ("\nAttempt" + conTO);
                getEventTxt.Text = TxString;
                getEventTxt.AppendText(BitConverter.ToString(session));
                serialPort1.WriteLine(TxString);                                // Sends communication request
            }
            if ((conTO == 100) & !ConSuc)                                       // On timeout (100 attempts) display error
            {
                BStateLbl.Text = (BStateLbl.Text + "\nConnection Failed.\nTry to reconnect to the board...");
                conTO = 101;
                TxString = ("COMERROR");
                serialPort1.WriteLine(TxString);                                // Sends error request
            }
        }

        private void Disconnect(object sender, EventArgs e)                                             // Disconnection routine
        {
            TxString = ("DISCONNECT");
            serialPort1.WriteLine(TxString);            // Send Disconnection request (board's led will blink three times)
            serialPort1.Dispose();
            serialPort1.Close();                            // Close Port and reset flags
            ConSuc = false;
            PortSel = false;

            BSpeedTB.Value = 6;                             // Manages form layout (Disable microscope control buttons) TODO: Find a more ellegant way to do this
            BStepTB.Value = 0;
            BSaveBtn.Enabled = false;
            BStepMinBtn.Enabled = false;
            BStepMaxBtn.Enabled = false;
            BStepSetBtn.Enabled = false;
            BCycle1Btn.Enabled = false;
            BCycle2Btn.Enabled = false;
            BCycleSetBtn.Enabled = false;
            BShutterBtn.Enabled = false;
            StartBtn.Enabled = false;
            BStepMax1Btn.Enabled = false;
            BStepMax2Btn.Enabled = false;
            BSpeedTB.Enabled = false;
            BStepTB.Enabled = false;
            BStepLbl.Enabled = false;
            BCycleLbl.Enabled = false;
            BTimeLbl.Enabled = false;
            BCycleCountLbl.Enabled = false;
            BSpeedTBLbl.Enabled = false;
            BStepTBLbl.Enabled = false;
            BStepMaxLbl.Enabled = false;
            BStepTxt.Enabled = false;
            BCycleTxt.Enabled = false;
            BTimeTxt.Enabled = false;
            uStepChkBtn.Enabled = false;
            reverseChkBtn.Enabled = false;
        }

        private void ComInstruction(object sender, EventArgs e)                                               // Old method for managing missconnection (initial string not proper) TODO: check if is actually doing something here
        {
            bool receivedAction = false;
            string lookup = "";
            string command = "";
            if (RxString.Length >= 4)
            {
                lookup = RxString.Substring(0, 2);
                command = RxString.Substring(2, 2);
            }
            string strSession = Encoding.ASCII.GetString(session);
            if (lookup == ("@" + strSession))
            {
                switch (command)
                {
                    case "MF":
                        receivedAction = true;
                        if (Pos == PosRef)
                        {
                            Busy = false;
                            break;
                        }
                        BStepTBLbl.Text = ("Step: " + PosRef);
                        MoveStage(BStepTB.Value, 'P');
                        break;
                    case "IF":
                        receivedAction = true;
                        Busy = false;
                        string tempstring = RxString.Substring(4, RxString.Length - 4);
                        var temparray = tempstring.Split(',');
                        BStepTxt.Text = temparray[0];
                        BCycleTxt.Text = temparray[1];
                        BTimeTxt.Text = temparray[2];
                        break;
                    case "OF":
                        receivedAction = true;
                        Busy = false;
                        if (OnCapture)
                        {
                            if (onStart)
                            {
                                StartCapture();
                                break;
                            }
                            BStateLbl.Text = (BStateLbl.Text + ("\nMove finished"));
                            onMove = true;
                            ManageFrames();
                            break;
                        }
                        break;
                    case "SF":
                        receivedAction = true;
                        Busy = true;
                        TxString = ("@" + strSession + "O");
                        Pos = 0;
                        PosRef = 0;
                        BStepTB.Value = 0;
                        BStepTBLbl.Text = ("Step: 0");
                        serialPort1.WriteLine(TxString);
                        break;
                    case "VF":
                        receivedAction = true;
                        Busy = false;
                        if (onStart)
                        {
                            CreateFolders();
                            break;
                        }
                        break;
                    case "QF":
                    case "UF":
                    case "WF":
                    case "RF":
                    case "FF":
                        receivedAction = true;
                        Busy = false;
                        break;
                    default:
                        break;
                }
            }
            if (!receivedAction)
            {
                serialPort1.WriteLine(TxString);
            }
        }

        private void MoveStage(int steps, char inst)
        {
            Pos = steps;
            byte[] bytePos = BitConverter.GetBytes(steps);
            byte instruction = Convert.ToByte(inst);
            byte[] sendthis = new byte[] { 64, session[0], Convert.ToByte(inst), bytePos[0], bytePos[1], 0x0A, 0X0A };
            BStateLbl.Text = ("Moving...");
            serialPort1.Write(sendthis, 0, 7);
            ;
        }

        bool unmanaged = false;                              // Unmanaged capture Flag
        bool OnCapture = false;
        bool onMove = false;
        bool onSave = false;
        bool onStart = false;
        int myFrame = 0;
        int myImg = 0;
        int frameCount = 0;
        string nameSave;
        string pathSave;
        int TotalFrames;
        int TotalTime;


        private void StartBtn_Click(object sender, EventArgs e)
        {
            //foreach (Control item in this.Controls)
            //{
            //    if (item is Button)
            //    {
            //        if (((Button)item).Text.ToCharArray().FirstOrDefault() == 'B')
            //            ((Button)item).Enabled = false;
            //    }
            //    if (item is Label)
            //    {
            //        if (((Label)item).Text.ToCharArray().FirstOrDefault() == 'B')
            //            ((Label)item).Enabled = false;
            //    }
            //}

            BSaveBtn.Enabled = false;
            BStepMinBtn.Enabled = false;
            BStepMaxBtn.Enabled = false;
            BStepSetBtn.Enabled = false;
            BCycle1Btn.Enabled = false;
            BCycle2Btn.Enabled = false;
            BCycleSetBtn.Enabled = false;
            BShutterBtn.Enabled = false;
            BStepMax1Btn.Enabled = false;
            BStepMax2Btn.Enabled = false;
            //BSpeedTB.Enabled = false;
            //BStepTB.Enabled = false;
            BStepLbl.Enabled = false;
            BCycleLbl.Enabled = false;
            BTimeLbl.Enabled = false;
            BCycleCountLbl.Enabled = false;
            //BSpeedTBLbl.Enabled = false;
            //BStepTBLbl.Enabled = false;
            BStepMaxLbl.Enabled = false;
            BStepTxt.Enabled = false;
            BCycleTxt.Enabled = false;
            BTimeTxt.Enabled = false;

            TotalFrames = Convert.ToInt32(BCycleTxt.Text);
            TotalTime = Convert.ToInt32(BTimeTxt.Text) * 1000;
            IntervalTmr.Interval = TotalTime;

            BStateLbl.Text = ("Sinchronizing Configuration...");
            onStart = true;
            TxString = ("@" + Encoding.ASCII.GetString(session) + "V" + BStepTxt.Text + "s" + BCycleTxt.Text + "c" + BTimeTxt.Text + "t");
            serialPort1.WriteLine(TxString);

        }

        private void captureBtn_Click(object sender, EventArgs e)
        {
            if (!OnCapture)
            {
                OnCapture = true;
                StartCapture();
            }
        }

        private void ManageChkBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (ManageChkBtn.Checked)
                unmanaged = true;
            else
                unmanaged = false;
        }

        private void CreateFolders()
        {
            BStateLbl.Text = (BStateLbl.Text + ("OK"));
            nameSave = ("Session" + BitConverter.ToString(session));
            pathSave = ("C:\\Observation\\" + nameSave);
            if (!Directory.Exists(pathSave))
            {
                DirectoryInfo di = Directory.CreateDirectory(pathSave);
                BStateLbl.Text = (BStateLbl.Text + ("\nCreating Folders..."));
            }
            for (i = 0; i <= Convert.ToInt32(BCycleTxt.Text); i++)
            {
                if (!Directory.Exists(pathSave + "\\Frame" + i.ToString("D4")))
                {
                    DirectoryInfo di = Directory.CreateDirectory(pathSave + "\\Frame" + i.ToString("D4"));
                }
            }
            BStateLbl.Text = (BStateLbl.Text + ("\nAwaiting for capture"));
            if (unmanaged)
            {
                OnCapture = true;
            }
            else
                captureBtn.Enabled = true;

            Busy = true;
            onStart = true;
            Pos = 0;
            PosRef = 0;
            BStepTB.Value = 0;
            BStepTBLbl.Text = ("Step: 0");
            TxString = ("@" + Encoding.ASCII.GetString(session) + "O");
            serialPort1.WriteLine(TxString);
        }

        private void TakePictue()
        {
            CamResponse = SendRequest("actTakePicture", "");
            string imgURL = CamResponse.Substring(20).Split('\"').FirstOrDefault();
            string TempPath = (pathSave + "\\Frame" + myFrame.ToString("D4") + "\\" + ("S") + BitConverter.ToString(session) + ("F") + myFrame.ToString("D4") + ("P") + myImg.ToString("D4") + ".jpg"); ;
            //picCount += 1;
            WebClient imageClient = new WebClient();
            imageClient.DownloadFile(imgURL, TempPath);
            //ImgAux.Image = Image.FromFile("C:\\Users\\TOSHIBA\\Documents\\Archivos doctorado\\2016-2\\Programming\\Microscope Control\\Microscope control V1.0\\microscope.gif");
            //ImgAux.ImageLocation = imgURL;
            onSave = true;
        }

        private void StartCapture()
        {
            LiveviewTmr.Interval = 100;
            onStart = false;
            if (myFrame == 0)
            {
                IntervalTmr.Enabled = true;
            }
            Thread.Sleep(500);
            TakePictue();

            if (myFrame == TotalFrames)
            {
                onMove = true;
                onSave = true;
                frameCount = TotalFrames;
                ManageFrames();
            }
            else
            {
                if (!Busy)
                {
                    Busy = true;
                    MoveStage(Convert.ToInt32(BStepTxt.Text), 'Z');
                }

            }
        }

        private void ManageFrames()
        {
            if (onMove & onSave)
            {
                if (myFrame < TotalFrames)
                {
                    myFrame += 1;
                    onMove = false;
                    onSave = false;
                    if (unmanaged)
                    {
                        StartCapture();
                    }
                    else
                    {
                        OnCapture = false;
                    }
                }
                else
                {
                    LiveviewTmr.Interval = 3;
                    Busy = true;
                    MoveStage(Convert.ToInt32(BStepTxt.Text) * TotalFrames, 'S');
                    onMove = false;
                    onSave = false;
                    OnCapture = false;
                    captureBtn.Enabled = false;
                    myImg += 1;
                    myFrame = 0;
                }
            }
        }

        private void IntervalTmr_Tick(object sender, EventArgs e)
        {
            IntervalTmr.Enabled = false;
            if (!unmanaged)
            {
                captureBtn.Enabled = true;
                SystemSounds.Beep.Play();
            }
            else
            {
                StartCapture();
                OnCapture = true;
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            try
            {
                while (bw.CancellationPending == false)
                {
                    using (var memstream = new MemoryStream())
                    {
                        imgData = new List<byte>();
                        buffer = new byte[520];
                        bufferAux = new byte[4];
                        payloadType = 0;
                        imgSize = 0;
                        frameNo = -1;
                        paddingSize = 0;

                        GetHeader:                                                          // Retrieves a byte(s) from the stream to check if it corresponds to Sony header construction

                        // Common Header (8 Bytes)
                        //buffer = new byte[520];
                        imgReader.BaseStream.Read(buffer, 0, 1);                            // Seeks for start byte
                        var start = buffer[0];
                        if (start != 0xff)
                            goto GetHeader;

                        //buffer = new byte[520];
                        imgReader.BaseStream.Read(buffer, 0, 1);                            // Stores payload Type
                        payloadType = (buffer[0]);
                        if (!((payloadType == 1) || (payloadType == 2)))
                            goto GetHeader;

                        //buffer = new byte[520];
                        imgReader.BaseStream.Read(buffer, 0, 2);                            // Stores Frame Number depending Payload type
                        if (payloadType == 1)
                            frameNo = BitConverter.ToUInt16(buffer, 0);

                        imgReader.BaseStream.Read(buffer, 0, 4);                            // Discards expected Time stamp

                        // Payload header (128 bytes)
                        //buffer = new byte[520];
                        imgReader.BaseStream.Read(buffer, 0, 4);
                        if (!((buffer[0] == 0x24) & (buffer[1] == 0x35) & (buffer[2] == 0x68) & (buffer[3] == 0x79)))
                            goto GetHeader;                                                 // If the start code does not correspond to fixed code (0x24, 0x35, 0x68, 0x79), starts over

                        //bufferAux = new byte[4];
                        imgReader.BaseStream.Read(bufferAux, 0, 4);
                        paddingSize = bufferAux[3];
                        bufferAux[3] = bufferAux[2];
                        bufferAux[2] = bufferAux[1];
                        bufferAux[1] = bufferAux[0];
                        bufferAux[0] = 0;
                        Array.Reverse(bufferAux);
                        imgSize = BitConverter.ToInt32(bufferAux, 0);                       // Reads and translates Data stream size

                        if (payloadType == 1)                                               // Case JPEG data
                        {
                            imgReader.BaseStream.Read(buffer, 0, 120);
                            while (imgData.Count < imgSize)
                            {
                                //buffer = new byte[520];
                                imgReader.BaseStream.Read(buffer, 0, 1);
                                imgData.Add(buffer[0]);
                            }
                        }

                        //getEventTxt.AppendText("Image size: " + imgData.Count.ToString());
                        MemoryStream stream = new MemoryStream(imgData.ToArray());
                        BinaryReader reader = new BinaryReader(stream);
                        Bitmap bmpImage = (Bitmap)Image.FromStream(stream);

                        if (ImgLiveview.Image != null)
                            ImgLiveview.Image.Dispose();

                        //ImgLiveview.Image = bmpImage;
                        bw.ReportProgress(0, bmpImage);
                    }
                }

            }
            catch (Exception ex)
            {

            }
            e.Cancel = true;
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            lock (locker)
                ImgLiveview.Image = (Bitmap)e.UserState;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                //resultLabel.Text = "Canceled!";
            }
            else if (e.Error != null)
            {
                //resultLabel.Text = "Error: " + e.Error.Message;
            }
            else
            {
                //resultLabel.Text = "Done!";
            }
        }

        private void ShutterBW_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            CamResponse = SendRequest("actTakePicture", "");
            string imgURL = CamResponse.Substring(20).Split('\"').FirstOrDefault();
            string name = ("Session" + BitConverter.ToString(session));
            string name2 = ("P" + picCount.ToString("D4"));
            string path = ("C:\\" + name);
            picCount += 1;
            WebClient imageClient = new WebClient();
            if (!Directory.Exists(path))
            {
                DirectoryInfo di = Directory.CreateDirectory(path);
            }

            worker.ReportProgress(10);

            imageClient.DownloadFile(imgURL, path + "\\" + name + name2 + ".jpg");
            //ImgAux.Image = Image.FromFile("C:\\Users\\TOSHIBA\\Documents\\Archivos doctorado\\2016-2\\Programming\\Microscope Control\\Microscope control V1.0\\microscope.gif");
            //ImgAux.ImageLocation = imgURL;
        }

        private void ShutterBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                //resultLabel.Text = "Canceled!";
            }
            else if (e.Error != null)
            {
                //resultLabel.Text = "Error: " + e.Error.Message;
            }
            else
            {
                //resultLabel.Text = "Done!";
            }
        }

        private void ShutterBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 10)
            {
                // Ya tome la foto

            }
        }
    }

}


