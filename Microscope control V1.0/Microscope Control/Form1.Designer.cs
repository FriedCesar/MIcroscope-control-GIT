namespace Microscope_Control
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.TestBtn = new System.Windows.Forms.Button();
            this.ImgLiveview = new System.Windows.Forms.PictureBox();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.ConnectBtn = new System.Windows.Forms.Button();
            this.ConnectionTxt = new System.Windows.Forms.TextBox();
            this.LiveviewBtn = new System.Windows.Forms.Button();
            this.getEventBtn = new System.Windows.Forms.Button();
            this.getEventTxt = new System.Windows.Forms.TextBox();
            this.LiveviewTmr = new System.Windows.Forms.Timer(this.components);
            this.ImgGuide = new System.Windows.Forms.PictureBox();
            this.guideChkBtn = new System.Windows.Forms.CheckBox();
            this.ImgAux = new System.Windows.Forms.PictureBox();
            this.ImgLogo = new System.Windows.Forms.PictureBox();
            this.serialPort1 = new System.IO.Ports.SerialPort(this.components);
            this.guideRefreshBtn = new System.Windows.Forms.Button();
            this.BShutterBtn = new System.Windows.Forms.Button();
            this.BStepMaxLbl = new System.Windows.Forms.Label();
            this.BStepMax2Btn = new System.Windows.Forms.Button();
            this.BStepMax1Btn = new System.Windows.Forms.Button();
            this.BStepTBLbl = new System.Windows.Forms.Label();
            this.BStateLbl = new System.Windows.Forms.Label();
            this.BSaveBtn = new System.Windows.Forms.Button();
            this.BStepSetBtn = new System.Windows.Forms.Button();
            this.BStepMaxBtn = new System.Windows.Forms.Button();
            this.BStepMinBtn = new System.Windows.Forms.Button();
            this.BCycleCountLbl = new System.Windows.Forms.Label();
            this.BTimeLbl = new System.Windows.Forms.Label();
            this.BTimeTxt = new System.Windows.Forms.TextBox();
            this.BCycleSetBtn = new System.Windows.Forms.Button();
            this.BCycleLbl = new System.Windows.Forms.Label();
            this.BCycle2Btn = new System.Windows.Forms.Button();
            this.BStepLbl = new System.Windows.Forms.Label();
            this.BCycleTxt = new System.Windows.Forms.TextBox();
            this.BStepTxt = new System.Windows.Forms.TextBox();
            this.BStepTB = new System.Windows.Forms.TrackBar();
            this.BCycle1Btn = new System.Windows.Forms.Button();
            this.BConnectBtn = new System.Windows.Forms.Button();
            this.BConnectionCBox = new System.Windows.Forms.ComboBox();
            this.BSpeedTBLbl = new System.Windows.Forms.Label();
            this.BSpeedTB = new System.Windows.Forms.TrackBar();
            this.wifiCameraRB = new System.Windows.Forms.RadioButton();
            this.IRCameraRB = new System.Windows.Forms.RadioButton();
            this.ShutterGB = new System.Windows.Forms.GroupBox();
            this.uStepChkBtn = new System.Windows.Forms.CheckBox();
            this.reverseChkBtn = new System.Windows.Forms.CheckBox();
            this.StartBtn = new System.Windows.Forms.Button();
            this.ManageChkBtn = new System.Windows.Forms.CheckBox();
            this.captureBtn = new System.Windows.Forms.Button();
            this.IntervalTmr = new System.Windows.Forms.Timer(this.components);
            this.LiveviewBW = new System.ComponentModel.BackgroundWorker();
            this.ShutterBW = new System.ComponentModel.BackgroundWorker();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.ImgLiveview)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ImgGuide)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ImgAux)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ImgLogo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.BStepTB)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.BSpeedTB)).BeginInit();
            this.ShutterGB.SuspendLayout();
            this.SuspendLayout();
            // 
            // TestBtn
            // 
            this.TestBtn.Location = new System.Drawing.Point(26, 515);
            this.TestBtn.Name = "TestBtn";
            this.TestBtn.Size = new System.Drawing.Size(75, 23);
            this.TestBtn.TabIndex = 0;
            this.TestBtn.Text = "TEST";
            this.TestBtn.UseVisualStyleBackColor = true;
            this.TestBtn.Click += new System.EventHandler(this.TestBtn_Click);
            // 
            // ImgLiveview
            // 
            this.ImgLiveview.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ImgLiveview.Location = new System.Drawing.Point(131, 33);
            this.ImgLiveview.Name = "ImgLiveview";
            this.ImgLiveview.Size = new System.Drawing.Size(680, 510);
            this.ImgLiveview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ImgLiveview.TabIndex = 1;
            this.ImgLiveview.TabStop = false;
            this.ImgLiveview.Visible = false;
            // 
            // timer1
            // 
            this.timer1.Interval = 1;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // ConnectBtn
            // 
            this.ConnectBtn.Location = new System.Drawing.Point(26, 391);
            this.ConnectBtn.Name = "ConnectBtn";
            this.ConnectBtn.Size = new System.Drawing.Size(75, 34);
            this.ConnectBtn.TabIndex = 2;
            this.ConnectBtn.Text = "Connect Camera";
            this.ConnectBtn.UseVisualStyleBackColor = true;
            this.ConnectBtn.Click += new System.EventHandler(this.ConnectBtn_Click);
            // 
            // ConnectionTxt
            // 
            this.ConnectionTxt.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.ConnectionTxt.Enabled = false;
            this.ConnectionTxt.Location = new System.Drawing.Point(131, 33);
            this.ConnectionTxt.Multiline = true;
            this.ConnectionTxt.Name = "ConnectionTxt";
            this.ConnectionTxt.ReadOnly = true;
            this.ConnectionTxt.Size = new System.Drawing.Size(163, 184);
            this.ConnectionTxt.TabIndex = 4;
            this.ConnectionTxt.Text = "Camera Connection Status";
            this.ConnectionTxt.Visible = false;
            // 
            // LiveviewBtn
            // 
            this.LiveviewBtn.Enabled = false;
            this.LiveviewBtn.Location = new System.Drawing.Point(26, 431);
            this.LiveviewBtn.Name = "LiveviewBtn";
            this.LiveviewBtn.Size = new System.Drawing.Size(75, 23);
            this.LiveviewBtn.TabIndex = 5;
            this.LiveviewBtn.Text = "Liveview";
            this.LiveviewBtn.UseVisualStyleBackColor = true;
            this.LiveviewBtn.Click += new System.EventHandler(this.LiveviewBtn_Click);
            // 
            // getEventBtn
            // 
            this.getEventBtn.Location = new System.Drawing.Point(26, 460);
            this.getEventBtn.Name = "getEventBtn";
            this.getEventBtn.Size = new System.Drawing.Size(75, 23);
            this.getEventBtn.TabIndex = 6;
            this.getEventBtn.Text = "getEvent";
            this.getEventBtn.UseVisualStyleBackColor = true;
            this.getEventBtn.Click += new System.EventHandler(this.getEventBtn_Click);
            // 
            // getEventTxt
            // 
            this.getEventTxt.Location = new System.Drawing.Point(26, 489);
            this.getEventTxt.Name = "getEventTxt";
            this.getEventTxt.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.getEventTxt.Size = new System.Drawing.Size(75, 20);
            this.getEventTxt.TabIndex = 7;
            // 
            // LiveviewTmr
            // 
            this.LiveviewTmr.Interval = 3;
            this.LiveviewTmr.Tick += new System.EventHandler(this.LiveviewTmr_Tick);
            // 
            // ImgGuide
            // 
            this.ImgGuide.BackColor = System.Drawing.Color.Transparent;
            this.ImgGuide.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ImgGuide.Location = new System.Drawing.Point(131, 33);
            this.ImgGuide.Name = "ImgGuide";
            this.ImgGuide.Size = new System.Drawing.Size(680, 510);
            this.ImgGuide.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ImgGuide.TabIndex = 8;
            this.ImgGuide.TabStop = false;
            this.ImgGuide.Visible = false;
            // 
            // guideChkBtn
            // 
            this.guideChkBtn.AutoSize = true;
            this.guideChkBtn.Enabled = false;
            this.guideChkBtn.Location = new System.Drawing.Point(25, 540);
            this.guideChkBtn.Name = "guideChkBtn";
            this.guideChkBtn.Size = new System.Drawing.Size(54, 17);
            this.guideChkBtn.TabIndex = 10;
            this.guideChkBtn.Text = "Guide";
            this.guideChkBtn.UseVisualStyleBackColor = true;
            this.guideChkBtn.CheckedChanged += new System.EventHandler(this.guideChkBtn_CheckedChanged);
            // 
            // ImgAux
            // 
            this.ImgAux.Location = new System.Drawing.Point(1014, 383);
            this.ImgAux.Name = "ImgAux";
            this.ImgAux.Size = new System.Drawing.Size(160, 120);
            this.ImgAux.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.ImgAux.TabIndex = 11;
            this.ImgAux.TabStop = false;
            this.ImgAux.LoadCompleted += new System.ComponentModel.AsyncCompletedEventHandler(this.pictureBox3_LoadCompleted);
            // 
            // ImgLogo
            // 
            this.ImgLogo.BackColor = System.Drawing.Color.Transparent;
            this.ImgLogo.Image = ((System.Drawing.Image)(resources.GetObject("ImgLogo.Image")));
            this.ImgLogo.Location = new System.Drawing.Point(131, 33);
            this.ImgLogo.Name = "ImgLogo";
            this.ImgLogo.Size = new System.Drawing.Size(680, 510);
            this.ImgLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.ImgLogo.TabIndex = 12;
            this.ImgLogo.TabStop = false;
            // 
            // serialPort1
            // 
            this.serialPort1.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(this.serialPort1_DataReceived);
            // 
            // guideRefreshBtn
            // 
            this.guideRefreshBtn.Enabled = false;
            this.guideRefreshBtn.Image = ((System.Drawing.Image)(resources.GetObject("guideRefreshBtn.Image")));
            this.guideRefreshBtn.Location = new System.Drawing.Point(78, 536);
            this.guideRefreshBtn.Name = "guideRefreshBtn";
            this.guideRefreshBtn.Size = new System.Drawing.Size(23, 23);
            this.guideRefreshBtn.TabIndex = 14;
            this.guideRefreshBtn.UseVisualStyleBackColor = true;
            this.guideRefreshBtn.Click += new System.EventHandler(this.guideRefreshBtn_Click);
            // 
            // BShutterBtn
            // 
            this.BShutterBtn.Enabled = false;
            this.BShutterBtn.Location = new System.Drawing.Point(42, 24);
            this.BShutterBtn.Name = "BShutterBtn";
            this.BShutterBtn.Size = new System.Drawing.Size(75, 23);
            this.BShutterBtn.TabIndex = 57;
            this.BShutterBtn.Text = "Shutter";
            this.BShutterBtn.UseVisualStyleBackColor = true;
            this.BShutterBtn.Click += new System.EventHandler(this.BShutterBtn_Click);
            // 
            // BStepMaxLbl
            // 
            this.BStepMaxLbl.AutoSize = true;
            this.BStepMaxLbl.Enabled = false;
            this.BStepMaxLbl.Location = new System.Drawing.Point(1203, 525);
            this.BStepMaxLbl.Name = "BStepMaxLbl";
            this.BStepMaxLbl.Size = new System.Drawing.Size(51, 13);
            this.BStepMaxLbl.TabIndex = 56;
            this.BStepMaxLbl.Text = "Max: 100";
            // 
            // BStepMax2Btn
            // 
            this.BStepMax2Btn.Enabled = false;
            this.BStepMax2Btn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BStepMax2Btn.Location = new System.Drawing.Point(1260, 520);
            this.BStepMax2Btn.Name = "BStepMax2Btn";
            this.BStepMax2Btn.Size = new System.Drawing.Size(22, 23);
            this.BStepMax2Btn.TabIndex = 55;
            this.BStepMax2Btn.Text = "+";
            this.BStepMax2Btn.UseVisualStyleBackColor = true;
            this.BStepMax2Btn.Click += new System.EventHandler(this.BStepMax2Btn_Click);
            // 
            // BStepMax1Btn
            // 
            this.BStepMax1Btn.Enabled = false;
            this.BStepMax1Btn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BStepMax1Btn.Location = new System.Drawing.Point(1175, 520);
            this.BStepMax1Btn.Name = "BStepMax1Btn";
            this.BStepMax1Btn.Size = new System.Drawing.Size(22, 23);
            this.BStepMax1Btn.TabIndex = 54;
            this.BStepMax1Btn.Text = "-";
            this.BStepMax1Btn.UseVisualStyleBackColor = true;
            this.BStepMax1Btn.Click += new System.EventHandler(this.BStepMax1Btn_Click);
            // 
            // BStepTBLbl
            // 
            this.BStepTBLbl.AutoSize = true;
            this.BStepTBLbl.Enabled = false;
            this.BStepTBLbl.Location = new System.Drawing.Point(832, 511);
            this.BStepTBLbl.Name = "BStepTBLbl";
            this.BStepTBLbl.Size = new System.Drawing.Size(32, 13);
            this.BStepTBLbl.TabIndex = 53;
            this.BStepTBLbl.Text = "Step:";
            // 
            // BStateLbl
            // 
            this.BStateLbl.AutoSize = true;
            this.BStateLbl.Enabled = false;
            this.BStateLbl.Location = new System.Drawing.Point(1125, 199);
            this.BStateLbl.Name = "BStateLbl";
            this.BStateLbl.Size = new System.Drawing.Size(37, 13);
            this.BStateLbl.TabIndex = 51;
            this.BStateLbl.Text = "Status";
            // 
            // BSaveBtn
            // 
            this.BSaveBtn.Enabled = false;
            this.BSaveBtn.Location = new System.Drawing.Point(1047, 260);
            this.BSaveBtn.Name = "BSaveBtn";
            this.BSaveBtn.Size = new System.Drawing.Size(75, 23);
            this.BSaveBtn.TabIndex = 45;
            this.BSaveBtn.Text = "Save";
            this.BSaveBtn.UseVisualStyleBackColor = true;
            this.BSaveBtn.Click += new System.EventHandler(this.BSaveBtn_Click);
            // 
            // BStepSetBtn
            // 
            this.BStepSetBtn.Enabled = false;
            this.BStepSetBtn.Location = new System.Drawing.Point(1209, 290);
            this.BStepSetBtn.Name = "BStepSetBtn";
            this.BStepSetBtn.Size = new System.Drawing.Size(75, 23);
            this.BStepSetBtn.TabIndex = 42;
            this.BStepSetBtn.Text = "Set as step";
            this.BStepSetBtn.UseVisualStyleBackColor = true;
            this.BStepSetBtn.Click += new System.EventHandler(this.BStepSetBtn_Click);
            // 
            // BStepMaxBtn
            // 
            this.BStepMaxBtn.Enabled = false;
            this.BStepMaxBtn.Location = new System.Drawing.Point(1128, 290);
            this.BStepMaxBtn.Name = "BStepMaxBtn";
            this.BStepMaxBtn.Size = new System.Drawing.Size(75, 23);
            this.BStepMaxBtn.TabIndex = 43;
            this.BStepMaxBtn.Text = "Set Max";
            this.BStepMaxBtn.UseVisualStyleBackColor = true;
            this.BStepMaxBtn.Click += new System.EventHandler(this.BStepMaxBtn_Click);
            // 
            // BStepMinBtn
            // 
            this.BStepMinBtn.Enabled = false;
            this.BStepMinBtn.Location = new System.Drawing.Point(1047, 290);
            this.BStepMinBtn.Name = "BStepMinBtn";
            this.BStepMinBtn.Size = new System.Drawing.Size(75, 23);
            this.BStepMinBtn.TabIndex = 41;
            this.BStepMinBtn.Text = "Set Min";
            this.BStepMinBtn.UseVisualStyleBackColor = true;
            this.BStepMinBtn.Click += new System.EventHandler(this.BStepMinBtn_Click);
            // 
            // BCycleCountLbl
            // 
            this.BCycleCountLbl.AutoSize = true;
            this.BCycleCountLbl.Enabled = false;
            this.BCycleCountLbl.Location = new System.Drawing.Point(1078, 324);
            this.BCycleCountLbl.Name = "BCycleCountLbl";
            this.BCycleCountLbl.Size = new System.Drawing.Size(13, 13);
            this.BCycleCountLbl.TabIndex = 52;
            this.BCycleCountLbl.Text = "0";
            // 
            // BTimeLbl
            // 
            this.BTimeLbl.AutoSize = true;
            this.BTimeLbl.Enabled = false;
            this.BTimeLbl.Location = new System.Drawing.Point(934, 352);
            this.BTimeLbl.Name = "BTimeLbl";
            this.BTimeLbl.Size = new System.Drawing.Size(30, 13);
            this.BTimeLbl.TabIndex = 50;
            this.BTimeLbl.Text = "Time";
            // 
            // BTimeTxt
            // 
            this.BTimeTxt.Enabled = false;
            this.BTimeTxt.Location = new System.Drawing.Point(973, 348);
            this.BTimeTxt.Name = "BTimeTxt";
            this.BTimeTxt.Size = new System.Drawing.Size(68, 20);
            this.BTimeTxt.TabIndex = 39;
            this.BTimeTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // BCycleSetBtn
            // 
            this.BCycleSetBtn.Enabled = false;
            this.BCycleSetBtn.Location = new System.Drawing.Point(1128, 318);
            this.BCycleSetBtn.Name = "BCycleSetBtn";
            this.BCycleSetBtn.Size = new System.Drawing.Size(75, 23);
            this.BCycleSetBtn.TabIndex = 46;
            this.BCycleSetBtn.Text = "Set as Cycle";
            this.BCycleSetBtn.UseVisualStyleBackColor = true;
            this.BCycleSetBtn.Click += new System.EventHandler(this.BCycleSetBtn_Click);
            // 
            // BCycleLbl
            // 
            this.BCycleLbl.AutoSize = true;
            this.BCycleLbl.Enabled = false;
            this.BCycleLbl.Location = new System.Drawing.Point(934, 324);
            this.BCycleLbl.Name = "BCycleLbl";
            this.BCycleLbl.Size = new System.Drawing.Size(33, 13);
            this.BCycleLbl.TabIndex = 49;
            this.BCycleLbl.Text = "Cycle";
            // 
            // BCycle2Btn
            // 
            this.BCycle2Btn.Enabled = false;
            this.BCycle2Btn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BCycle2Btn.Location = new System.Drawing.Point(1100, 318);
            this.BCycle2Btn.Name = "BCycle2Btn";
            this.BCycle2Btn.Size = new System.Drawing.Size(22, 23);
            this.BCycle2Btn.TabIndex = 44;
            this.BCycle2Btn.Text = "+";
            this.BCycle2Btn.UseVisualStyleBackColor = true;
            this.BCycle2Btn.Click += new System.EventHandler(this.BCycle2Btn_Click);
            // 
            // BStepLbl
            // 
            this.BStepLbl.AutoSize = true;
            this.BStepLbl.Enabled = false;
            this.BStepLbl.Location = new System.Drawing.Point(934, 296);
            this.BStepLbl.Name = "BStepLbl";
            this.BStepLbl.Size = new System.Drawing.Size(29, 13);
            this.BStepLbl.TabIndex = 48;
            this.BStepLbl.Text = "Step";
            // 
            // BCycleTxt
            // 
            this.BCycleTxt.Enabled = false;
            this.BCycleTxt.Location = new System.Drawing.Point(973, 321);
            this.BCycleTxt.Name = "BCycleTxt";
            this.BCycleTxt.Size = new System.Drawing.Size(68, 20);
            this.BCycleTxt.TabIndex = 38;
            this.BCycleTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // BStepTxt
            // 
            this.BStepTxt.Enabled = false;
            this.BStepTxt.Location = new System.Drawing.Point(973, 292);
            this.BStepTxt.Name = "BStepTxt";
            this.BStepTxt.Size = new System.Drawing.Size(68, 20);
            this.BStepTxt.TabIndex = 37;
            this.BStepTxt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            // 
            // BStepTB
            // 
            this.BStepTB.Enabled = false;
            this.BStepTB.Location = new System.Drawing.Point(839, 549);
            this.BStepTB.Maximum = 100;
            this.BStepTB.Name = "BStepTB";
            this.BStepTB.Size = new System.Drawing.Size(480, 45);
            this.BStepTB.TabIndex = 40;
            this.BStepTB.Scroll += new System.EventHandler(this.BStepTB_Scroll);
            // 
            // BCycle1Btn
            // 
            this.BCycle1Btn.Enabled = false;
            this.BCycle1Btn.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BCycle1Btn.Location = new System.Drawing.Point(1047, 318);
            this.BCycle1Btn.Name = "BCycle1Btn";
            this.BCycle1Btn.Size = new System.Drawing.Size(22, 23);
            this.BCycle1Btn.TabIndex = 47;
            this.BCycle1Btn.Text = "-";
            this.BCycle1Btn.UseVisualStyleBackColor = true;
            this.BCycle1Btn.Click += new System.EventHandler(this.BCycle1Btn_Click);
            // 
            // BConnectBtn
            // 
            this.BConnectBtn.Enabled = false;
            this.BConnectBtn.Location = new System.Drawing.Point(966, 244);
            this.BConnectBtn.Name = "BConnectBtn";
            this.BConnectBtn.Size = new System.Drawing.Size(75, 39);
            this.BConnectBtn.TabIndex = 36;
            this.BConnectBtn.Text = "Connect Board";
            this.BConnectBtn.UseVisualStyleBackColor = true;
            this.BConnectBtn.Click += new System.EventHandler(this.BConnectBtn_Click);
            // 
            // BConnectionCBox
            // 
            this.BConnectionCBox.FormattingEnabled = true;
            this.BConnectionCBox.Location = new System.Drawing.Point(839, 245);
            this.BConnectionCBox.Name = "BConnectionCBox";
            this.BConnectionCBox.Size = new System.Drawing.Size(121, 21);
            this.BConnectionCBox.TabIndex = 13;
            this.BConnectionCBox.DropDown += new System.EventHandler(this.comboBox1_DropDown);
            this.BConnectionCBox.SelectedIndexChanged += new System.EventHandler(this.BConnectionCBox_SelectedIndexChanged);
            // 
            // BSpeedTBLbl
            // 
            this.BSpeedTBLbl.AutoSize = true;
            this.BSpeedTBLbl.Enabled = false;
            this.BSpeedTBLbl.Location = new System.Drawing.Point(832, 604);
            this.BSpeedTBLbl.Name = "BSpeedTBLbl";
            this.BSpeedTBLbl.Size = new System.Drawing.Size(38, 13);
            this.BSpeedTBLbl.TabIndex = 64;
            this.BSpeedTBLbl.Text = "Speed";
            // 
            // BSpeedTB
            // 
            this.BSpeedTB.Enabled = false;
            this.BSpeedTB.Location = new System.Drawing.Point(839, 646);
            this.BSpeedTB.Maximum = 100;
            this.BSpeedTB.Minimum = 1;
            this.BSpeedTB.Name = "BSpeedTB";
            this.BSpeedTB.Size = new System.Drawing.Size(480, 45);
            this.BSpeedTB.TabIndex = 65;
            this.BSpeedTB.Value = 3;
            this.BSpeedTB.Scroll += new System.EventHandler(this.BSpeedTB_Scroll);
            // 
            // wifiCameraRB
            // 
            this.wifiCameraRB.AutoSize = true;
            this.wifiCameraRB.Checked = true;
            this.wifiCameraRB.Location = new System.Drawing.Point(13, 62);
            this.wifiCameraRB.Name = "wifiCameraRB";
            this.wifiCameraRB.Size = new System.Drawing.Size(104, 17);
            this.wifiCameraRB.TabIndex = 66;
            this.wifiCameraRB.TabStop = true;
            this.wifiCameraRB.Text = "Sony DSC-QX10";
            this.wifiCameraRB.UseVisualStyleBackColor = true;
            this.wifiCameraRB.Visible = false;
            // 
            // IRCameraRB
            // 
            this.IRCameraRB.AutoSize = true;
            this.IRCameraRB.Location = new System.Drawing.Point(13, 85);
            this.IRCameraRB.Name = "IRCameraRB";
            this.IRCameraRB.Size = new System.Drawing.Size(104, 17);
            this.IRCameraRB.TabIndex = 67;
            this.IRCameraRB.Text = "Nikon IR Shutter";
            this.IRCameraRB.UseVisualStyleBackColor = true;
            this.IRCameraRB.Visible = false;
            // 
            // ShutterGB
            // 
            this.ShutterGB.Controls.Add(this.BShutterBtn);
            this.ShutterGB.Controls.Add(this.wifiCameraRB);
            this.ShutterGB.Controls.Add(this.IRCameraRB);
            this.ShutterGB.Location = new System.Drawing.Point(1177, 376);
            this.ShutterGB.Name = "ShutterGB";
            this.ShutterGB.Size = new System.Drawing.Size(130, 130);
            this.ShutterGB.TabIndex = 69;
            this.ShutterGB.TabStop = false;
            // 
            // uStepChkBtn
            // 
            this.uStepChkBtn.AutoSize = true;
            this.uStepChkBtn.Enabled = false;
            this.uStepChkBtn.Location = new System.Drawing.Point(839, 383);
            this.uStepChkBtn.Name = "uStepChkBtn";
            this.uStepChkBtn.Size = new System.Drawing.Size(97, 17);
            this.uStepChkBtn.TabIndex = 70;
            this.uStepChkBtn.Text = "Micro Stepping";
            this.uStepChkBtn.UseVisualStyleBackColor = true;
            this.uStepChkBtn.CheckedChanged += new System.EventHandler(this.uStepChkBtn_CheckedChanged);
            // 
            // reverseChkBtn
            // 
            this.reverseChkBtn.AutoSize = true;
            this.reverseChkBtn.Enabled = false;
            this.reverseChkBtn.Location = new System.Drawing.Point(839, 407);
            this.reverseChkBtn.Name = "reverseChkBtn";
            this.reverseChkBtn.Size = new System.Drawing.Size(109, 17);
            this.reverseChkBtn.TabIndex = 71;
            this.reverseChkBtn.Text = "Reverse direction";
            this.reverseChkBtn.UseVisualStyleBackColor = true;
            this.reverseChkBtn.CheckedChanged += new System.EventHandler(this.reverseChkBtn_CheckedChanged);
            // 
            // StartBtn
            // 
            this.StartBtn.Enabled = false;
            this.StartBtn.Location = new System.Drawing.Point(1128, 33);
            this.StartBtn.Name = "StartBtn";
            this.StartBtn.Size = new System.Drawing.Size(110, 46);
            this.StartBtn.TabIndex = 72;
            this.StartBtn.Text = "START";
            this.StartBtn.UseVisualStyleBackColor = true;
            this.StartBtn.Click += new System.EventHandler(this.StartBtn_Click);
            // 
            // ManageChkBtn
            // 
            this.ManageChkBtn.AutoSize = true;
            this.ManageChkBtn.Enabled = false;
            this.ManageChkBtn.Location = new System.Drawing.Point(1128, 114);
            this.ManageChkBtn.Name = "ManageChkBtn";
            this.ManageChkBtn.Size = new System.Drawing.Size(124, 17);
            this.ManageChkBtn.TabIndex = 73;
            this.ManageChkBtn.Text = "Unmanaged Capture";
            this.ManageChkBtn.UseVisualStyleBackColor = true;
            this.ManageChkBtn.CheckedChanged += new System.EventHandler(this.ManageChkBtn_CheckedChanged);
            // 
            // captureBtn
            // 
            this.captureBtn.Enabled = false;
            this.captureBtn.Location = new System.Drawing.Point(1128, 85);
            this.captureBtn.Name = "captureBtn";
            this.captureBtn.Size = new System.Drawing.Size(110, 23);
            this.captureBtn.TabIndex = 74;
            this.captureBtn.Text = "Capture";
            this.captureBtn.UseVisualStyleBackColor = true;
            this.captureBtn.Click += new System.EventHandler(this.captureBtn_Click);
            // 
            // IntervalTmr
            // 
            this.IntervalTmr.Tick += new System.EventHandler(this.IntervalTmr_Tick);
            // 
            // LiveviewBW
            // 
            this.LiveviewBW.WorkerReportsProgress = true;
            this.LiveviewBW.WorkerSupportsCancellation = true;
            this.LiveviewBW.DoWork += new System.ComponentModel.DoWorkEventHandler(this.LiveviewBW_DoWork);
            this.LiveviewBW.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.LiveviewBW_ProgressChanged);
            this.LiveviewBW.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.LiveviewBW_RunWorkerCompleted);
            // 
            // ShutterBW
            // 
            this.ShutterBW.WorkerReportsProgress = true;
            this.ShutterBW.WorkerSupportsCancellation = true;
            this.ShutterBW.DoWork += new System.ComponentModel.DoWorkEventHandler(this.ShutterBW_DoWork);
            this.ShutterBW.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.ShutterBW_ProgressChanged);
            this.ShutterBW.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.ShutterBW_RunWorkerCompleted);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(1, 1);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBox1.Size = new System.Drawing.Size(124, 317);
            this.textBox1.TabIndex = 76;
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(1, 324);
            this.textBox2.Multiline = true;
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(124, 61);
            this.textBox2.TabIndex = 77;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(1350, 729);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.captureBtn);
            this.Controls.Add(this.ManageChkBtn);
            this.Controls.Add(this.StartBtn);
            this.Controls.Add(this.reverseChkBtn);
            this.Controls.Add(this.uStepChkBtn);
            this.Controls.Add(this.ConnectBtn);
            this.Controls.Add(this.TestBtn);
            this.Controls.Add(this.BConnectionCBox);
            this.Controls.Add(this.LiveviewBtn);
            this.Controls.Add(this.BCycleSetBtn);
            this.Controls.Add(this.getEventBtn);
            this.Controls.Add(this.ShutterGB);
            this.Controls.Add(this.getEventTxt);
            this.Controls.Add(this.BConnectBtn);
            this.Controls.Add(this.guideChkBtn);
            this.Controls.Add(this.ImgAux);
            this.Controls.Add(this.BStepMax1Btn);
            this.Controls.Add(this.guideRefreshBtn);
            this.Controls.Add(this.ImgGuide);
            this.Controls.Add(this.BStepTBLbl);
            this.Controls.Add(this.ConnectionTxt);
            this.Controls.Add(this.ImgLiveview);
            this.Controls.Add(this.BCycleLbl);
            this.Controls.Add(this.BTimeTxt);
            this.Controls.Add(this.BCycleTxt);
            this.Controls.Add(this.BCycle1Btn);
            this.Controls.Add(this.BStepMinBtn);
            this.Controls.Add(this.BStepMax2Btn);
            this.Controls.Add(this.BStepSetBtn);
            this.Controls.Add(this.BStateLbl);
            this.Controls.Add(this.BSpeedTBLbl);
            this.Controls.Add(this.BCycle2Btn);
            this.Controls.Add(this.BStepMaxBtn);
            this.Controls.Add(this.BTimeLbl);
            this.Controls.Add(this.BCycleCountLbl);
            this.Controls.Add(this.BStepTB);
            this.Controls.Add(this.BStepTxt);
            this.Controls.Add(this.BStepMaxLbl);
            this.Controls.Add(this.BStepLbl);
            this.Controls.Add(this.BSaveBtn);
            this.Controls.Add(this.BSpeedTB);
            this.Controls.Add(this.ImgLogo);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "Microscope Control V1.0";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.ImgLiveview)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ImgGuide)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ImgAux)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ImgLogo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.BStepTB)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.BSpeedTB)).EndInit();
            this.ShutterGB.ResumeLayout(false);
            this.ShutterGB.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button TestBtn;
        private System.Windows.Forms.PictureBox ImgLiveview;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Button ConnectBtn;
        private System.Windows.Forms.TextBox ConnectionTxt;
        private System.Windows.Forms.Button LiveviewBtn;
        private System.Windows.Forms.Button getEventBtn;
        private System.Windows.Forms.TextBox getEventTxt;
        private System.Windows.Forms.Timer LiveviewTmr;
        private System.Windows.Forms.PictureBox ImgGuide;
        private System.Windows.Forms.CheckBox guideChkBtn;
        private System.Windows.Forms.PictureBox ImgAux;
        private System.Windows.Forms.PictureBox ImgLogo;
        private System.IO.Ports.SerialPort serialPort1;
        private System.Windows.Forms.Button guideRefreshBtn;
        private System.Windows.Forms.Button BShutterBtn;
        private System.Windows.Forms.Label BStepMaxLbl;
        private System.Windows.Forms.Button BStepMax2Btn;
        private System.Windows.Forms.Button BStepMax1Btn;
        private System.Windows.Forms.Label BStepTBLbl;
        private System.Windows.Forms.Label BStateLbl;
        private System.Windows.Forms.Button BSaveBtn;
        private System.Windows.Forms.Button BStepSetBtn;
        private System.Windows.Forms.Button BStepMaxBtn;
        private System.Windows.Forms.Button BStepMinBtn;
        private System.Windows.Forms.Label BCycleCountLbl;
        private System.Windows.Forms.Label BTimeLbl;
        private System.Windows.Forms.TextBox BTimeTxt;
        private System.Windows.Forms.Button BCycleSetBtn;
        private System.Windows.Forms.Label BCycleLbl;
        private System.Windows.Forms.Button BCycle2Btn;
        private System.Windows.Forms.Label BStepLbl;
        private System.Windows.Forms.TextBox BCycleTxt;
        private System.Windows.Forms.TextBox BStepTxt;
        private System.Windows.Forms.TrackBar BStepTB;
        private System.Windows.Forms.Button BCycle1Btn;
        private System.Windows.Forms.Button BConnectBtn;
        private System.Windows.Forms.ComboBox BConnectionCBox;
        private System.Windows.Forms.Label BSpeedTBLbl;
        private System.Windows.Forms.TrackBar BSpeedTB;
        private System.Windows.Forms.RadioButton wifiCameraRB;
        private System.Windows.Forms.RadioButton IRCameraRB;
        private System.Windows.Forms.GroupBox ShutterGB;
        private System.Windows.Forms.CheckBox uStepChkBtn;
        private System.Windows.Forms.CheckBox reverseChkBtn;
        private System.Windows.Forms.Button StartBtn;
        private System.Windows.Forms.CheckBox ManageChkBtn;
        private System.Windows.Forms.Button captureBtn;
        private System.Windows.Forms.Timer IntervalTmr;
        private System.ComponentModel.BackgroundWorker LiveviewBW;
        private System.ComponentModel.BackgroundWorker ShutterBW;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.TextBox textBox2;
    }
}

