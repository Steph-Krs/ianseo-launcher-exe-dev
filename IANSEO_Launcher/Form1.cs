using QRCoder;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace IANSEO_Launcher
{
    

    public partial class MainForm : Form
    {
        private ResourceManager rm; // Embedded resources > translation & images
        private Button btnStartOpen; // Start/Open button > switch de pending on service status
        private Button btnStop; // Button to stop Apache and MySQL
        private Label lblAddressTitle; // "Server address" title
        private Label lblIanseoAddress; // Ianseo server IP address
        private Button btnCopyLink; // Button to copy the server IP address to clipboard
        private Button btnShowQR; // Button to display the server's IP address as a QR code
        private Label lblStatusTitle; // "Status" title
        private Panel dotApache; // Apache status indicator dot (red/green)
        private Label lblApache; // "Apache" label (Auto = Windows service / Manual = xampp_start method)
        private Panel dotMySQL; // MySQL status indicator dot (red/green)
        private Label lblMySQL; // "MySQL" label (Auto = Windows service / Manual = xampp_start method)
        private Button btnOpenXamppControl; // Button to open xampp-control.exe
        private Label lblRepairTitle; // "Problem solving" title
        private Label lblAdminMessage; // "Please run as admin" message
        private Button btnRunAsAdmin; // Button to open this exe as admin
        private Button btnRepairMySQL; // Button to repair MySQL database
        private Button btnEnableStartup; // Button to create and enable Apache and MySQL Windows services at startup
        private Button btnWindowsDefender; // Button to set Windows Defender exceptions

        private System.Windows.Forms.Timer checkTimer;
        private bool servicesRunning = false;
        private enum ServiceState { Unknown, Running, Stopped }
        private ServiceState desiredState = ServiceState.Unknown;

        private AnimatedProgressBar progressBar;
        private System.Windows.Forms.Timer progressTimer;
        private DateTime progressStartTime;
        private bool isProgressRunning = false;

        FlowLayoutPanel MainPanel = new FlowLayoutPanel();
        FlowLayoutPanel ControlPanel = new FlowLayoutPanel();
        FlowLayoutPanel ServerInfoPanel = new FlowLayoutPanel();
        FlowLayoutPanel LinkPanel = new FlowLayoutPanel();
        FlowLayoutPanel StatusPanel = new FlowLayoutPanel();
        FlowLayoutPanel StatusSubPanel = new FlowLayoutPanel();
        FlowLayoutPanel ServicesPanel = new FlowLayoutPanel();
        FlowLayoutPanel ApachePanel = new FlowLayoutPanel();
        FlowLayoutPanel MysqlPanel = new FlowLayoutPanel();
        FlowLayoutPanel RepairPanel = new FlowLayoutPanel();

        // Apply the current user interface culture > language
        CultureInfo culture = Thread.CurrentThread.CurrentUICulture;
        // Optional: comment the previous line and uncomment the following line to force the language. Available: en, fr, de, es, it
        //CultureInfo culture = new CultureInfo("it");

        private string apacheServiceName; // Name of the Windows service created for Apache
        private string mysqlServiceName; // Name of the Windows service created for MySQL
        private string exePath; // Path to this executable file
        private string exeDir; // Path to the folder containing this executable file
        private string basePath; // Path to the ianseo folder serving as the basis for all files used in the program
        private string xamppStartPath; // Path to xampp_start.exe
        private string xamppStopPath; // Path to xampp_stop.exe
        private string httpdConfPath; // Path to httpd.conf
        private string xamppControlPath; // Path to xampp-control.exe
        private string apacheBin; // Path to httpd.exe
        private string mysqlBin; // Path to mysqld.exe


        /* Tree structure of the panels that make up the interface
        Form "IANSEO" (300×510 min 316×549)
        └── MainPanel (Flow: TopDown, 300×510)
            ├── ControlPanel (Flow: TopDown, 300×155)
            │   ├── btnStartOpen (Button, 290×50)
            │   ├-─ btnStop (Button, 250×40)
            │   └-─ progressBar (300×20)
            │
            ├── ServerInfoPanel (Flow: TopDown, 300×105)
            │   ├── lblAddressTitle (Label, 300×20)
            │   ├-─ lblIanseoAddress (Label, 300×20)
            │   └-─ LinkPanel (Flow: LeftToRight, 300×25)
            │       ├-─ btnCopyLink (100×25)
            │       └-─ btnShowQR (100×25)
            │
            ├── StatusPanel (Flow: TopDown, 300×105)
            │   ├── lblStatusTitle (Label, 300×20)
            │   └── StatusSubPanel (FlowDirection.LeftToRight, 300×55)
            │       ├── ServicesPanel (FlowDirection.TopDown, 190×55)
            │       │   ├── ApachePanel (FlowDirection.LeftToRight, 160×20)
            │       │   │   ├── dotApache (Panel rond, 20×20)
            │       │   │   └── lblApache (Label, 140×20)
            │       │   └── MysqlPanel (FlowDirection.LeftToRight, 160×20)
            │       │       ├── dotMySQL (Panel rond, 20×20)
            │       │       └── lblMySQL (Label, 140×20)
            │       └── btnOpenXamppControl (Button, 80×35)
            │
            └── RepairPanel (FlowDirection.TopDown, 300×145)
                ├── lblRepairTitle (Label, 300×20)
                ├── lblAdminMessage (Label, 280×60)
                ├── btnRunAsAdmin (Button, 290×25)
                ├── btnRepairMySQL (Button, 290×25)
                ├── btnEnableStartup (Button, 290×25)
                └── btnWindowsDefender (Button, 290×25)
        */


        public MainForm()
        {
            rm = new ResourceManager("IANSEO_Launcher.Properties.Resources", typeof(MainForm).Assembly);
            apacheServiceName = "Ianseo_Apache";
            mysqlServiceName = "Ianseo_MySQL";
            exePath = Assembly.GetExecutingAssembly().Location;
            exeDir = Path.GetDirectoryName(exePath);

            if (exeDir != null && // Check if the exe is in a “ianseo” folder and that “xampp-control.exe” is present
                string.Equals(Path.GetFileName(exeDir), "ianseo", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(Path.Combine(exeDir, "xampp-control.exe")))
            {
                basePath = exeDir; // Uses this folder as the base for building relative paths
                xamppStartPath = Path.Combine(basePath, "xampp_start.exe");
                xamppStopPath = Path.Combine(basePath, "xampp_stop.exe");
                httpdConfPath = Path.Combine(basePath, "apache", "conf", "httpd.conf");
                xamppControlPath = Path.Combine(basePath, "xampp-control.exe");
                apacheBin = Path.Combine(basePath, "apache", "bin", "httpd.exe");
                mysqlBin = Path.Combine(basePath, "mysql", "bin", "mysqld.exe");
            }
            else if (Directory.Exists(@"C:\ianseo") && File.Exists(Path.Combine(@"C:\ianseo", "xampp-control.exe"))) // If xampp-control is not found and the base folder is not ianseo, we try to see if C:\ianseo exists
            {
                basePath = @"C:\ianseo"; // Uses C:\ianseo as the base for building relative paths
                xamppStartPath = Path.Combine(basePath, "xampp_start.exe");
                xamppStopPath = Path.Combine(basePath, "xampp_stop.exe");
                httpdConfPath = Path.Combine(basePath, "apache", "conf", "httpd.conf");
                xamppControlPath = Path.Combine(basePath, "xampp-control.exe");
                apacheBin = Path.Combine(basePath, "apache", "bin", "httpd.exe");
                mysqlBin = Path.Combine(basePath, "mysql", "bin", "mysqld.exe");
            }
            else // Otherwise, displays a window with a message asking you to move ianseo.exe to the ianseo installation folder
            {
                Form errorForm = new Form()
                {
                    Text = rm.GetString("lblErrorInvalidDirectoryTitle", culture),
                    Size = new Size(400, 350),
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };
                PictureBox iconBox = new PictureBox() // Error icon
                {
                    Image = SystemIcons.Error.ToBitmap(),
                    Size = new Size(32, 32),
                    Location = new Point(175, 30)
                };
                Label lbl = new Label()
                {
                    Text = rm.GetString("lblErrorInvalidDirectory", culture),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                Button btnClose = new Button()
                {
                    Text = rm.GetString("btnClose", culture),
                    Dock = DockStyle.Bottom,
                    Height = 40
                };
                btnClose.BackColor = Color.FromArgb(0, 68, 136);
                btnClose.ForeColor = Color.White;
                btnClose.Click += (s, e) =>
                {
                    errorForm.Close();
                    Application.Exit(); // Close the app completely
                };

                errorForm.Controls.Add(iconBox);
                errorForm.Controls.Add(lbl);
                errorForm.Controls.Add(btnClose);
                errorForm.ShowDialog();
            }
            InitUI();
            CheckInitialServiceStatus(); // Only used at the app startup
            InitTimer();
            UpdateUIByServiceStatus();
        }


        //* UI Initialization and Update Methods *//
        private void InitUI() // Initialization of all interface components
        {
            // this = application window
            this.Text = "IANSEO";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.BackColor = Color.Black;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            this.ClientSize = new Size(300, 510);
            this.MinimumSize = new Size(316, 549);
            this.Icon = Properties.Resources.ianseo;

                // Main panel [Flow - Vertical - this/MainPanel]
                MainPanel.BackColor = Color.FromArgb(240, 248, 255);
                MainPanel.FlowDirection = FlowDirection.TopDown;
                MainPanel.Size = new Size(300, 510);
                MainPanel.Padding = new Padding(0);
                MainPanel.Margin = new Padding(0);
                MainPanel.Anchor = AnchorStyles.None;
                MainPanel.Controls.Clear();

                    // ControlPanel [Flow - Vertical - this/MainPanel/ControlPanel]
                    ControlPanel.Anchor = AnchorStyles.None;
                    ControlPanel.FlowDirection = FlowDirection.TopDown;
                    ControlPanel.Size = new Size(300, 155);
                    ControlPanel.Padding = new Padding(0);
                    ControlPanel.Margin = new Padding(0, 0, 0, 0);
                    ControlPanel.Anchor = AnchorStyles.Top;
                    ControlPanel.WrapContents = false;

                        // btnStartOpen [Button - this/MainPanel/ControlPanel/btnStartOpen]
                        btnStartOpen = new Button();
                        btnStartOpen.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                        btnStartOpen.BackColor = Color.FromArgb(0, 68, 136);
                        btnStartOpen.ForeColor = Color.White;
                        btnStartOpen.Size = new Size(290, 50);
                        btnStartOpen.FlatStyle = FlatStyle.Flat;
                        btnStartOpen.FlatAppearance.BorderSize = 0;
                        btnStartOpen.Padding = new Padding(0);
                        btnStartOpen.Margin = new Padding(5, 10, 0, 0);
                        btnStartOpen.Click += BtnStartOpen_Click;
                        btnStartOpen.Anchor = AnchorStyles.Top;
                    ControlPanel.Controls.Add(btnStartOpen);

                        // btnStop [Button - this/MainPanel/ControlPanel/btnStop]
                        btnStop = new Button();
                        btnStop.Text = rm.GetString("btnStop", culture); ;
                        btnStop.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                        btnStop.BackColor = Color.FromArgb(200, 50, 50);
                        btnStop.ForeColor = Color.White;
                        btnStop.Size = new Size(250, 40);
                        btnStop.FlatStyle = FlatStyle.Flat;
                        btnStop.FlatAppearance.BorderSize = 0;
                        btnStop.Padding = new Padding(0);
                        btnStop.Margin = new Padding(0,20,0,0);
                        btnStop.Click += BtnStop_Click;
                        btnStop.Anchor = AnchorStyles.Bottom;
                    ControlPanel.Controls.Add(btnStop);

                        // btnStop [Button - Vertical - this/MainPanel/ControlPanel/btnStop]
                        progressBar = new AnimatedProgressBar();
                        progressBar.Size = new Size(300, 20);
                        progressBar.Visible = false;
                        progressBar.Padding = new Padding(0);
                        progressBar.Margin = new Padding(0,10,0,0);
                        progressBar.Anchor = AnchorStyles.Bottom;
                    ControlPanel.Controls.Add(progressBar);
                MainPanel.Controls.Add(ControlPanel);

                    // ServerInfoPanel [Flow - Vertical - this/MainPanel/ServerInfoPanel]
                    ServerInfoPanel.Anchor = AnchorStyles.None;
                    ServerInfoPanel.FlowDirection = FlowDirection.TopDown;
                    ServerInfoPanel.Size = new Size(300, 105);
                    ServerInfoPanel.Padding = new Padding(0);
                    ServerInfoPanel.Margin = new Padding(0, 0, 0, 0);
                    ServerInfoPanel.Anchor = AnchorStyles.Top;
                    ServerInfoPanel.WrapContents = false;

                        // lblAddressTitle [Label - this/MainPanel/ServerInfoPanel/lblAddressTitle]
                        lblAddressTitle = new Label();
                        lblAddressTitle.Text = rm.GetString("lblAddressTitle", culture);
                        lblAddressTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                        lblAddressTitle.Size = new Size(300, 20);
                        lblAddressTitle.Padding = new Padding(0);
                        lblAddressTitle.Margin = new Padding(0,10,0,0);
                        lblAddressTitle.Anchor = AnchorStyles.Top;
                        lblAddressTitle.BackColor = Color.FromArgb(0, 68, 136);
                        lblAddressTitle.ForeColor = Color.White;
                        lblAddressTitle.TextAlign = ContentAlignment.MiddleCenter;
                    ServerInfoPanel.Controls.Add(lblAddressTitle);

                        // lblIanseoAddress [Label - this/MainPanel/ServerInfoPanel/lblIanseoAddress]
                        lblIanseoAddress = new Label();
                        lblIanseoAddress.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                        lblIanseoAddress.Size = new Size(300, 20);
                        lblIanseoAddress.Padding = new Padding(0);
                        lblIanseoAddress.Margin = new Padding(0,10,0,0);
                        lblIanseoAddress.Anchor = AnchorStyles.Top;
                        lblIanseoAddress.TextAlign = ContentAlignment.MiddleCenter;
                    ServerInfoPanel.Controls.Add(lblIanseoAddress);

                        // LinkPanel [Flow - Horizontal - this/MainPanel/ServerInfoPanel/LinkPanel]
                        LinkPanel.Size = new Size(300, 25);
                        LinkPanel.Padding = new Padding(0);
                        LinkPanel.Margin = new Padding(0);
                        LinkPanel.Anchor = AnchorStyles.Top;

                            // btnCopyLink [Button - this/MainPanel/ServerInfoPanel/LinkPanel/btnCopyLink]
                            btnCopyLink = new Button();
                            btnCopyLink.Text = rm.GetString("btnCopyLink", culture); ;
                            btnCopyLink.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                            btnCopyLink.Size = new Size(100, 25);
                            btnCopyLink.FlatStyle = FlatStyle.Flat;
                            btnCopyLink.FlatAppearance.BorderSize = 1;
                            btnCopyLink.Padding = new Padding(0);
                            btnCopyLink.Margin = new Padding(40, 0, 20, 0);
                            btnCopyLink.Anchor = AnchorStyles.Top;
                            btnCopyLink.Click += BtnCopyLink_Click;
                        LinkPanel.Controls.Add(btnCopyLink);

                            // btnShowQR [Button - this/MainPanel/ServerInfoPanel/LinkPanel/btnShowQR]
                            btnShowQR = new Button();
                            btnShowQR.Text = rm.GetString("btnShowQR", culture);
                            btnShowQR.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                            btnShowQR.Size = new Size(100, 25);
                            btnShowQR.FlatStyle = FlatStyle.Flat;
                            btnShowQR.FlatAppearance.BorderSize = 1;
                            btnShowQR.Padding = new Padding(0);
                            btnShowQR.Margin = new Padding(0);
                            btnShowQR.Anchor = AnchorStyles.Top;
                            btnShowQR.Click += BtnShowQR_Click;
                        LinkPanel.Controls.Add(btnShowQR);
                    ServerInfoPanel.Controls.Add(LinkPanel);
                MainPanel.Controls.Add(ServerInfoPanel);

                    // StatusPanel [Flow - Vertical - this/MainPanel/StatusPanel]
                    StatusPanel.Anchor = AnchorStyles.None;
                    StatusPanel.FlowDirection = FlowDirection.TopDown;
                    StatusPanel.Size = new Size(300, 105);
                    StatusPanel.Padding = new Padding(0);
                    StatusPanel.Margin = new Padding(0, 0, 0, 0);
                    StatusPanel.Anchor = AnchorStyles.Top;
                    StatusPanel.WrapContents = false;

                        // lblStatusTitle [Label - this/MainPanel/StatusPanel/lblStatusTitle]
                        lblStatusTitle = new Label();
                        lblStatusTitle.Text = rm.GetString("lblStatusTitle", culture);
                        lblStatusTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                        lblStatusTitle.Size = new Size(300, 20);
                        lblStatusTitle.Padding = new Padding(0);
                        lblStatusTitle.Margin = new Padding(0,10,0,0);
                        lblStatusTitle.Anchor = AnchorStyles.Top;
                        lblStatusTitle.BackColor = Color.FromArgb(0, 68, 136);
                        lblStatusTitle.ForeColor = Color.White;
                        lblStatusTitle.Anchor = AnchorStyles.None;
                        lblStatusTitle.TextAlign = ContentAlignment.MiddleCenter;
                    StatusPanel.Controls.Add(lblStatusTitle);

                        // StatusSubPanel [Flow - Horizontal - this/MainPanel/StatusPanel/StatusSubPanel]
                        StatusSubPanel.FlowDirection = FlowDirection.LeftToRight;
                        StatusSubPanel.Size = new Size(300, 55);
                        StatusSubPanel.Padding = new Padding(0);
                        StatusSubPanel.Margin = new Padding(0);
                        StatusSubPanel.Anchor = AnchorStyles.Top;

                            // ServicesPanel [Flow - Vertical - this/MainPanel/StatusPanel/StatusSubPanel/ServicesPanel]
                            ServicesPanel.FlowDirection = FlowDirection.TopDown;
                            ServicesPanel.Size = new Size(190, 55);
                            ServicesPanel.Padding = new Padding(0);
                            ServicesPanel.Margin = new Padding(0);
                            ServicesPanel.Anchor = AnchorStyles.Top;

                                // ApachePanel [Flow - Horizontal - this/MainPanel/StatusPanel/StatusSubPanel/ServicesPanel/ApachePanel]
                                ApachePanel.FlowDirection = FlowDirection.LeftToRight;
                                ApachePanel.Size = new Size(160, 20);
                                ApachePanel.Padding = new Padding(0);
                                ApachePanel.Margin = new Padding(30, 10, 0, 0);
                                ApachePanel.Anchor = AnchorStyles.Left;

                                    // dotApache [Dot - this/MainPanel/StatusPanel/StatusSubPanel/ServicesPanel/ApachePanel/dotApache]
                                    dotApache = new Panel();
                                    dotApache.Size = new Size(20, 20);
                                    dotApache.BackColor = Color.Red;
                                    dotApache.Padding = new Padding(0);
                                    dotApache.Margin = new Padding(0);
                                    dotApache.Paint += (s, e) =>
                                        {
                                            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                                            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                                            {
                                                path.AddEllipse(0, 0, dotApache.Width, dotApache.Height);
                                                dotApache.Region = new Region(path);
                                            }
                                        };
                                    dotApache.Region = new Region(new Rectangle(0, 0, dotApache.Width, dotApache.Height));
                                ApachePanel.Controls.Add(dotApache);

                                    // lblApache [Label - this/MainPanel/StatusPanel/StatusSubPanel/ServicesPanel/ApachePanel/lblApache]
                                    lblApache = new Label();
                                    lblApache.Text = rm.GetString("lblApache", culture);
                                    lblApache.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                                    lblApache.Size = new Size(140, 20);
                                    lblApache.Padding = new Padding(0);
                                    lblApache.Margin = new Padding(0, 0, 0, 0);
                                    lblApache.Anchor = AnchorStyles.Top;
                                    lblApache.Anchor = AnchorStyles.None;
                                    lblApache.TextAlign = ContentAlignment.MiddleLeft;
                                ApachePanel.Controls.Add(lblApache);
                            ServicesPanel.Controls.Add(ApachePanel);

                                // MysqlPanel [Flow - Horizontal - this/MainPanel/StatusPanel/StatusSubPanel/ServicesPanel/MysqlPanel]
                                MysqlPanel.FlowDirection = FlowDirection.LeftToRight;
                                MysqlPanel.Size = new Size(160, 20);
                                MysqlPanel.Padding = new Padding(0);
                                MysqlPanel.Margin = new Padding(30, 5, 0, 0);
                                MysqlPanel.Anchor = AnchorStyles.Left;

                                    // dotMySQL [Dot - this/MainPanel/StatusPanel/StatusSubPanel/ServicesPanel/MysqlPanel/dotMySQL]
                                    dotMySQL = new Panel();
                                    dotMySQL.Size = new Size(20, 20);
                                    dotMySQL.BackColor = Color.Red;
                                    dotMySQL.Padding = new Padding(0);
                                    dotMySQL.Margin = new Padding(0);
                                    dotMySQL.Paint += (s, e) =>
                                        {
                                            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                                            using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                                            {
                                                path.AddEllipse(0, 0, dotMySQL.Width, dotMySQL.Height);
                                                dotMySQL.Region = new Region(path);
                                            }
                                        };
                                    dotMySQL.Region = new Region(new Rectangle(0, 0, dotMySQL.Width, dotMySQL.Height));
                                MysqlPanel.Controls.Add(dotMySQL);

                                    // lblMySQL [Label - this/MainPanel/StatusPanel/StatusSubPanel/ServicesPanel/MysqlPanel/lblMySQL]
                                    lblMySQL = new Label();
                                    lblMySQL.Text = rm.GetString("lblMySQL", culture);
                                    lblMySQL.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                                    lblMySQL.Size = new Size(140, 20);
                                    lblMySQL.Padding = new Padding(0);
                                    lblMySQL.Margin = new Padding(0, 0, 0, 0);
                                    lblMySQL.Anchor = AnchorStyles.Top;
                                    lblMySQL.Anchor = AnchorStyles.None;
                                    lblMySQL.TextAlign = ContentAlignment.MiddleLeft;
                                MysqlPanel.Controls.Add(lblMySQL);
                            ServicesPanel.Controls.Add(MysqlPanel);
                        StatusSubPanel.Controls.Add(ServicesPanel);

                            // btnOpenXamppControl [Button - this/MainPanel/StatusPanel/StatusSubPanel/btnOpenXamppControl]
                            btnOpenXamppControl = new Button();
                            btnOpenXamppControl.Text = rm.GetString("btnOpenXamppControl", culture);
                            btnOpenXamppControl.Font = new Font("Segoe UI", 7, FontStyle.Regular);
                            btnOpenXamppControl.BackColor = Color.FromArgb(251, 122, 36);
                            btnOpenXamppControl.ForeColor = Color.White;
                            btnOpenXamppControl.Size = new Size(80, 35);
                            btnOpenXamppControl.FlatStyle = FlatStyle.Flat;
                            btnOpenXamppControl.FlatAppearance.BorderSize = 0;
                            btnOpenXamppControl.Padding = new Padding(0);
                            btnOpenXamppControl.Margin = new Padding(25, 20, 0, 0);
                            btnOpenXamppControl.Anchor = AnchorStyles.Top;
                            btnOpenXamppControl.Click += BtnOpenXamppControl_Click;
                        StatusSubPanel.Controls.Add(btnOpenXamppControl);
                    StatusPanel.Controls.Add(StatusSubPanel);
                MainPanel.Controls.Add(StatusPanel);

                    // RepairPanel [Flow - Vertical - this/MainPanel/RepairPanel]
                    RepairPanel.Anchor = AnchorStyles.None;
                    RepairPanel.FlowDirection = FlowDirection.TopDown;
                    RepairPanel.Size = new Size(300, 145);
                    RepairPanel.Padding = new Padding(0);
                    RepairPanel.Margin = new Padding(0, 0, 0, 0);
                    RepairPanel.Anchor = AnchorStyles.Top;
                    RepairPanel.WrapContents = false;

                        // lblRepairTitle [Label - this/MainPanel/RepairPanel/lblRepairTitle]
                        lblRepairTitle = new Label();
                        lblRepairTitle.Text = rm.GetString("lblRepairTitle", culture);
                        lblRepairTitle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                        lblRepairTitle.Size = new Size(300, 20);
                        lblRepairTitle.Padding = new Padding(0);
                        lblRepairTitle.Margin = new Padding(0, 10, 0, 0);
                        lblRepairTitle.Anchor = AnchorStyles.Top;
                        lblRepairTitle.BackColor = Color.FromArgb(0, 68, 136);
                        lblRepairTitle.ForeColor = Color.White;
                        lblRepairTitle.TextAlign = ContentAlignment.MiddleCenter;
                    RepairPanel.Controls.Add(lblRepairTitle);

                        // lblAdminMessage [Label - this/MainPanel/RepairPanel/lblAdminMessage]
                        lblAdminMessage = new Label();
                        lblAdminMessage.Text = rm.GetString("lblAdminMessage", culture);
                        lblAdminMessage.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                        lblAdminMessage.Size = new Size(280, 60);
                        lblAdminMessage.Padding = new Padding(0);
                        lblAdminMessage.Margin = new Padding(0, 10, 0, 0);
                        lblAdminMessage.Anchor = AnchorStyles.Top;
                        lblAdminMessage.ForeColor = Color.Black;
                        lblAdminMessage.TextAlign = ContentAlignment.MiddleCenter;
                        lblAdminMessage.Padding = new Padding(0);

                        // btnRunAsAdmin [Button - this/MainPanel/RepairPanel/btnRunAsAdmin]
                        btnRunAsAdmin = new Button();
                        btnRunAsAdmin.Text = rm.GetString("btnRunAsAdmin", culture);
                        btnRunAsAdmin.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                        btnRunAsAdmin.Size = new Size(290, 25);
                        btnRunAsAdmin.FlatStyle = FlatStyle.Flat;
                        btnRunAsAdmin.FlatAppearance.BorderSize = 0;
                        btnRunAsAdmin.Padding = new Padding(0);
                        btnRunAsAdmin.Margin = new Padding(0);
                        btnRunAsAdmin.Anchor = AnchorStyles.Top;
                        btnRunAsAdmin.ForeColor = Color.Yellow;
                        btnRunAsAdmin.BackColor = Color.FromArgb(50, 50, 70);
                        btnRunAsAdmin.Click += BtnRunAsAdmin_Click;

                        // btnRepairMySQL [Button - this/MainPanel/RepairPanel/btnRepairMySQL]
                        btnRepairMySQL = new Button();
                        btnRepairMySQL.Text = rm.GetString("btnRepairMySQL", culture);
                        btnRepairMySQL.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                        btnRepairMySQL.Size = new Size(290, 25);
                        btnRepairMySQL.FlatStyle = FlatStyle.Flat;
                        btnRepairMySQL.FlatAppearance.BorderSize = 0;
                        btnRepairMySQL.Padding = new Padding(0);
                        btnRepairMySQL.Margin = new Padding(0, 10, 0, 0);
                        btnRepairMySQL.Anchor = AnchorStyles.Top;
                        btnRepairMySQL.ForeColor = Color.Yellow;
                        btnRepairMySQL.BackColor = Color.FromArgb(50, 50, 70);
                        btnRepairMySQL.Click += BtnRepairMySQL_Click;

                        // btnEnableStartup [Button - this/MainPanel/RepairPanel/btnEnableStartup]
                        btnEnableStartup = new Button();
                        btnEnableStartup.Text = rm.GetString("btnEnableStartup", culture);
                        btnEnableStartup.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                        btnEnableStartup.Size = new Size(290, 25);
                        btnEnableStartup.FlatStyle = FlatStyle.Flat;
                        btnEnableStartup.FlatAppearance.BorderSize = 0;
                        btnEnableStartup.Padding = new Padding(0);
                        btnEnableStartup.Margin = new Padding(0, 5, 0, 0);
                        btnEnableStartup.Anchor = AnchorStyles.Top;
                        btnEnableStartup.ForeColor = Color.Yellow;
                        btnEnableStartup.BackColor = Color.FromArgb(50, 50, 70);
                        btnEnableStartup.Click += BtnSetAsService_Click;

                        // btnWindowsDefender [Button - this/MainPanel/RepairPanel/btnWindowsDefender]
                        btnWindowsDefender = new Button();
                        btnWindowsDefender.Text = rm.GetString("btnWindowsDefender", culture);
                        btnWindowsDefender.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                        btnWindowsDefender.Size = new Size(290, 25);
                        btnWindowsDefender.FlatStyle = FlatStyle.Flat;
                        btnWindowsDefender.FlatAppearance.BorderSize = 0;
                        btnWindowsDefender.Padding = new Padding(0);
                        btnWindowsDefender.Margin = new Padding(0, 5, 0, 0);
                        btnWindowsDefender.Anchor = AnchorStyles.Top;
                        btnWindowsDefender.ForeColor = Color.Yellow;
                        btnWindowsDefender.BackColor = Color.FromArgb(50, 50, 70);
                        btnWindowsDefender.Click += BtnWindowsDefender_Click;
                MainPanel.Controls.Add(RepairPanel);
            
            if (!IsAdministrator()) //If not admin, displays the admin button
            {
                RepairPanel.Controls.Add(lblAdminMessage);
                RepairPanel.Controls.Add(btnRunAsAdmin);
            }
            else //Else, displays the function buttons
            {
                RepairPanel.Controls.Add(btnRepairMySQL);
                RepairPanel.Controls.Add(btnEnableStartup);
                RepairPanel.Controls.Add(btnWindowsDefender);
            }
            this.Controls.Add(MainPanel);
        }
        private void CheckInitialServiceStatus() // Check the status of Apache and MySQL processes
        {
            bool apacheRunning = Process.GetProcessesByName("httpd").Any();
            bool mysqlRunning = Process.GetProcessesByName("mysqld").Any();
            dotApache.BackColor = apacheRunning ? Color.Green : Color.Red;
            dotApache.Invalidate();
            dotMySQL.BackColor = mysqlRunning ? Color.Green : Color.Red;
            dotMySQL.Invalidate();
            servicesRunning = apacheRunning && mysqlRunning;
            desiredState = ServiceState.Unknown;
        }
        private void InitTimer() // Initialization of the timer that checks the status of Apache and MySQL every second
        {
            checkTimer = new System.Windows.Forms.Timer();
            checkTimer.Interval = 1000; // here to change the interval (in milliseconds)
            checkTimer.Tick += CheckServices;
            checkTimer.Start();
        }
        private async void CheckServices(object sender, EventArgs e) // Check the status of Apache and MySQL processes and services ///////////////////????what if mysql shutdown unespectedly???//////////////
        {
            // Check processes to determine status
            bool apacheRunning = Process.GetProcessesByName("httpd").Any();
            bool mysqlRunning = Process.GetProcessesByName("mysqld").Any();
            dotApache.BackColor = apacheRunning ? Color.Green : Color.Red;
            dotApache.Invalidate();
            dotMySQL.BackColor = mysqlRunning ? Color.Green : Color.Red;
            dotMySQL.Invalidate();

            // Check Windows services to determine auto / manuel mode
            bool apacheServiceRunning = await IsServiceRunning(apacheServiceName);
            bool mysqlServiceRunning = await IsServiceRunning(mysqlServiceName);
            lblApache.Text = rm.GetString("lblApache", culture) + (apacheRunning ? (apacheServiceRunning ? " (auto)" : " (manual)") : "");
            lblMySQL.Text = rm.GetString("lblMySQL", culture) + (mysqlRunning ? (mysqlServiceRunning ? " (auto)" : " (manual)") : "");

            bool allRunning = apacheRunning && mysqlRunning;
            bool allStopped = !apacheRunning && !mysqlRunning;
            if (desiredState == ServiceState.Running && allRunning)
            {
                servicesRunning = true;
                desiredState = ServiceState.Unknown;
                UpdateUIByServiceStatus();
                StopProgressBarSafe();
            }
            else if (desiredState == ServiceState.Stopped && allStopped)
            {
                servicesRunning = false;
                desiredState = ServiceState.Unknown;
                UpdateUIByServiceStatus();
                StopProgressBarSafe();
            }
        }
        private void UpdateUIByServiceStatus() // Set the UI elements according to the status of the services
        {
            if (servicesRunning) // If httpd & mysqld are running, switch "start" to "open" and show "stop" and links elements
            {
                btnStartOpen.Text = rm.GetString("btnStartOpen_Open", culture);
                btnStartOpen.BackColor = Color.FromArgb(0, 68, 136);
                btnStop.Visible = true;
                btnStop.Enabled = true;
                btnStop.Text = rm.GetString("btnStop", culture);
                lblAddressTitle.Visible = true;
                lblAddressTitle.Enabled = true;
                lblIanseoAddress.Visible = true;
                lblIanseoAddress.Enabled = true;
                btnCopyLink.Visible = true;
                btnCopyLink.Enabled = true;
                btnShowQR.Visible = true;
                btnShowQR.Enabled = true;
                LinkPanel.Visible = true;
                btnStartOpen.BackColor = Color.FromArgb(0, 68, 136);//Green
                btnStartOpen.ForeColor = Color.White;//Black
                btnStop.Margin = new Padding(0, 20, 0, 0);
                btnStartOpen.Margin = new Padding(5, 10, 0, 0);
            }
            else // If httpd & mysqld are not running, switch "open" to "start" and hide "stop" and links elements
            {
                btnStartOpen.Text = rm.GetString("btnStartOpen_Run", culture);
                btnStartOpen.BackColor = Color.FromArgb(0, 68, 136);
                btnStop.Visible = false;
                btnStop.Enabled = false;
                btnStop.Text = rm.GetString("btnStop", culture);
                lblAddressTitle.Visible = false;
                lblAddressTitle.Enabled = false;
                lblIanseoAddress.Visible = false;
                lblIanseoAddress.Enabled = false;
                btnCopyLink.Visible = false;
                btnCopyLink.Enabled = false;
                btnShowQR.Visible = false;
                btnShowQR.Enabled = false;
                LinkPanel.Visible = false;
                btnStartOpen.BackColor = Color.Green;
                btnStop.Margin = new Padding(0, 80, 0, 0);
                btnStartOpen.Margin = new Padding(5, 75, 0, 0);
            }
            btnStartOpen.Enabled = true;
            btnStartOpen.Visible = true;
            UpdateIanseoAddress();

        }
        private void UpdateIanseoAddress() // Set the Ianseo address label with the current IP and port
        {
            string ip = GetLocalIPAddress();
            int port = ReadApachePort();
            lblIanseoAddress.Text = $"{ip}:{port}";
        }


        //* Utility Methods *//
        private string GetLocalIPAddress() // Get the local IP address of the machine and return 127.0.0.1 if not found
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1";
        }
        private int ReadApachePort() // Read the Apache port from httpd.conf, return 80 if not found
        {
            try
            {
                string confPath = @httpdConfPath;
                var lines = System.IO.File.ReadAllLines(confPath);
                foreach (var line in lines)
                {
                    if (line.Trim().StartsWith("Listen"))
                    {
                        string[] parts = line.Split(' ');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int port))
                            return port;
                    }
                }
            }
            catch { }
            return 80;
        }
        private void OpenBrowser() // Open the default web browser to the Ianseo address
        {
            try
            {
                string ip = GetLocalIPAddress();
                int port = ReadApachePort();
                string url = $"http://{ip}:{port}/";

                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    rm.GetString("lblErrorOpenBrowserAlt", culture) + ex.Message,
                    rm.GetString("lblError", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private async Task<(int exitCode, string output, string error)> RunCommandAsync(string fileName, string arguments) // Run a command line process and capture output and error
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var proc = new Process { StartInfo = psi })
            {
                var outputBuilder = new System.Text.StringBuilder();
                var errorBuilder = new System.Text.StringBuilder();

                proc.OutputDataReceived += (s, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
                proc.ErrorDataReceived += (s, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                await Task.Run(() => proc.WaitForExit());
                return (proc.ExitCode, outputBuilder.ToString(), errorBuilder.ToString());
            }
        }
        async Task<bool> ServiceExists(string name) // Check if a Windows service exists, by default returns false
        {
            var (code, outp, err) = await RunCommandAsync("sc.exe", $"query \"{name}\""); // If sc.exe return "1060" in output = service does not exist; if "STATE" or "SERVICE_NAME" in output = service exists
            if (!string.IsNullOrEmpty(err) && err.Contains("1060")) return false;
            if (outp.IndexOf("FAILED", StringComparison.OrdinalIgnoreCase) >= 0 && outp.IndexOf("1060", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (outp.IndexOf("STATE", StringComparison.OrdinalIgnoreCase) >= 0 || outp.IndexOf("SERVICE_NAME", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }
        private async Task<bool> IsServiceRunning(string serviceName) // Check if a Windows service is running
        {
            try
            {
                var result = await RunCommandAsync("sc.exe", $"query \"{serviceName}\""); // If sc.exe return "RUNNING" in output = service is running
                return result.output.Contains("RUNNING");
            }
            catch
            {
                return false;
            }
        }
        private bool IsAdministrator() // Check if the application is running with administrator privileges
        {
            using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
            {
                var principal = new System.Security.Principal.WindowsPrincipal(identity);
                return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
            }
        }
        private async Task RunProcessAndWait(string filePath) // Run a process, wait for its completion and manage progress bar
        {
            try
            {
                StartProgressBar();
                Process process = new Process();
                process.StartInfo.FileName = filePath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                await Task.Run(() => process.WaitForExit());
                await FinishProgressBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    rm.GetString("lblErrorExecution", culture) + ex.Message,
                    rm.GetString("lblError", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void CopyDirectory(string sourceDir, string destDir) // Copy all files and subdirectories from sourceDir to destDir (merge, overwrite existing files)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir)) // Copy all files
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (var dir in Directory.GetDirectories(sourceDir)) // Copy all subdirectories recursively
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }


        //* Button Handlers *//
        private async void BtnStartOpen_Click(object sender, EventArgs e) // Start xampp_start.exe or open browser depending on service status
        {
            if (!servicesRunning)
            {
                desiredState = ServiceState.Running;
                btnStartOpen.Text = rm.GetString("btnStartOpen_Starting", culture);
                btnStartOpen.Enabled = false;
                btnStop.Enabled = false;
                progressBar.Margin = new Padding(0, 10, 0, 0);
                await RunProcessAndWait(@xamppStartPath);
            }
            else
            {
                try
                {
                    OpenBrowser();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorOpenBrowser", culture) + ex.Message,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }
        private async void BtnStop_Click(object sender, EventArgs e) // Stop with xampp_stop.exe and services if needed
        {
            desiredState = ServiceState.Stopped; // Set UI to stopping state
            btnStop.Text = rm.GetString("btnStop_Stopping", culture);
            btnStop.Enabled = false;
            btnStartOpen.Enabled = false;
            btnStartOpen.Visible = false;
            btnStop.Margin = new Padding(0, 80, 0, 0);
            progressBar.Margin = new Padding(0, 15, 0, 0);
            lblAddressTitle.Visible = false;
            lblAddressTitle.Enabled = false;
            lblIanseoAddress.Visible = false;
            lblIanseoAddress.Enabled = false;
            btnCopyLink.Visible = false;
            btnCopyLink.Enabled = false;
            btnShowQR.Visible = false;
            btnShowQR.Enabled = false;
            LinkPanel.Visible = false;
            bool apacheExists = await ServiceExists(apacheServiceName); // Check services
            bool mysqlExists = await ServiceExists(mysqlServiceName);
            bool apacheServiceRunning = apacheExists && await IsServiceRunning(apacheServiceName);
            bool mysqlServiceRunning = mysqlExists && await IsServiceRunning(mysqlServiceName);
            if (apacheServiceRunning || mysqlServiceRunning) // If at least one service is running → we must stop via sc.exe with admin rights
            {
                if (!IsAdministrator())
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorStopNotAdministrator", culture),
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    UpdateUIByServiceStatus();
                    return;
                }
                else
                {
                    if (apacheServiceRunning)
                        await RunCommandAsync("sc.exe", $"stop \"{apacheServiceName}\"");

                    if (mysqlServiceRunning)
                        await RunCommandAsync("sc.exe", $"stop \"{mysqlServiceName}\"");
                }

            }
            await RunProcessAndWait(xamppStopPath); // Always run xampp_stop.exe to ensure proper shutdown
        }
        private void BtnCopyLink_Click(object sender, EventArgs e) // Copy Ianseo address to clipboard
        {
            try
            {
                Clipboard.SetText($"{GetLocalIPAddress()}:{ReadApachePort()}");
                MessageBox.Show(
                    rm.GetString("lblLinkCopied", culture),
                    rm.GetString("lblLinkCopiedTitle", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    rm.GetString("lblErrorLinkCopied", culture) + ex.Message,
                    rm.GetString("lblError", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void BtnShowQR_Click(object sender, EventArgs e) // Open a dialog with the QR code of the Ianseo address, needs QRCoder library (QRCoder.dll)
        {
            try
            {
                string url = $"http://{GetLocalIPAddress()}:{ReadApachePort()}/";

                using (var qrGenerator = new QRCodeGenerator())
                using (var qrData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q))
                using (var qrCode = new QRCode(qrData))
                {
                    Bitmap qrImage = qrCode.GetGraphic(20);

                    Form qrForm = new Form();
                    qrForm.Text = rm.GetString("lblShowQRTitle", culture);
                    qrForm.ClientSize = new Size(qrImage.Width + 20, qrImage.Height + 20);
                    qrForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    qrForm.StartPosition = FormStartPosition.CenterParent;
                    qrForm.MaximizeBox = false;
                    qrForm.MinimizeBox = false;

                    PictureBox pb = new PictureBox();
                    pb.Image = qrImage;
                    pb.SizeMode = PictureBoxSizeMode.AutoSize;
                    pb.Location = new Point(10, 10);

                    qrForm.Controls.Add(pb);
                    qrForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    rm.GetString("lblErrorShowQR", culture) + ex.Message,
                    rm.GetString("lblError", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void BtnOpenXamppControl_Click(object sender, EventArgs e) // Open XAMPP Control Panel
        {
            try
            {
                Process.Start(@xamppControlPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    rm.GetString("lblErrorOpenXamppControl", culture) + ex.Message,
                    rm.GetString("lblError", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void BtnRunAsAdmin_Click(object sender, EventArgs e) // Close current instance and relaunch as administrator
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(exePath)
                {
                    UseShellExecute = true,
                    Verb = "runas"
                };
                System.Diagnostics.Process.Start(psi);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    rm.GetString("lblErrorRunAsAdmin", culture) + ex.Message,
                    rm.GetString("lblError", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private async void BtnRepairMySQL_Click(object sender, EventArgs e) // Repair MySQL by restoring from backup
        {
            try
            {
                string mysqlDir = Path.Combine(basePath, "mysql");
                string dataDir = Path.Combine(mysqlDir, "data");
                string backupDir = Path.Combine(mysqlDir, "backup");
                string dataOldDir = Path.Combine(mysqlDir, "data_old");
                string ibdataPath = Path.Combine(dataDir, "ibdata1");
                try // Close XAMPP / MySQL / Apache if running
                {
                    foreach (var p in Process.GetProcessesByName("xampp-control"))
                        p.Kill();
                }
                catch { /* ignore */ }
                try
                {
                    foreach (var p in Process.GetProcessesByName("mysqld"))
                        p.Kill();
                }
                catch { /* ignore */ }
                try
                {
                    foreach (var p in Process.GetProcessesByName("httpd"))
                        p.Kill();
                }
                catch { /* ignore */ }
                System.Threading.Thread.Sleep(800);
                if (!Directory.Exists(mysqlDir)) // Check existence of mysql, data, backup folders and ibdata1 file
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorMysqlDir", culture) + mysqlDir,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                if (!Directory.Exists(dataDir))
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorDataDir", culture) + mysqlDir,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                if (!Directory.Exists(backupDir))
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorBackupDir", culture) + mysqlDir,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                if (!File.Exists(ibdataPath))
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorIbdataPath", culture),
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                if (Directory.Exists(dataOldDir)) // Delete data_old if it already exists
                {
                    try
                    {
                        Directory.Delete(dataOldDir, true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            rm.GetString("lblErrorDelDataOldDir", culture) + ex.Message,
                            rm.GetString("lblError", culture),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return;
                    }
                }

                try // Rename data to data_old and delete data
                {
                    Directory.Move(dataDir, dataOldDir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorRenameDataDir", culture) + ex.Message,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                try // Copy backup to data
                {
                    CopyDirectory(backupDir, dataDir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorCopyBackupDir", culture) + ex.Message,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                try // Copy subdirectories from data_old except excluded ones
                {
                    string[] exclude = new string[] { "mysql", "performance_schema", "phpmyadmin" };
                    if (Directory.Exists(dataOldDir))
                    {
                        foreach (string dir in Directory.GetDirectories(dataOldDir))
                        {
                            string name = Path.GetFileName(dir);
                            if (exclude.Contains(name, StringComparer.OrdinalIgnoreCase))
                                continue;
                            string dest = Path.Combine(dataDir, name);
                            CopyDirectory(dir, dest); // If dest exists, overwrite/merge
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            rm.GetString("lblErrorDataOldDir", culture),
                            rm.GetString("lblError", culture),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorCopyDataOldDir", culture) + ex.Message,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                try // Copy ibdata1 from data_old to data (overwrite if necessary)
                {
                    string ibdataOld = Path.Combine(dataOldDir, "ibdata1");
                    if (File.Exists(ibdataOld))
                    {
                        File.Copy(ibdataOld, Path.Combine(dataDir, "ibdata1"), true);
                    }
                    else
                    {
                        MessageBox.Show(
                            rm.GetString("lblWarningIbdata1", culture),
                            rm.GetString("lblWarning", culture),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorCopyIbdata1", culture) + ex.Message,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                try // Launch xampp_start.exe
                {
                    StartProgressBar();
                    Process process = new Process();
                    process.StartInfo.FileName = @xamppStartPath;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();
                    await FinishProgressBar();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorXamppRestart", culture) + ex.Message,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
                try // Recreate MySQL user ianseo with all privileges on ianseo database
                {
                    string mysqlBin = Path.Combine(basePath, "mysql", "bin", "mysql.exe");
                    if (File.Exists(mysqlBin))
                    {
                        string sqlCommands = "drop user ianseo@localhost; " +
                                             "create user ianseo@localhost identified by 'ianseo'; " +
                                             "grant all privileges on ianseo.* to ianseo@localhost; ";
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = mysqlBin,
                            Arguments = "-u root",
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using (Process mysqlProc = new Process())
                        {
                            mysqlProc.StartInfo = psi;
                            mysqlProc.Start();
                            mysqlProc.StandardInput.WriteLine(sqlCommands); // Send SQL commands to MySQL
                            mysqlProc.StandardInput.WriteLine("exit");
                            mysqlProc.StandardInput.Close();
                            string output = mysqlProc.StandardOutput.ReadToEnd(); // Read outputs for potential debugging
                            string error = mysqlProc.StandardError.ReadToEnd();
                            mysqlProc.WaitForExit();
                            if (!string.IsNullOrEmpty(error))
                            {
                                MessageBox.Show(
                                    rm.GetString("lblErrorMySQL", culture) + error,
                                    rm.GetString("lblError", culture),
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                                );
                            }
                            else
                            {
                                MessageBox.Show(
                                    rm.GetString("lblMySQLRepaired", culture),
                                    rm.GetString("lblMySQLRepairedTitle", culture),
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information
                                );
                                desiredState = ServiceState.Running;
                                btnStartOpen.Text = rm.GetString("btnStartOpen_Starting", culture);
                                btnStartOpen.Enabled = false;
                                btnStop.Enabled = false;
                                progressBar.Margin = new Padding(0, 10, 0, 0);
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            rm.GetString("lblWarningMysqlBin", culture) + mysqlBin,
                            rm.GetString("lblWarning", culture),
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorMysqlUserCreation", culture) + ex.Message,
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    rm.GetString("lblErrorRepairMySQL", culture) + ex.Message,
                    rm.GetString("lblError", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private async void BtnSetAsService_Click(object sender, EventArgs e) // Configure Apache and MySQL as Windows services and enable them in xampp-control.ini
        {
            try
            {
                string iniPath = Path.Combine(basePath, "xampp-control.ini");
                if (!File.Exists(iniPath))
                {
                    MessageBox.Show(
                        rm.GetString("lblErrorXamppIni", culture),
                        rm.GetString("lblError", culture),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                StartProgressBar();
                var lines = File.ReadAllLines(iniPath).ToList();
                int idx = lines.FindIndex(l => l.Trim().Equals("[EnableServices]", StringComparison.OrdinalIgnoreCase)); // Find [EnableServices] section in the INI file
                if (idx >= 0)
                {
                    int i = idx + 1;
                    while (i < lines.Count && !lines[i].TrimStart().StartsWith("["))
                    {
                        string trimmed = lines[i].TrimStart();
                        if (trimmed.StartsWith("Apache=", StringComparison.OrdinalIgnoreCase)) // Modify Apache= line
                        {
                            lines[i] = "Apache=1";
                        }
                        else if (trimmed.StartsWith("MySQL=", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("MySql=", StringComparison.OrdinalIgnoreCase)) // Modify MySQL= line
                        {
                            lines[i] = "MySQL=1";
                        }
                        i++;
                    }
                    File.WriteAllLines(iniPath, lines);
                }
                else // If no [EnableServices] section found then add it at the end
                {
                    lines.Add("");
                    lines.Add("[EnableServices]");
                    lines.Add("Apache=1");
                    lines.Add("MySQL=1");
                    File.WriteAllLines(iniPath, lines);
                }
                if (!string.IsNullOrEmpty(apacheBin) && File.Exists(apacheBin)) // Remove the Apache service if it exists and create the Apache service
                {
                    bool apacheExists = await ServiceExists(apacheServiceName);
                    if (apacheExists)
                    {
                        await RunCommandAsync("sc.exe", $"stop \"{apacheServiceName}\"");
                        await RunCommandAsync("sc.exe", $"delete \"{apacheServiceName}\"");
                        await Task.Delay(1000);
                    }
                    string createCmd = $"create \"{apacheServiceName}\" binPath= \"{apacheBin} -k runservice\" start= auto";
                    await RunCommandAsync("sc.exe", createCmd);
                }

                if (!string.IsNullOrEmpty(mysqlBin) && File.Exists(mysqlBin)) // Remove the MySQL service if it exists and create the Apache service
                {
                    bool mysqlExists = await ServiceExists(mysqlServiceName);
                    if (mysqlExists)
                    {
                        await RunCommandAsync("sc.exe", $"stop \"{mysqlServiceName}\"");
                        await RunCommandAsync("sc.exe", $"delete \"{mysqlServiceName}\"");
                        await Task.Delay(1000);
                    }

                    string myIni = Path.Combine(basePath, "mysql", "bin", "my.ini");
                    await RunCommandAsync(mysqlBin, $"--install Ianseo_MySQL --defaults-file=\"{myIni}\"");
                }
                await Task.Delay(1000);
                await RunCommandAsync("net.exe", $"start \"{apacheServiceName}\""); // Start the services
                await RunCommandAsync("net.exe", $"start \"{mysqlServiceName}\"");
                await FinishProgressBar();
                MessageBox.Show(
                    rm.GetString("lblSetAsService", culture),
                    rm.GetString("lblSetAsServiceTitle", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                desiredState = ServiceState.Running;
                btnStartOpen.Text = rm.GetString("btnStartOpen_Starting", culture);
                btnStartOpen.Enabled = false;
                btnStop.Enabled = false;
                progressBar.Margin = new Padding(0, 10, 0, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    rm.GetString("lblErrorSetAsService", culture) + ex.Message,
                    rm.GetString("lblError", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
        private void BtnWindowsDefender_Click(object sender, EventArgs e) // Add exclusions to Windows Defender via PowerShell
        {
            try
            {
                string[] defenderCommands = new string[] // PowwerShell commands to add exclusions
                {
                    @"Add-MpPreference -ExclusionPath '"+basePath+"'",
                    @"Add-MpPreference -ExclusionExtension '.mysql', '.php'",
                    @"Add-MpPreference -ExclusionProcess 'httpd.exe', 'mysqld.exe', 'xampp-control.exe'"
                };
                string command = string.Join(" ; ", defenderCommands); // Combine commands with semicolon
                ProcessStartInfo psi = new ProcessStartInfo // Start PowerShell as admin
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                };
                Process.Start(psi);
                MessageBox.Show(
                    rm.GetString("lblWindowsDefenderExclusions", culture),
                    rm.GetString("lblWindowsDefenderExclusionsTitle", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    rm.GetString("lblErrorWindowsDefenderExclusions", culture) + ex.Message,
                    rm.GetString("lblError", culture),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        //* Progress Bar Management *//
        private void StartProgressBar() // Launch a fake progress bar to reassure the user and keep them waiting
        {
            isProgressRunning = true;
            progressBar.Progress = 0;
            progressBar.Visible = true;
            progressBar.StartAnimation();
            progressStartTime = DateTime.Now;
            if (progressTimer != null)
            {
                progressTimer.Stop();
                progressTimer.Dispose();
            }
            progressTimer = new System.Windows.Forms.Timer();
            progressTimer.Interval = 200;
            progressTimer.Tick += ProgressTimer_Tick;
            progressTimer.Start();
        }
        private void StopProgressBar() // Stop the progress bar
        {
            isProgressRunning = false;
            progressBar.StopAnimation();
            progressBar.Visible = false;

            if (progressTimer != null)
            {
                progressTimer.Stop();
                progressTimer.Dispose();
                progressTimer = null;
            }
        }
        public void StopProgressBarSafe() // Thread-safe stop progress bar
        {
            if (InvokeRequired)
            {
                Invoke((Action)(() => StopProgressBarSafe()));
                return;
            }
            StopProgressBar();
        }
        private async Task FinishProgressBar() // Finish the progress bar quickly to 100%
        {
            if (!isProgressRunning) return;
            float current = progressBar.Progress;
            float target = 100f;
            int steps = 10;
            int delay = 50;
            for (int i = 1; i <= steps; i++) // Quickly increase to 100% in 0.5 seconds
            {
                progressBar.Progress = current + (target - current) * i / steps;
                await Task.Delay(delay);
            }
        }
        private Random rnd = new Random();
        private void ProgressTimer_Tick(object sender, EventArgs e) // Timer tick to update progress bar
        {
            if (!isProgressRunning) return;
            double elapsedSeconds = (DateTime.Now - progressStartTime).TotalSeconds;
            double totalDuration = 10.0; // 10 seconds to reach 90%
            double targetProgress; // Target progression over time (deceleration curve)
            if (elapsedSeconds >= totalDuration)
            {
                targetProgress = 90;
            }
            else
            {
                targetProgress = 90 * (1 - Math.Exp(-elapsedSeconds * 0.5)); // Faster progression at the beginning, then slows down (logarithmic)
                if (targetProgress > 90) targetProgress = 90;
            }
            float current = progressBar.Progress;
            if (current < targetProgress)
            {
                float step = (float)(rnd.NextDouble() * 4.9 + 0.1); // Random advance between 0.1 and 5.0%
                float newProgress = current + step;
                if (newProgress > targetProgress) newProgress = (float)targetProgress;
                progressBar.Progress = newProgress;
            }
        }

    }

    public class AnimatedProgressBar : Control // New class for animated progress bar from scratch
    {
        private System.Windows.Forms.Timer animationTimer;
        private float progress = 0f;       // 0-100 %
        private float animationOffset = 0; // clear band animation
        public AnimatedProgressBar()
        {
            this.SetStyle(ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.UserPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);

            this.Height = 20;

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 40; // 25 fps
            animationTimer.Tick += AnimationTimer_Tick;
        }
        public float Progress
        {
            get => progress;
            set
            {
                progress = Math.Min(100f, Math.Max(0f, value));
                this.Invalidate();
            }
        }
        public void StartAnimation() => animationTimer.Start();
        public void StopAnimation() => animationTimer.Stop();
        private void AnimationTimer_Tick(object sender, EventArgs e) // Clear band effect offset (scrolling)
        {
            animationOffset += 4;
            if (animationOffset > this.Width * 2)
                animationOffset = 0;
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = this.ClientRectangle;
            g.Clear(Color.LightGray);
            int fillWidth = (int)(rect.Width * (progress / 100f));
            if (fillWidth <= 0) return;
            var fillRect = new Rectangle(rect.X, rect.Y, fillWidth, rect.Height);
            using (var brush = new SolidBrush(Color.FromArgb(0, 68, 136))) // Fill main bar with solid color
            {
                g.FillRectangle(brush, fillRect);
            }
            using (var path = new GraphicsPath()) // Draw the animated light strip
            {
                int bandWidth = 60;
                int bandHeight = rect.Height;
                var bandRect = new Rectangle((int)(animationOffset - bandWidth), 0, bandWidth, bandHeight); // Offset strip creation
                path.AddRectangle(bandRect);
                g.SetClip(fillRect); // Clip to filled area (do not exceed the filled bar)
                using (var brush = new LinearGradientBrush(bandRect, Color.FromArgb(120, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Horizontal)) // Semi-transparent light band (white with alpha)
                {
                    g.FillPath(brush, path);
                }

                g.ResetClip();
            }
            using (var pen = new Pen(Color.DarkGray)) // Border drawing
            {
                g.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
            }
        }
    }

}
