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

namespace Microscope_Control
{
    public partial class Form1 : Form
    {


        // The following Variables are related to the camera behavior

        // Type definition of Camera related variables


        List<byte> imgData = new List<byte>();              // Byte list for storing image data
        Stream imgStream;                                   // Data stream for image aquisition (Liveview)
        StreamReader imgReader;                             // Stream reader for image data (Liveview)
        int i = 0;                                          // Multipropose counter
        int imgSize = 0;                                    // Image size for data retrieval (Liveview)
        int frameNo = 0;                                    // Frame No. (Liveview)
        int paddingSize = 0;                                // Padding size (Liveview)
        bool FlagLvw = false;                               // Flag to retrieve action on liveview event                   
        bool CamConStatus = false;                          // Camera connection flag
        byte[] buffer = new byte[520];                      // Data buffer for liveview
        byte[] bufferAux = new byte[4];                     // Data auxiliar buffer for liveview
        byte payloadType = 0;                               // Stores the payload type from liveview stream
        string CamResponse = "";                            // Retrieves the camera response when any action is invoked
        string lvwURL = "";                                 // Stores camera URL for liveview
        bool shutterFlag = false;
        private static object locker = new object();

        public class RootGetEvent
        {
            public int id { get; set; }
            public JArray result { get; set; }
        }

        // The following Variables are related to the board managing and communication

        // Type definition of Stage related variables


        Random rnd = new Random();                          // Random session iniciator
        byte[] session;                                     // Byte session identifier
        byte[] sessionRx;                                   // Byte session echo
        byte[] byteRead = new byte[12];                     // Receiver byte manager
        byte[] pos1;
        bool PortSel = false;                               // Retrieves information of board connection
        bool ConSuc = false;                                // Succesful connection flag
        bool Busy = false;                                  // Activity monitoring flag
        string TxString;                                    // Data transmision string (Send this)
        string RxString;                                    // Data received string
        int conTO = 0;                                      // Timeout connection by attempts
        int Pos = 0;                                        // Position verifier
        int PosRef = 0;                                     // Position reference
        int Cycle = 0;                                      // Cycle verifier
        int PosAux = 0;                                     // Position verifier (Auxiliar)
        int PosRefAux = 0;                                  // Position reference (Auxiliar)
        int CycleAux = 0;                                   // Cycle verifier (Auxiliar)


        // The following Variables are related to the automated observation

        // Type definition of Automated observation


        bool unmanaged = false;                              // Unmanaged capture Flag
        bool OnCapture = false;
        bool onMove = false;
        bool onSave = false;
        bool onStart = false;
        bool onAuxiliar = false;
        int myFrame = 0;
        int myImg = 0;
        int frameCount = 0;
        int picCount = 0;
        string nameSave;
        string pathSave;
        int TotalFrames;
        int TotalTime;


        public Form1()
        {
            InitializeComponent();
            //CheckForIllegalCrossThreadCalls = false;
            ImgGuide.BackColor = Color.Transparent;
            ImgGuide.Parent = ImgLiveview;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //LiveviewBW.RunWorkerAsync();
            BConnectionCBox.Items.Add("Port selection");
            BConnectionCBox.SelectedIndex = 0;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)                   // On close, zoom camera out, disconnect board
        {
            if (CamConStatus)
                CamResponse = SendRequest("actZoom", "\"out\",\"start\"");
            if (serialPort1.IsOpen == true)
            {
                Invoke(new EventHandler(Disconnect));
            }
        }


        // The following code is (Mostly) related to the managing of the Camera
        //      TODO:
        //              -


        private void ConnectBtn_Click(object sender, EventArgs e)                               // Manages the discovery routine to connect with camera DSC-QX10 (Must be connected to PC WiFi)
        {
            try
            {
                ConnectionTxt.Visible = true;
                ConnectBtn.Enabled = false;

                // Setup Client/Host Endpoints and communication socket
                IPEndPoint LocalEndPoint = new IPEndPoint(IPAddress.Any, 60000);                                        // Creates Endpoint to connect with system client
                IPEndPoint MulticastEndPoint = new IPEndPoint(IPAddress.Parse("239.255.255.250"), 1900);                // Creates Endpoint to connect with camera host (Multicast messages reserved address, Sony SDK)
                Socket UdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);          // Creates Socket for managing network communication
                UdpSocket.Bind(LocalEndPoint);                                                                          // Asociates Local socket to external host (Camera)
                ConnectionTxt.Text = ("Status\r\n\r\nUDP-Socket setup finished...\r\n");

                // Sends discovery request to camera host (SSDP M-SEARCH)
                string SearchString = "M-SEARCH * HTTP/1.1\r\nHOST:239.255.255.250:1900\r\nMAN:\"ssdp:discover\"\r\nMX:2\r\nST:urn:schemas-sony-com:service:ScalarWebAPI:1\r\n\r\n";
                // SSDP M-SEARCH request (SONY SDK) string
                UdpSocket.SendTo(Encoding.UTF8.GetBytes(SearchString), SocketFlags.None, MulticastEndPoint);            // Sends M-SEARCH request (8-bit Unicode) UNICAST
                ConnectionTxt.AppendText("M-Search sent\r\n");

                // Receives discovery response from camera UNICAST (TimedOut on 10 secs)
                byte[] ReceiveBuffer = new byte[64000];
                int ReceivedBytes = 0;
                Thread TimeoutThread = new Thread(ThreadProc);
                TimeoutThread.Start();
                i = 0;
                while (TimeoutThread.IsAlive)                                                                                            // Received Buffered response
                {
                    i += 1;
                    if (i % 950000 == 0)
                    {
                        ConnectionTxt.AppendText("█");
                    }
                    if (UdpSocket.Available > 0)
                    {
                        ConnectionTxt.AppendText("r\\nConnection established\n");
                        ReceivedBytes = UdpSocket.Receive(ReceiveBuffer, SocketFlags.None);

                        if (ReceivedBytes > 0)
                        {
                            ConnectionTxt.AppendText(Encoding.UTF8.GetString(ReceiveBuffer, 0, ReceivedBytes));
                            CamConStatus = true;
                        }
                        break;
                    }
                }
                TimeoutThread.Abort();
                if (CamConStatus)
                {
                    // Setups form objects OnConnect, Zooms camera in and sends request toreceive full resolution images.
                    ConnectionTxt.AppendText("\r\n\rConnection successful =)  \n");
                    CamResponse = SendRequest("setPostviewImageSize", "\"Original\"");
                    CamResponse = SendRequest("actZoom", "\"in\",\"start\"");
                    getEventTxt.Text = CamResponse;

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
                    ImgGuide.Location = new Point(0, 0);
                    //**************** Control Visualization *************
                    ConnectionTxt.Visible = false;
                    ConnectionTxt.Text = "";
                    LiveviewBtn.Enabled = true;
                    resolutionChkBtn.Enabled = true;
                    HPShutterChkBtn.Enabled = true;
                    if (ConSuc)
                    {
                        focusTB.Visible = true;
                        focusLbl.Visible = true;
                        BShutterBtn.Enabled = true;
                        StartBtn.Enabled = true;
                        ManageChkBtn.Enabled = true;
                    }
                    //******************************************************
                }
                else
                {
                    ConnectBtn.Enabled = true;
                    ConnectionTxt.AppendText("\r\n\r\nFailed: Connection TimedOut =(  \n");
                    UdpSocket.Close();
                }
            }
            catch (Exception ex)
            {
                ConnectionTxt.Text = ex.Message;
            }
        }

        private void LiveviewBtn_Click(object sender, EventArgs e)                              // Manages beginning/end of liveview
        {
            if (!FlagLvw)
            {
                // Start liveview Background Worker (Send HTTP GET request, calls Liveview handler)
                FlagLvw = true;
                ImgLiveview.Visible = true;
                CamResponse = SendRequest("startLiveview", "");                             // Send action request to camera host to start liveview
                lvwURL = ReadRequestJson(CamResponse,0);                                    // Setup the URL for the liveview download
                WebRequest lvwRequest = WebRequest.Create(lvwURL);                          // Create a request using the camera liveview URL, send HTTP GET request
                lvwRequest.Method = "GET";
                lvwRequest.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
                imgStream = lvwRequest.GetResponse().GetResponseStream();                   // Setup and get the request stream response
                imgReader = new StreamReader(imgStream);
                if (LiveviewBW.IsBusy != true)
                    LiveviewBW.RunWorkerAsync();
                guideChkBtn.Enabled = true;
            }
            else
            {
                ImgLiveview.Visible = false;
                FlagLvw = false;
                LiveviewBW.CancelAsync();
                CamResponse = SendRequest("stopLiveview", "");                              // Send action request to camera host to stop liveview
                guideChkBtn.Enabled = false;
                imgStream.Close();
                imgReader.Close();
            }
            ConnectionTxt.AppendText(CamResponse + "\r\n");
        }

        private void guideRefreshBtn_Click(object sender, EventArgs e)                          // Loads image from live view to be frozen and displayed as a guide frame
        {
            ImgGuide.Location = new Point(0, 0);                                                // Ensures the reference image frame is in place
            Bitmap referenceImg;
            lock (locker)                                                                       // Calls lock on objects (necessary for avoiding issues on image load)
                referenceImg = new Bitmap(ImgLiveview.Image);
            Bitmap transparentImg = new Bitmap(referenceImg.Width, referenceImg.Height);        // Aquires Image from Liveview
            Graphics tempG = Graphics.FromImage(referenceImg);
            Color c = Color.Transparent;
            Color v = Color.Transparent;
            for (int x = 0; x < referenceImg.Width; x++)                                        // Sweeps image pixels to change opacity
            {
                for (int y = 0; y < referenceImg.Height; y++)
                {
                    c = referenceImg.GetPixel(x, y);
                    v = Color.FromArgb(60, c.R, c.G, c.B);
                    transparentImg.SetPixel(x, y, v);
                }
            }
            tempG.DrawImage(transparentImg, Point.Empty);                                       // Loads Tranparent(ed) image on ImgGuide
            ImgGuide.Image = transparentImg;
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

        private void resolutionChkBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (resolutionChkBtn.Checked)
            {
                CamResponse = SendRequest("setPostviewImageSize", "\"Original\"");
            }
            else
            {
                CamResponse = SendRequest("setPostviewImageSize", "\"2M\"");
            }
        }

        private void HPShutterChkBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (HPShutterChkBtn.Checked)
            {
                CamResponse = SendRequest("actHalfPressShutter");
            }
            else
            {
                CamResponse = SendRequest("cancelHalfPressShutter");
            }
        }

        private string SendRequest(params string[] data)                                        // Gives format to the action request, manages sending request and receiving response. Output: Response JSON string
        {
            Array.Resize(ref data, 3);                                                                              // Arrange input data (Arranges a 3-item array)
            string method = data[0];                                                                                // Sets default values for parameters and version
            string param = ("");
            string version = ("1.0");
            if (data[1] != null)                                                                                    // Assigns input values (If any)
            {
                param = data[1];
            }
            if (data[2] != null)
            {
                version = data[2];
            }
            string responseF;                                                                                       // String for storing camera response (Return)
            try
            {
                // Create POST data and convert it to a byte array (Set the ContentType property of the WebRequest to an 8-bit Unicode). Data is not Serialized to JSON due that params (required property) is a C# keyword
                string postData = "{\"method\": \"" + method + "\",\"params\": [" + param + "],\"id\": 1,\"version\": \"" + version + "\"}";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                // Send action request
                WebRequest request = WebRequest.Create("http://10.0.0.1:10000/sony/camera ");                       // Create a request using the camera Action list URL
                request.Method = "POST";                                                                            // Set the Method property of the request to POST
                request.ContentType = "application/json; charset=utf-8";                                            // Set the request content type to match JSON encoding
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
        
        private string ReadRequestJson(string json, int order, string key, int item)            // Reads JSON format and returns specified property: 
        {
            RootGetEvent myjson = JsonConvert.DeserializeObject<RootGetEvent>(json);
            string property = myjson.result[order][key][item].ToString();
            return property;
        }
        private string ReadRequestJson(string json, int order, string key)                      //      Uses the JSON string, the order number and the string key (Ref. Sony remote camera API reference document) 
        {
            RootGetEvent myjson = JsonConvert.DeserializeObject<RootGetEvent>(json);
            string property = myjson.result[order][key].ToString();
            return property;
        }
        private string ReadRequestJson(string json, int order, int key)                         //      Uses the JSON string, the order number and number key (Ref. Sony remote camera API reference document)
        {
            RootGetEvent myjson = JsonConvert.DeserializeObject<RootGetEvent>(json);
            string property = myjson.result[order][key].ToString();
            return property;
        }
        private string ReadRequestJson(string json, int order)                                  //      Uses the JSON string and the order number (Ref. Sony remote camera API reference document)
        {
            RootGetEvent myjson = JsonConvert.DeserializeObject<RootGetEvent>(json);
            string property = myjson.result[order].ToString();
            return property;
        }
        private string ReadRequestJson(string json)                                             //      Uses only the JSON string (Ref. Sony remote camera API reference document)
        {
            RootGetEvent myjson = JsonConvert.DeserializeObject<RootGetEvent>(json);
            string property = myjson.result.ToString();
            return property;
        }

        private static void ThreadProc()                                                        // Connection timer, manages timeout OnConnection to cammera
        {
            Thread.Sleep(10000);
        }

        private void LiveviewBW_DoWork(object sender, DoWorkEventArgs e)                        // Request liveview image, reads and stores image 
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

                        //if (ImgLiveview.Image != null)
                        //    ImgLiveview.Image.Dispose();

                        //ImgLiveview.Image = bmpImage;
                        if (bmpImage != null)
                            bw.ReportProgress(0, bmpImage);
                    }
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                // ConnectionTxt.Text = ex.Message;
            }
            e.Cancel = true;
        }

        private void LiveviewBW_ProgressChanged(object sender, ProgressChangedEventArgs e)      // Retrieves liveview image and loads to screen on load completition
        {
            lock (locker)
                ImgLiveview.Image = (Bitmap)e.UserState;
        }

        private void LiveviewBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
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

        private void ShutterBW_DoWork(object sender, DoWorkEventArgs e)                         // Sends HTTP GET request for camera shutter (Manages saving image)
        {
            if (LiveviewBW.IsBusy != true)
                LiveviewBW.CancelAsync();
            BackgroundWorker worker = sender as BackgroundWorker;
            WebClient imageClient = new WebClient();                                                // Initializes webclient for image managing
            onSave = true;                                                                          // Sets OnSave flag
            CamResponse = SendRequest("actTakePicture", "");                                        // Sends HTTP GET request, retrieves image URL
            string imgURL = ReadRequestJson(CamResponse, 0, 0);                                            //NON JSON SOLUTION: CamResponse.Substring(20).Split('\"').FirstOrDefault();
            string name = "";
            string path = "";
            if (shutterFlag)                                                                        // On shutter action, saves file to location
            {
                name = ("P" + picCount.ToString("D4") + ".jpg");
                path = ("C:\\Observation\\Session" + BitConverter.ToString(session) + "Shutter");
                picCount += 1;
            }
            else                                                                                    // On automatic observation, saves file to location
            {
                name = (("S") + BitConverter.ToString(session) + ("F") + myFrame.ToString("D3") + ("P") + myImg.ToString("D3") + ".jpg");
                path = ("C:\\Observation\\Session" + BitConverter.ToString(session) + "\\Frame" + myFrame.ToString("D4"));
            }
            if (!Directory.Exists(path))                                                            // Check requested directory exists, if not, creates it
            {
                DirectoryInfo di = Directory.CreateDirectory(path);
            }
            worker.ReportProgress(10);                                                              // If action needed on shutter use this
            if (LiveviewBW.IsBusy != true)
                LiveviewBW.RunWorkerAsync();

            imageClient.DownloadFile(imgURL, path + "\\" + name);                                   // Saves File
        }

        private void ShutterBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) // On imagecapture completition: Manages action
        {
            BStateLbl.Text += ("\nImage saved");
            if (shutterFlag)                                                                            // On shutter action, manages form objects
            {
                BShutterBtn.Enabled = true;
                shutterFlag = false;
            }
            else                                                                                        // On automated observation, manages frame count, image count and stage movement
            {
                if (myFrame == TotalFrames)                                                             // Frames, completed
                {
                    onMove = true;
                    onSave = true;
                    frameCount = TotalFrames;
                    ManageFrames();
                }
                else                                                                                    // On picture, move stage
                {
                    if (!Busy)
                    {
                        if (BMAuxChkBtn.Checked == true)
                        {
                            Busy = true;
                            MoveStage(Convert.ToInt32(BAStepTxt.Text), 'Z', '~');
                        }
                        else
                        {
                            Busy = true;
                            MoveStage(Convert.ToInt32(BStepTxt.Text), 'Z', '@');
                        }
                    }

                }
            }

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

        private void ShutterBW_ProgressChanged(object sender, ProgressChangedEventArgs e)       // If action needed on shutter use this     
        {
            if (e.ProgressPercentage == 10)
            {
                BStateLbl.Text += ("\nSaving Image...");

            }
        }


        // These events are provided for test purposes only Any release: please leave these as NOT VISIBLE (Or not visible)
        //      TODO:
        //              -


        private void getEventBtn_Click(object sender, EventArgs e)                              // Test Button (Not visible) Requests Events to camera
        {
            //************* Reads JSON format (Returns camera status) *********************
            string json = SendRequest("getEvent", "false", "1.1");
            var myjson = JsonConvert.DeserializeObject<RootGetEvent>(json);
            textBox1.Text = ("");
            //************* Visualizes JSON response **************************************
            for (i = 0; i < myjson.result.Count; i++)
            {
                if ((myjson.result[i]) != null)
                {
                    textBox1.AppendText("\r\n" + i + ")\r\n");
                    textBox1.AppendText(myjson.result[i].ToString());
                }
            }
            //************* Visualizes Camera status **************************************
            textBox2.Text = myjson.result[1]["cameraStatus"].ToString();
        }

        private void TestBtn_Click(object sender, EventArgs e)                                  // Test Button (Not available) <INSERT YOUR TEST CODE HERE>
        {
            //************ Take picture ************************************
            //CamResponse = SendRequest("actTakePicture", "");

            //************ Check received info from board ******************
            BStateLbl.Text = RxString;
        }

        private void timer1_Tick(object sender, EventArgs e)                                    // Test timer (Not available) <INSERT YOUR TEST CODE HERE>
        {
            progressBar1.Value += 1; 
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
        //********************* Move Instuction and response command set *********************
        //
        //      Send: Minimum 3 bytes
        //          Header 2 bytes
        //              1st byte: Identifier for main or auxiliar motor (@ or ~)
        //              2nd byte: ASCII Encoded session ID (0-127)
        //
        //          Instruction
        //              1st byte: Command byte (Instruction)
        //              Extra: Depending on function
        //
        //              P: Movement request (Managed on MoveStage function)
        //                  Extra: 3 bytes
        //                      1st byte: Position High byte
        //                      2nd byte: Position Low byte
        //                      3rd byte: Data carrier 0x0A
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
        //              S: Movement forward cycle request (Managed on MoveStage function)
        //                  Extra: 2 bytes
        //                      1st byte: Position High byte
        //                      2nd byte: Position Low byte
        //                      3rd byte: Data carrier 0x0A
        //                  Received: SF
        //              Z: Movement backwards cycle request (Managed on MoveStage function)
        //                  Extra: 2 bytes
        //                      1st byte: Position High byte
        //                      2nd byte: Position Low byte
        //                      3rd byte: Data carrier 0x0A
        //                  Received: SF
        //              V: Save memory request (Data on TextBoxes)
        //                  Extra: 8 bytes
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
        //                  Extra: 1 byte
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
        //              A: Connect Auxiliar Motor
        //                  Received: AF
        //              D: Disconnect Auxiliar Motor
        //                  Received: DF
        //              L: Move Focus Servo
        //                  Received: LF                            
        //***********************************************************************************
        //      TODO:
        //              - if no received action, request torepeat information. Then, if error, send again
        //              - Setup actions on board communication session error


        private void comboBox1_DropDown(object sender, EventArgs e)                             // Sniffs for serial ports connected to the computer (Arduino connects vias Serial)
        {
            string[] ports = SerialPort.GetPortNames();                                             // Sniffs for connected ports
            BConnectionCBox.Items.Clear();                                                          // Cleans previous data in Combobox
            BConnectionCBox.Items.Add("Port selection");
            BConnectionCBox.SelectedIndex = 0;
            foreach (string port in ports)                                                          // Adds available ports to the Combobox's list
            {
                BConnectionCBox.Items.Add(port);
            }
        }

        private void BConnectionCBox_SelectedIndexChanged(object sender, EventArgs e)           // Enables connection button on serial type port selection (i.e. if a serial port is selecten in the combo box)
        {
            if (BConnectionCBox.Text.Contains("COM"))
                BConnectBtn.Enabled = true;
            else
                BConnectBtn.Enabled = false;
        }

        private void BConnectBtn_Click(object sender, EventArgs e)                              // Starts connection routine (No error handling)
        {
            conTO = 0;
            BStateLbl.Text = ("Status");
            if (BConnectionCBox.Text.Contains("COM") && !PortSel)                   // Allows connection if a valid COM port is connected and sets a flag for port selected
            {
                PortSel = true;
            }
            if (PortSel)
            {
                if (!serialPort1.IsOpen & PortSel)                             // If port is closed, and a valid serial port is selected, allow connection
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
                else if (serialPort1.IsOpen)                                             // If port is open, close port (Manages controller labels)
                {
                    BStateLbl.Text = ("Disconnected...");
                    Invoke(new EventHandler(Disconnect));
                    BConnectBtn.Text = ("Connect Board");
                }
            }
        }

        private void BShutterBtn_Click(object sender, EventArgs e)                              // Starts a shutter routine, calls Background Worker
        {
            shutterFlag = true;
            BShutterBtn.Enabled = false;
            ShutterBW.RunWorkerAsync();
        }

        private void BSaveBtn_Click(object sender, EventArgs e)                                 // Saves data on calibration
        {
            Busy = true;
            byte[] byteStep = BitConverter.GetBytes(Convert.ToInt16(BStepTxt.Text));
            byte[] byteCycle = BitConverter.GetBytes(Convert.ToInt16(BCycleTxt.Text));
            byte[] byteTime = BitConverter.GetBytes(Convert.ToInt16(BTimeTxt.Text));
            byte[] byteStepAux = BitConverter.GetBytes(Convert.ToInt16(BAStepTxt.Text));
            byte[] sendthis = new byte[] { 64, session[0], Convert.ToByte('V'), byteStep[0], byteStep[1], byteCycle[0], byteCycle[1], byteTime[0], byteTime[1], byteStepAux[0], byteStepAux[1], 0X0A };
            serialPort1.Write(sendthis, 0, sendthis.Length);
        }

        private void BStepTB_Scroll(object sender, EventArgs e)
        {
            PosRef = BStepTB.Value;                                                                 // Stores user position of the Trackbar, this is the position reference to verify the stage movement
            textBox2.Text = PosRef.ToString();
            if (!Busy)                                                                              // Send data in execution timeif busy flag is false (When position is not fully attained, the program will check board reported position and stored position and send the difference)
            {
                Busy = true;                                                                        // Sets busy flag
                BStepTBLbl.Text = ("Step: " + PosRef);                                              // Update position on visualization
                MoveStage(BStepTB.Value, 'P', '@');
                Pos = BStepTB.Value;                                                                // Request stage movement (managed by MoveStage function)
            }
        }

        private void BStepMinBtn_Click(object sender, EventArgs e)                              // Sends board request and sets current Trackbar position as Origin
        {
            Busy = true;                                                                            // Sets busy flag
            TxString = ("@" + Encoding.ASCII.GetString(session) + "O");                             // Formats Reset position board request
            Pos = 0;
            PosRef = 0;
            BStepTB.Value = 0;
            BStepTBLbl.Text = ("Step: 0");                                                          // Updates position visualization
            serialPort1.WriteLine(TxString);                                                        // Send Reset position board request
        }

        private void BStepMaxBtn_Click(object sender, EventArgs e)                              // Sets current Trackbar position as Max Step position
        {
            BStepTB.Maximum = BStepTB.Value;                                                        // Retrieves Trackbar current position
            BStepMaxLbl.Text = ("Max: " + BStepTB.Maximum);                                         // Updates position visualization
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
        }

        private void BCycle1Btn_Click(object sender, EventArgs e)                               // Sends board request to move a complete cycle (Backwards)
        {
            if (!Busy)
            {
                Busy = true;                                                                        // Sets busy flag
                if (BCycleCountLbl.Text == "1")                                                     // Allow only positive movement
                {
                    BCycle1Btn.Enabled = false;
                }
                Cycle = Cycle - 1;
                BCycleCountLbl.Text = Cycle.ToString();
                MoveStage(Convert.ToInt32(BStepTxt.Text), 'S', '@');                                     // Request cycle movement though MoveStage function
            }
        }

        private void BCycle2Btn_Click(object sender, EventArgs e)                               // Sends board request to move a complete cycle (Foward)
        {
            if (!Busy)
            {
                Busy = true;                                                                        // Sets busy flag
                if (BCycleCountLbl.Text == "0")                                                     // Enables for positive movement
                {
                    BCycle1Btn.Enabled = true;
                }
                Cycle = Cycle + 1;
                BCycleCountLbl.Text = Cycle.ToString();
                MoveStage(Convert.ToInt32(BStepTxt.Text), 'Z', '@');                                     // Request cycle movement though MoveStage function
            }
        }

        private void BCycleSetBtn_Click(object sender, EventArgs e)                             // Updates step setup to current step count
        {
            BCycleTxt.Text = BCycleCountLbl.Text;
        }

        private void BStepSetBtn_Click(object sender, EventArgs e)                              // Updates step setup to current Trackbar position
        {
            BStepTxt.Text = BStepTB.Value.ToString();
        }

        private void BSpeedTB_Scroll(object sender, EventArgs e)                                // Sends board request for changing stage moving speed on execution time
        {
            if (!Busy)
            {
                string posAux = Convert.ToString(BSpeedTB.Value, 2);                                // Formats data to ASCII encoding
                int lendif = 7 - posAux.Length;
                for (i = 0; i < lendif; i++)                                                        // Encodes position
                    posAux = '0' + posAux;
                pos1 = new byte[] { Convert.ToByte(posAux.Substring(0, 7), 2) };
                TxString = ("@" + Encoding.ASCII.GetString(session) + "Q" + Encoding.ASCII.GetString(pos1));    // Builds instruction (Formatting)
                Busy = true;                                                                        // Sets busy flag
                serialPort1.WriteLine(TxString);                                                    // Sends board request for changing stage movement speed
            }
        }

        private void uStepChkBtn_CheckedChanged(object sender, EventArgs e)                     // Sends board request for uStep activation (Format and sends request depending case activation/deactivation)
        {
            if (ConSuc)
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
        }

        private void reverseChkBtn_CheckedChanged(object sender, EventArgs e)                   // Sends board request for reverse activation (Format and sends request depending case forward/backwards)
        {
            if (ConSuc)
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
        }
        
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)     // Actions on connection, manages received requests
        {
            Array.Resize(ref byteRead, serialPort1.BytesToRead);
            serialPort1.Read(byteRead, 0, serialPort1.BytesToRead);
            RxString = Encoding.UTF8.GetString(byteRead);
            if (!ConSuc)                                                                            // Activates connection routine if no connection is stablished
            {

                Invoke(new EventHandler(Connect));
            }
            else
            {
                Invoke(new EventHandler(ComInstruction));                                           // Manages connection//deconnection error report
            }
            serialPort1.DiscardInBuffer();
        }

        private void Connect(object sender, EventArgs e)                                        // Manages on connection actions. This routine has been designed in order to avoid communnication errors (Tested on errors, the normal behavior should not have any)
        {
            if (RxString.Contains("COMSTART"))                                                      // on communication request, "COMSTART" is the identifier generated on the board. This instruction comes with an extra byte, session, which is used along the process to verify proper work.
            {
                sessionRx = new byte[] { Convert.ToByte(RxString.ElementAt(RxString.Length - 1)) }; // Extracts session byte from command
                if (sessionRx != null)
                {
                    BStateLbl.Text = (BStateLbl.Text + "\nSession ID: " + BitConverter.ToString(sessionRx));
                }
                if (BitConverter.ToString(sessionRx) == BitConverter.ToString(session))             // Compares session, if succesful, then connect
                {
                    BStateLbl.Text = (BStateLbl.Text + "\nPort: " + serialPort1.PortName.ToString() + "\n" + serialPort1.BaudRate.ToString() + " bps\nConnection successful!!! :)");
                    ConSuc = true;

                    // Form Object visualization routine
                    foreach (Control control in BoardPanel.Controls)
                    {
                        control.Enabled = true;
                    }
                    if (CamConStatus)
                    {
                        focusTB.Visible = true;
                        focusLbl.Visible = true;
                        BShutterBtn.Enabled = true;
                        StartBtn.Enabled = true;
                        ManageChkBtn.Enabled = true;
                    }
                }
                if (serialPort1.IsOpen)                                                             // On success, manages control label and request information (step, cycle and time), if not, sends error msg
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

            if (RxString.Contains("DISCONNECT"))                                                    // Disconnect request received TODO: Check disconnection on error
            {
                Invoke(new EventHandler(Disconnect));
            }

            if ((conTO < 100) & !ConSuc)                                                            // Manages connection timeout, if connection is not succesful, it will reinitiate connection protocol
            {
                serialPort1.DiscardInBuffer();
                serialPort1.DiscardOutBuffer();
                conTO += 1;
                PortSel = true;
                session = new byte[] { Convert.ToByte(rnd.Next(1, 128)) };                          // Reconstruct comunication request (session number regenerated)
                TxString = ("COMREQU" + Encoding.ASCII.GetString(session));
                BStateLbl.Text = ("Status\nAttempts: " + conTO);
                getEventTxt.Text = TxString;
                getEventTxt.AppendText(BitConverter.ToString(session));
                serialPort1.WriteLine(TxString);                                                    // Sends communication request
            }
            if ((conTO == 100) & !ConSuc)                                                           // On timeout (100 attempts) display error
            {
                BStateLbl.Text = (BStateLbl.Text + "\nConnection Failed.\nTry to reconnect to the board...");
                conTO = 101;
                TxString = ("COMERROR");
                serialPort1.WriteLine(TxString);                                                    // Sends error request
            }
        }

        private void Disconnect(object sender, EventArgs e)                                     // Disconnection routine
        {
            TxString = ("DISCONNECT");
            serialPort1.WriteLine(TxString);                                                        // Send Disconnection request (board's led will blink three times)
            serialPort1.Dispose();
            serialPort1.Close();                                                                    // Close Port and reset flags
            ConSuc = false;
            PortSel = false;

            // Form Object visualization routine
            foreach (Control control in BoardPanel.Controls)
            {
                control.Enabled = false;
                if (control is CheckBox)
                    ((CheckBox)control).Checked = false;
                if (control is TextBox)
                    ((TextBox)control).Text = ("");
            }
            Cycle = 0;
            BSpeedTB.Value = 3;                                                                     // Manages form layout (Disable microscope control buttons) TODO: Find a more ellegant way to do this
            BStepTB.Value = 0;
            BStepTB.Maximum = 100;
            BStepTBLbl.Text = ("Step:");
            BCycleCountLbl.Text = ("0");
            CycleAux = 0;
            BASpeedTB.Value = 3;                                                                     // Manages form layout (Disable microscope control buttons) TODO: Find a more ellegant way to do this
            BAStepTB.Value = 0;
            BAStepTB.Maximum = 100;
            BAStepTBLbl.Text = ("Step:");
            BACycleCountLbl.Text = ("0");
            BShutterBtn.Enabled = false;
            StartBtn.Enabled = false;
            ManageChkBtn.Enabled = false;
            focusTB.Visible = false;
            focusLbl.Visible = false;
        }

        private void ComInstruction(object sender, EventArgs e)                                 // Manages received instructions from board (and actions on request)
        {
            bool receivedAction = false;
            string lookup = "";
            string command = "";
            if (RxString.Length >= 4)                                                               // Reads connection encoding and instruction
            {
                lookup = RxString.Substring(0, 2);
                command = RxString.Substring(2, 2);
            }
            string strSession = Encoding.ASCII.GetString(session);                                  // Retrieves session byte to check proper connection
            if (lookup == ("@" + strSession))
            {
                switch (command)                                                                    // Reads command and checks action (or none)
                {
                    case "MF":                                                                      // Move Finished (Answers to 'P' request)
                        textBox1.Text = (Pos + ", " + PosRef);
                        receivedAction = true;
                        if (Pos == PosRef)                                                          // Check if position is up-to-date
                        {
                            Busy = false;
                            BStateLbl.Text = ("Move Finished");
                            break;
                        }
                        BStepTBLbl.Text = ("Step: " + PosRef);                                      // Moves stage if position has not been reached (particularly useful when movement is slow)
                        MoveStage(BStepTB.Value, 'P', '@');                                              // Sends movement request to board
                        break;
                    case "IF":                                                                      // Information received
                        if (byteRead.Length<12)
                        {
                            Busy = true;
                            receivedAction = false;
                            break;
                        }
                        receivedAction = true;
                        Busy = false;
                        string[] tempstring= new string[4];                                         // Decode and allocate data
                        tempstring[0] = BitConverter.ToUInt16(byteRead,4).ToString();
                        tempstring[1] = BitConverter.ToUInt16(byteRead, 6).ToString();
                        tempstring[2] = BitConverter.ToUInt16(byteRead, 8).ToString();
                        tempstring[3] = BitConverter.ToUInt16(byteRead, 10).ToString();
                        BStepTxt.Text = tempstring[0];
                        BCycleTxt.Text = tempstring[1];
                        BTimeTxt.Text = tempstring[2];
                        BAStepTxt.Text = tempstring[3];
                        break;
                    case "OF":                                                                      // Origin stablished
                        receivedAction = true;
                        Busy = false;
                        if (OnCapture)                                                              // If OnCapture continue automatic routine
                        {
                            if (onStart)
                            {
                                StartCapture();                                                     // If OnStart (first image of the automated observation), initiate capture
                                break;
                            }
                            BStateLbl.Text = (BStateLbl.Text + ("\nMove finished"));
                            onMove = true;
                            ManageFrames();
                            break;
                        }
                        break;
                    case "SF":                                                                      // Cycle completed (Then sends board request for stablishing origin)
                        receivedAction = true;
                        Busy = true;
                        TxString = ("@" + strSession + "O");
                        Pos = 0;
                        PosRef = 0;
                        BStepTB.Value = 0;
                        BStepTBLbl.Text = ("Step: 0");
                        serialPort1.WriteLine(TxString);
                        break;
                    case "VF":                                                                      // Save completed
                        receivedAction = true;
                        Busy = false;
                        if (onStart)
                        {
                            CreateFolders();
                            break;
                        }
                        break;
                    case "QF":                                                                      // Completed speed selection
                    case "UF":                                                                      // Completed normal step selection
                    case "WF":                                                                      // Completed uStep selection
                    case "RF":                                                                      // Completed reverse direction selection
                    case "FF":                                                                      // Completed forward direction selection
                    case "LF":
                        receivedAction = true;
                        Busy = false;
                        break;
                    default:
                        break;
                }
            }
            //***************************** Auxiliar Motor *********************************
            if (lookup == ("~" + strSession))
            {
                switch (command)                                                                    // Reads command and checks action (or none)
                {
                    case "MF":                                                                      // Move Finished (Answers to 'P' request)
                        textBox1.Text = (PosAux + ", " + PosRefAux + " AUX");
                        receivedAction = true;
                        if (PosAux == PosRefAux)                                                          // Check if position is up-to-date
                        {
                            Busy = false;
                            BStateLbl.Text = ("Move Finished (Aux)");
                            break;
                        }
                        BAStepTBLbl.Text = ("Step: " + PosRefAux);                                      // Moves stage if position has not been reached (particularly useful when movement is slow)
                        MoveStage(BAStepTB.Value, 'P', '~');                                              // Sends movement request to board
                        break;
                    case "OF":                                                                      // Origin stablished
                        receivedAction = true;
                        Busy = false;
                        if (OnCapture & BMAuxChkBtn.Checked == true)                                                              // If OnCapture continue automatic routine
                        {
                            if (onStart)
                            {
                                TxString = ("@" + Encoding.ASCII.GetString(session) + "O");
                                serialPort1.WriteLine(TxString);                                                     // If OnStart (first image of the automated observation), initiate capture
                                break;
                            }
                            Busy = true;
                            MoveStage(Convert.ToInt32(BStepTxt.Text), 'Z', '@');
                            break;
                        }
                        if (onAuxiliar==true)
                        {
                            onAuxiliar = false;
                            MoveStage(Convert.ToInt32(BStepTxt.Text) * TotalFrames, 'S', '@');
                        }
                        break;
                    case "SF":                                                                      // Cycle completed (Then sends board request for stablishing origin)
                        receivedAction = true;
                        Busy = true;
                        TxString = ("~" + strSession + "O");
                        PosAux = 0;
                        PosRefAux = 0;
                        BAStepTB.Value = 0;
                        BAStepTBLbl.Text = ("Step: 0");
                        serialPort1.WriteLine(TxString);
                        break;
                    case "QF":                                                                      // Completed speed selection
                    case "UF":                                                                      // Completed normal step selection
                    case "WF":                                                                      // Completed uStep selection
                    case "RF":                                                                      // Completed reverse direction selection
                    case "FF":                                                                      // Completed forward direction selection
                    case "AF":
                    case "DF":
                        receivedAction = true;
                        Busy = false;
                        break;
                    default:
                        break;
                }
            }
            //*****************************
            if (!receivedAction)                                                                    // If no correct response from board is received, send again board request
            {
                serialPort1.WriteLine(TxString);
            }
        }

        private void MoveStage(int steps, char inst, char ID)                                            // Manages stage movement (used in board request 'P', 'S' and 'Z')
        {
            if (ID == '@')
            {
                Pos = steps;
                BStateLbl.Text += ("\nMoving main...");
            }
            else if (ID == '~')
            { 
                PosAux = steps;
                BStateLbl.Text += ("\nMoving auxiliar...");
            }
            byte[] bytePos = BitConverter.GetBytes(steps);
            byte[] sendthis = new byte[] { Convert.ToByte(ID), session[0], Convert.ToByte(inst), bytePos[0], bytePos[1],0X0A};
            serialPort1.Write(sendthis, 0, sendthis.Length);
        }

        //***************** Auxiliary motor ******************************
        //              TODO: Organize comments

        private void BMAuxChkBtn_CheckedChanged(object sender, EventArgs e)
        {
            if (ConSuc)
            {
                if (BMAuxChkBtn.Checked)
                {
                    TxString = ("~" + Encoding.ASCII.GetString(session) + "A");
                    BoardAuxPanel.Visible = true;
                    foreach (Control control in BoardAuxPanel.Controls)
                    {
                        control.Enabled = true;
                    }
                }
                else
                {
                    TxString = ("~" + Encoding.ASCII.GetString(session) + "D");
                    BoardAuxPanel.Visible = false;
                    foreach (Control control in BoardAuxPanel.Controls)
                    {
                        control.Enabled = false;
                    }
                }
                serialPort1.WriteLine(TxString);
            }
        }

        private void BAStepTB_Scroll(object sender, EventArgs e)
        {
            PosRefAux = BAStepTB.Value;                                                                 // Stores user position of the Trackbar, this is the position reference to verify the stage movement
            textBox2.Text = PosRefAux.ToString();
            if (!Busy)                                                                              // Send data in execution timeif busy flag is false (When position is not fully attained, the program will check board reported position and stored position and send the difference)
            {
                Busy = true;                                                                        // Sets busy flag
                BAStepTBLbl.Text = ("Step: " + PosRefAux);                                              // Update position on visualization
                MoveStage(BAStepTB.Value, 'P', '~');
                PosAux = BAStepTB.Value;                                                                // Request stage movement (managed by MoveStage function)
            }
        }

        private void BAStepMinBtn_Click(object sender, EventArgs e)                              // Sends board request and sets current Trackbar position as Origin
        {
            Busy = true;                                                                            // Sets busy flag
            TxString = ("~" + Encoding.ASCII.GetString(session) + "O");                             // Formats Reset position board request
            PosAux = 0;
            PosRefAux = 0;
            BAStepTB.Value = 0;
            BAStepTBLbl.Text = ("Step: 0");                                                          // Updates position visualization
            serialPort1.WriteLine(TxString);                                                        // Send Reset position board request
        }

        private void BAStepMaxBtn_Click(object sender, EventArgs e)                              // Sets current Trackbar position as Max Step position
        {
            BAStepTB.Maximum = BAStepTB.Value;                                                        // Retrieves Trackbar current position
            BAStepMaxLbl.Text = ("Max: " + BAStepTB.Maximum);                                         // Updates position visualization
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
                BAStepMaxLbl.Text = ("Max: " + BAStepTB.Maximum);                             // Updates position visualization
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
        }

        private void BACycle1Btn_Click(object sender, EventArgs e)                               // Sends board request to move a complete cycle (Backwards)
        {
            if (!Busy)
            {
                Busy = true;                                                                        // Sets busy flag
                if (BACycleCountLbl.Text == "1")                                                     // Allow only positive movement
                {
                    BACycle1Btn.Enabled = false;
                }
                CycleAux = CycleAux - 1;
                BACycleCountLbl.Text = CycleAux.ToString();
                MoveStage(Convert.ToInt32(BAStepTxt.Text), 'S', '~');                                     // Request cycle movement though MoveStage function
            }
        }

        private void BACycle2Btn_Click(object sender, EventArgs e)                               // Sends board request to move a complete cycle (Foward)
        {
            if (!Busy)
            {
                Busy = true;                                                                        // Sets busy flag
                if (BACycleCountLbl.Text == "0")                                                     // Enables for positive movement
                {
                    BACycle1Btn.Enabled = true;
                }
                CycleAux = CycleAux + 1;
                BACycleCountLbl.Text = CycleAux.ToString();
                MoveStage(Convert.ToInt32(BAStepTxt.Text), 'Z', '~');                                     // Request cycle movement though MoveStage function
            }
        }

        private void BAStepSetBtn_Click(object sender, EventArgs e)                              // Updates step setup to current Trackbar position
        {
            BAStepTxt.Text = BAStepTB.Value.ToString();
        }

        private void BASpeedTB_Scroll(object sender, EventArgs e)                                // Sends board request for changing stage moving speed on execution time
        {
            if (!Busy)
            {
                string pos = Convert.ToString(BASpeedTB.Value, 2);                                              // Formats data to ASCII encoding
                int lendif = 7 - pos.Length;
                for (i = 0; i < lendif; i++)                                                                    // Encodes position
                    pos = '0' + pos;
                pos1 = new byte[] { Convert.ToByte(pos.Substring(0, 7), 2) };
                TxString = ("~" + Encoding.ASCII.GetString(session) + "Q" + Encoding.ASCII.GetString(pos1));    // Builds instruction (Formatting)
                Busy = true;                                                                                    // Sets busy flag
                serialPort1.WriteLine(TxString);                                                                // Sends board request for changing stage movement speed
            }
        }

        private void AuStepChkBtn_CheckedChanged(object sender, EventArgs e)                     // Sends board request for uStep activation (Format and sends request depending case activation/deactivation)
        {
            if (ConSuc)
            {
                Busy = true;
                if (AuStepChkBtn.Checked)
                {
                    TxString = ("~" + Encoding.ASCII.GetString(session) + "W");
                }
                else
                {
                    TxString = ("~" + Encoding.ASCII.GetString(session) + "U");
                }
                serialPort1.WriteLine(TxString);
            }
        }

        private void AreverseChkBtn_CheckedChanged(object sender, EventArgs e)                   // Sends board request for reverse activation (Format and sends request depending case forward/backwards)
        {
            if (ConSuc)
            {
                Busy = true;
                if (AreverseChkBtn.Checked)
                {
                    TxString = ("~" + Encoding.ASCII.GetString(session) + "R");
                }
                else
                {
                    TxString = ("~" + Encoding.ASCII.GetString(session) + "F");
                }
                serialPort1.WriteLine(TxString);
            }
        }


        // The following code is (Mostly) related to Automated observation
        //      TODO:
        //              - Organize form object enable/disable routines
        //              - Check manual capture (Maybe add a visual of timer)


        private void StartBtn_Click(object sender, EventArgs e)
        {
            BStateLbl.Text = ("Sinchronizing Configuration...");
            foreach (Control control in BoardPanel.Controls)
            {
                    control.Enabled = false;
            }
            foreach (Control control in BoardAuxPanel.Controls)
            {
                control.Enabled = false;
            }
            BSpeedTB.Enabled = true;
            BStepTB.Enabled = true;
            BSpeedTBLbl.Enabled = true;
            BStepTBLbl.Enabled = true;
            uStepChkBtn.Enabled = true;
            reverseChkBtn.Enabled = true;
            BASpeedTB.Enabled = true;
            BAStepTB.Enabled = true;
            BASpeedTBLbl.Enabled = true;
            BAStepTBLbl.Enabled = true;
            AuStepChkBtn.Enabled = true;
            AreverseChkBtn.Enabled = true;


            Busy = true;
            onStart = true;
            TotalFrames = Convert.ToInt32(BCycleTxt.Text);
            TotalTime = Convert.ToInt32(BTimeTxt.Text) * 1000;
            timer1.Interval = TotalTime / 100;
            IntervalTmr.Interval = TotalTime;

            onStart = true;
            BSaveBtn_Click(sender, e);
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
            PosAux = 0;
            PosRefAux = 0;
            BAStepTB.Value = 0;
            BStepTBLbl.Text = ("Step: 0");
            if (BMAuxChkBtn.Checked == true)
                TxString = ("~" + Encoding.ASCII.GetString(session) + "O");
            else
                TxString = ("@" + Encoding.ASCII.GetString(session) + "O");

            serialPort1.WriteLine(TxString);
        }

        private void TakePictue()
        {

            BStateLbl.Text = ("Frame: " + myFrame.ToString() + " Cycle: " + myImg.ToString() + ("\nCapturing..."));
            ShutterBW.RunWorkerAsync();
            onSave = true;
        }

        private void StartCapture()
        {
            onStart = false;
            if (myFrame == 0)
            {
                timer1.Enabled = true;
                IntervalTmr.Enabled = true;
                progressBar1.Visible = true;
            }
            TakePictue();
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
                    Busy = true;
                    onMove = false;
                    onSave = false;
                    OnCapture = false;
                    captureBtn.Enabled = false;
                    BStateLbl.Text += ("\nCycle completed");
                    myImg += 1;
                    myFrame = 0;
                    if (BMAuxChkBtn.Checked == true)
                    {
                        onAuxiliar = true;
                        MoveStage(Convert.ToInt32(BAStepTxt.Text) * TotalFrames, 'S', '~');
                    }
                    else
                        MoveStage(Convert.ToInt32(BStepTxt.Text) * TotalFrames, 'S', '@');
                }
            }
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
                OnCapture = true;
                StartCapture();
            }
        }

        private void focusTB_Scroll(object sender, EventArgs e)
        {
            byte[] sendthis = new byte[] { 64, session[0], Convert.ToByte('L'), Convert.ToByte(focusTB.Value), 0X0A };
            serialPort1.Write(sendthis, 0, sendthis.Length);
        }
    }

}


