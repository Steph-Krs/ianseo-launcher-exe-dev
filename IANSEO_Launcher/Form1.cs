using QRCoder;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System;
using System.IO;
using System.Reflection;

namespace IANSEO_Launcher
{
    

    public partial class MainForm : Form
    {
        private ResourceManager rm;
        private Button btnStartOpen;
        private Button btnStop;
        private Label lblEtat;
        private Label lblApache;
        private Label lblMySQL;
        private Panel dotApache;
        private Panel dotMySQL;

        private System.Windows.Forms.Timer checkTimer;
        private bool servicesRunning = false;

        private enum ServiceState { Unknown, Running, Stopped }
        private ServiceState desiredState = ServiceState.Unknown;

        private AnimatedProgressBar progressBar;
        private System.Windows.Forms.Timer progressTimer;
        private DateTime progressStartTime;
        private bool isProgressRunning = false;

        private Label lblIanseoAddress;
        private Label lblAddress;
        private Button btnCopyLink;
        private Button btnShowQR;

        private Button btnOpenXamppControl;

        FlowLayoutPanel LinkPanel = new FlowLayoutPanel();
        FlowLayoutPanel ControlBloc = new FlowLayoutPanel();
        FlowLayoutPanel ServerBloc = new FlowLayoutPanel();
        FlowLayoutPanel StatusBloc = new FlowLayoutPanel();
        FlowLayoutPanel apachePanel = new FlowLayoutPanel();
        FlowLayoutPanel mysqlPanel = new FlowLayoutPanel();
        FlowLayoutPanel servicesPanel = new FlowLayoutPanel();
        FlowLayoutPanel etatPanel = new FlowLayoutPanel();
        FlowLayoutPanel mainPanel = new FlowLayoutPanel();

        // appliquer la culture de l'interface utilisateur courante
        CultureInfo culture = Thread.CurrentThread.CurrentUICulture;

        private string exePath;
        private string exeDir;
        private string basePath;
        private string xamppStartPath;
        private string xamppStopPath;
        private string httpdConfPath;
        private string xamppControlPath;


        public MainForm()
        {
            // Chargement des ressources embarquées
            rm = new ResourceManager("IANSEO_Launcher.Properties.Resources", typeof(MainForm).Assembly);


            // Chemin où se trouve le EXE
            exePath = Assembly.GetExecutingAssembly().Location;
            exeDir = Path.GetDirectoryName(exePath);

            // Vérifie si l'exe est dans un dossier "ianseo" et que "xampp-control.exe" est présent
            if (exeDir != null &&
                string.Equals(Path.GetFileName(exeDir), "ianseo", StringComparison.OrdinalIgnoreCase) &&
                File.Exists(Path.Combine(exeDir, "xampp-control.exe")))
            {
                basePath = exeDir; // utilise ce dossier comme base
                // Construit les chemins relatifs
                xamppStartPath = Path.Combine(basePath, "xampp_start.exe");
                xamppStopPath = Path.Combine(basePath, "xampp_stop.exe");
                httpdConfPath = Path.Combine(basePath, "apache", "conf", "httpd.conf");
                xamppControlPath = Path.Combine(basePath, "xampp-control.exe");
            }
            // si xampp-control n'est pas trouvé et que le dossier de base n'est pas ianseo, on essaie de voir si C:\ianseo existe
            else if (Directory.Exists(@"C:\ianseo") && File.Exists(Path.Combine(@"C:\ianseo", "xampp-control.exe")))
            {
                // Si le dossier C:\ianseo existe et contient xampp-control.exe, on l'utilise
                basePath = @"C:\ianseo";
                // Construit les chemins relatifs
                xamppStartPath = Path.Combine(basePath, "xampp_start.exe");
                xamppStopPath = Path.Combine(basePath, "xampp_stop.exe");
                httpdConfPath = Path.Combine(basePath, "apache", "conf", "httpd.conf");
                xamppControlPath = Path.Combine(basePath, "xampp-control.exe");
            }
            // sinon, on affiche une fenetre avec un message demandant de déplacer ianseo.exe dans le dossier d'installation de ianseo
            else
            {
                Form errorForm = new Form()
                {
                    Text = rm.GetString("InvalidDirectory_Title", culture),
                    Size = new Size(400, 350),
                    StartPosition = FormStartPosition.CenterScreen,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                // Icône d'erreur
                PictureBox iconBox = new PictureBox()
                {
                    Image = SystemIcons.Error.ToBitmap(),
                    Size = new Size(32, 32),
                    Location = new Point(175, 30)
                };

                Label lbl = new Label()
                {
                    Text = rm.GetString("Msg_InvalidDirectory", culture),
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };

                Button btnClose = new Button()
                {
                    Text = rm.GetString("BtnClose", culture), // ici le texte du bouton
                    Dock = DockStyle.Bottom,
                    Height = 40
                };
                btnClose.BackColor = Color.FromArgb(0, 68, 136);
                btnClose.ForeColor = Color.White;
                btnClose.Click += (s, e) =>
                {
                    errorForm.Close();
                    Application.Exit(); // ferme vraiment l’appli
                };

                errorForm.Controls.Add(iconBox);
                errorForm.Controls.Add(lbl);
                errorForm.Controls.Add(btnClose);
                errorForm.ShowDialog();
            }
            
            // Optionnel : forcer la langue française
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr");
            InitializeComponent();
            InitUI();
            InitTimer();
            // Vérification immédiate de l'état au démarrage
            CheckInitialServiceStatus();
            UpdateUIByServiceStatus();

            
        }
        private void CheckInitialServiceStatus()
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
        private void InitUI()
        {
            this.Text = "IANSEO";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.Sizable;
            //this.MaximizeBox = true;
            this.Padding = new Padding(0);
            this.Margin = new Padding(0);
            this.ClientSize = new Size(300, 365);
            this.MinimumSize = new Size(316, 404);
            this.Icon = Properties.Resources.ianseo;



            // Panel principal vertical

            mainPanel.BackColor = Color.FromArgb(240, 248, 255);
            mainPanel.FlowDirection = FlowDirection.TopDown;
            mainPanel.Size = new Size(300, 365);
            mainPanel.Padding = new Padding(0);
            mainPanel.Margin = new Padding(0);
            mainPanel.Anchor = AnchorStyles.None;

            mainPanel.Controls.Clear();




            ControlBloc.Anchor = AnchorStyles.None;
            ControlBloc.FlowDirection = FlowDirection.TopDown;
            ControlBloc.Size = new Size(300, 155);
            ControlBloc.Padding = new Padding(0);
            ControlBloc.Margin = new Padding(0, 0, 0, 0);
            ControlBloc.Anchor = AnchorStyles.Top;
            ControlBloc.WrapContents = false;

            // Bouton Start/Open
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
            ControlBloc.Controls.Add(btnStartOpen);

            // Bouton Stop
                btnStop = new Button();
                btnStop.Text = rm.GetString("BtnStop", culture); ;
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
            ControlBloc.Controls.Add(btnStop);

            // ProgressBar
                progressBar = new AnimatedProgressBar();
                progressBar.Size = new Size(300, 20);
                progressBar.Visible = false;
                progressBar.Padding = new Padding(0);
                progressBar.Margin = new Padding(0,10,0,0);
                progressBar.Anchor = AnchorStyles.Bottom;
            ControlBloc.Controls.Add(progressBar);


            
            ServerBloc.Anchor = AnchorStyles.None;
            ServerBloc.FlowDirection = FlowDirection.TopDown;
            ServerBloc.Size = new Size(300, 105);
            ServerBloc.Padding = new Padding(0);
            ServerBloc.Margin = new Padding(0, 0, 0, 0);
            ServerBloc.Anchor = AnchorStyles.Top;
            ServerBloc.WrapContents = false;

            // Label adresse Ianseo
                lblAddress = new Label();
                lblAddress.Text = rm.GetString("LblAddress", culture);
                lblAddress.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lblAddress.Size = new Size(300, 20);
                lblAddress.Padding = new Padding(0);
                lblAddress.Margin = new Padding(0,10,0,0);
                lblAddress.Anchor = AnchorStyles.Top;
                lblAddress.BackColor = Color.FromArgb(0, 68, 136);
                lblAddress.ForeColor = Color.White;
                lblAddress.TextAlign = ContentAlignment.MiddleCenter;
                ServerBloc.Controls.Add(lblAddress);

            // Adresse du serveur
                lblIanseoAddress = new Label();
                lblIanseoAddress.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                lblIanseoAddress.Size = new Size(300, 20);
                lblIanseoAddress.Padding = new Padding(0);
                lblIanseoAddress.Margin = new Padding(0,10,0,0);
                lblIanseoAddress.Anchor = AnchorStyles.Top;
                lblIanseoAddress.TextAlign = ContentAlignment.MiddleCenter;
                ServerBloc.Controls.Add(lblIanseoAddress);



            // Bloc pour les liens
            LinkPanel.Size = new Size(300, 25);
            LinkPanel.Padding = new Padding(0);
            LinkPanel.Margin = new Padding(0);
            LinkPanel.Anchor = AnchorStyles.Top;

            // Boutons pour copier le lien et afficher le QR code
                btnCopyLink = new Button();
                btnCopyLink.Text = rm.GetString("BtnCopyLink", culture); ;
                btnCopyLink.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                btnCopyLink.Size = new Size(100, 25);
                btnCopyLink.FlatStyle = FlatStyle.Flat;
                btnCopyLink.FlatAppearance.BorderSize = 1;
                btnCopyLink.Padding = new Padding(0);
                btnCopyLink.Margin = new Padding(40, 0, 20, 0);
                btnCopyLink.Anchor = AnchorStyles.Top;
                btnCopyLink.Click += BtnCopyLink_Click;

                btnShowQR = new Button();
            btnShowQR.Text = rm.GetString("BtnShowQR", culture);
                btnShowQR.Font = new Font("Segoe UI", 9, FontStyle.Regular);
                btnShowQR.Size = new Size(100, 25);
                btnShowQR.FlatStyle = FlatStyle.Flat;
                btnShowQR.FlatAppearance.BorderSize = 1;
                btnShowQR.Padding = new Padding(0);
                btnShowQR.Margin = new Padding(0);
                btnShowQR.Anchor = AnchorStyles.Top;
                btnShowQR.Click += BtnShowQR_Click;

                LinkPanel.Controls.Add(btnCopyLink);
                LinkPanel.Controls.Add(btnShowQR);
            ServerBloc.Controls.Add(LinkPanel);


            
            
            StatusBloc.Anchor = AnchorStyles.None;
            StatusBloc.FlowDirection = FlowDirection.TopDown;
            StatusBloc.Size = new Size(300, 105);
            StatusBloc.Padding = new Padding(0);
            StatusBloc.Margin = new Padding(0, 0, 0, 0);
            StatusBloc.Anchor = AnchorStyles.Top;
            StatusBloc.WrapContents = false;


            // Bloc état services
                lblEtat = new Label();
                lblEtat.Text = rm.GetString("LblEtat", culture);
                lblEtat.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lblEtat.Size = new Size(300, 20);
                lblEtat.Padding = new Padding(0);
                lblEtat.Margin = new Padding(0,10,0,0);
                lblEtat.Anchor = AnchorStyles.Top;
                lblEtat.BackColor = Color.FromArgb(0, 68, 136);
                lblEtat.ForeColor = Color.White;
                lblEtat.Anchor = AnchorStyles.None;
                lblEtat.TextAlign = ContentAlignment.MiddleCenter;
            StatusBloc.Controls.Add(lblEtat);


            //ajouter un lien vers Xampp_control
            etatPanel.FlowDirection = FlowDirection.LeftToRight;
            etatPanel.Size = new Size(300, 55);
            etatPanel.Padding = new Padding(0);
            etatPanel.Margin = new Padding(0);
            etatPanel.Anchor = AnchorStyles.Top;

            
            servicesPanel.FlowDirection = FlowDirection.TopDown;
            servicesPanel.Size = new Size(140, 55);
            servicesPanel.Padding = new Padding(0);
            servicesPanel.Margin = new Padding(0);
            servicesPanel.Anchor = AnchorStyles.Top;

            // Apache
            apachePanel.FlowDirection = FlowDirection.LeftToRight;
                apachePanel.Size = new Size(100, 20);
                apachePanel.Padding = new Padding(0);
                apachePanel.Margin = new Padding(40, 10, 0, 0);
                apachePanel.Anchor = AnchorStyles.Left;

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
            // Forme ronde
                dotApache.Region = new Region(new Rectangle(0, 0, dotApache.Width, dotApache.Height));
                lblApache = new Label();
                lblApache.Text = rm.GetString("LblApache", culture);
                lblApache.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                lblApache.TextAlign = ContentAlignment.MiddleLeft;
                lblApache.AutoSize = true;
                apachePanel.Controls.Add(dotApache);
                apachePanel.Controls.Add(lblApache);
            servicesPanel.Controls.Add(apachePanel);

            // MySQL
                mysqlPanel.FlowDirection = FlowDirection.LeftToRight;

                mysqlPanel.Size = new Size(100, 20);
                mysqlPanel.Padding = new Padding(0);
                mysqlPanel.Margin = new Padding(40, 5, 0, 0);
                mysqlPanel.Anchor = AnchorStyles.Left;


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
            // Forme ronde
                dotMySQL.Region = new Region(new Rectangle(0, 0, dotMySQL.Width, dotMySQL.Height));
                lblMySQL = new Label();
                lblMySQL.Text = rm.GetString("LblMySQL", culture);
                lblMySQL.Font = new Font("Segoe UI", 10, FontStyle.Regular);
                lblMySQL.TextAlign = ContentAlignment.MiddleLeft;
                lblMySQL.AutoSize = true;
                mysqlPanel.Controls.Add(dotMySQL);
                mysqlPanel.Controls.Add(lblMySQL);
            servicesPanel.Controls.Add(mysqlPanel);


            etatPanel.Controls.Add(servicesPanel);

            //Lien vers Xampp-control
            btnOpenXamppControl = new Button();
            btnOpenXamppControl.Text = rm.GetString("BtnOpenXamppControl", culture);
            btnOpenXamppControl.Font = new Font("Segoe UI", 7, FontStyle.Regular);
            btnOpenXamppControl.BackColor = Color.FromArgb(251, 122, 36);
            btnOpenXamppControl.ForeColor = Color.White;
            btnOpenXamppControl.Size = new Size(80, 35);
            btnOpenXamppControl.FlatStyle = FlatStyle.Flat;
            btnOpenXamppControl.FlatAppearance.BorderSize = 0;
            btnOpenXamppControl.Padding = new Padding(0);
            btnOpenXamppControl.Margin = new Padding(75, 20, 0, 0);
            btnOpenXamppControl.Anchor = AnchorStyles.Top;
            btnOpenXamppControl.Click += BtnOpenXamppControl_Click;
            etatPanel.Controls.Add(btnOpenXamppControl);


            StatusBloc.Controls.Add(etatPanel);


            mainPanel.Controls.Add(ControlBloc);
            mainPanel.Controls.Add(ServerBloc);
            mainPanel.Controls.Add(StatusBloc);


            this.Controls.Add(mainPanel);
            this.BackColor = Color.Black;


            UpdateUIByServiceStatus();
        }

        private void InitTimer()
        {
            checkTimer = new System.Windows.Forms.Timer();
            checkTimer.Interval = 1000;
            checkTimer.Tick += CheckServices;
            checkTimer.Start();
        }

        private void CheckServices(object sender, EventArgs e)
        {
            bool apacheRunning = Process.GetProcessesByName("httpd").Any();
            bool mysqlRunning = Process.GetProcessesByName("mysqld").Any();

            dotApache.BackColor = apacheRunning ? Color.Green : Color.Red;
            dotApache.Invalidate();
            dotMySQL.BackColor = mysqlRunning ? Color.Green : Color.Red;
            dotMySQL.Invalidate();

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
            UpdateIanseoAddress();
        }

        // Met à jour l'adresse affichée à chaque fois que tu changes l'UI (par ex. dans UpdateUIByServiceStatus)
        private void UpdateIanseoAddress()
        {
            string ip = GetLocalIPAddress();
            int port = ReadApachePort();
            lblIanseoAddress.Text = $"{ip}:{port}";
        }

        private void UpdateUIByServiceStatus()
        {
            
            if (servicesRunning)
            {
                btnStartOpen.Text = rm.GetString("BtnStartOpen_Open", culture);
                btnStartOpen.BackColor = Color.FromArgb(0, 68, 136);
                btnStop.Visible = true;
                btnStop.Enabled = true;
                btnStop.Text = rm.GetString("BtnStop", culture);
                lblAddress.Visible = true;
                lblAddress.Enabled = true;
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
            else
            {
                btnStartOpen.Text = rm.GetString("BtnStartOpen_Run", culture);
                btnStartOpen.BackColor = Color.FromArgb(0, 68, 136);
                btnStop.Visible = false;
                btnStop.Enabled = false;
                btnStop.Text = rm.GetString("BtnStop", culture);
                lblAddress.Visible = false;
                lblAddress.Enabled = false;
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

        private async void BtnStartOpen_Click(object sender, EventArgs e)
        {
            if (!servicesRunning)
            {
                desiredState = ServiceState.Running;
                btnStartOpen.Text = rm.GetString("BtnStartOpen_Starting", culture);
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
                    MessageBox.Show(rm.GetString("Msg_Error_OpenBrowser", culture) + ex.Message);
                }
            }
        }

        private async void BtnStop_Click(object sender, EventArgs e)
        {
            desiredState = ServiceState.Stopped;
            btnStop.Text = rm.GetString("BtnStop_Stopping", culture);
            btnStop.Enabled = false;
            btnStartOpen.Enabled = false;
            btnStartOpen.Visible = false;
            btnStop.Margin = new Padding(0, 80, 0, 0);
            progressBar.Margin = new Padding(0, 15, 0, 0);
            lblAddress.Visible = false;
            lblAddress.Enabled = false;
            lblIanseoAddress.Visible = false;
            lblIanseoAddress.Enabled = false;
            btnCopyLink.Visible = false;
            btnCopyLink.Enabled = false;
            btnShowQR.Visible = false;
            btnShowQR.Enabled = false;
            LinkPanel.Visible = false;
            await RunProcessAndWait(@xamppStopPath);
        }

        private async Task RunProcessAndWait(string filePath)
        {
            try
            {
                // Démarrer la progress bar
                StartProgressBar();

                Process process = new Process();
                process.StartInfo.FileName = filePath;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                await Task.Run(() => process.WaitForExit());

                // Finir la progress bar rapidement à 100%
                await FinishProgressBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(rm.GetString("Msg_Error_Execution", culture) + ex.Message);
            }
        }
        private void OpenBrowser()
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
                MessageBox.Show(rm.GetString("Msg_Error_OpenBrowserAlt", culture) + ex.Message);
            }
        }

        private string GetLocalIPAddress()
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

        private int ReadApachePort()
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
        private void StartProgressBar()
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

        private void StopProgressBar()
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
        public void StopProgressBarSafe()
        {
            if (InvokeRequired)
            {
                Invoke((Action)(() => StopProgressBarSafe()));
                return;
            }
            StopProgressBar();
        }


        private async Task FinishProgressBar()
        {
            if (!isProgressRunning) return;

            // Monter rapidement à 100% en 0.5s
            float current = progressBar.Progress;
            float target = 100f;
            int steps = 10;
            int delay = 50;

            for (int i = 1; i <= steps; i++)
            {
                progressBar.Progress = current + (target - current) * i / steps;
                await Task.Delay(delay);
            }
        }
        private Random rnd = new Random();
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            if (!isProgressRunning) return;

            double elapsedSeconds = (DateTime.Now - progressStartTime).TotalSeconds;
            double totalDuration = 10.0; // 10 secondes pour aller jusqu'à 90%

            // Progression cible selon temps (courbe de ralentissement)
            double targetProgress;

            if (elapsedSeconds >= totalDuration)
            {
                targetProgress = 90;
            }
            else
            {
                // Courbe exponentielle lente au départ et plus rapide ensuite (ou l'inverse)
                // Ici on fait une progression plus rapide au début puis ralentit (logarithmique)
                targetProgress = 90 * (1 - Math.Exp(-elapsedSeconds * 0.5)); // ajustable
                if (targetProgress > 90) targetProgress = 90;
            }

            // On avance la barre par petits pas aléatoires vers targetProgress
            float current = progressBar.Progress;
            if (current < targetProgress)
            {
                // Avance aléatoire entre 0.1 et 5.0 %
                float step = (float)(rnd.NextDouble() * 4.9 + 0.1);

                float newProgress = current + step;

                if (newProgress > targetProgress) newProgress = (float)targetProgress;

                progressBar.Progress = newProgress;
            }
        }

        // Copier dans le presse-papier
        private void BtnCopyLink_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText($"{GetLocalIPAddress()}:{ReadApachePort()}");
                MessageBox.Show(rm.GetString("Msg_LinkCopied", culture), rm.GetString("Copied_Title", culture), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(rm.GetString("Msg_Error_Copy", culture) + ex.Message);
            }
        }

        // Afficher QR Code dans une popup
        private void BtnShowQR_Click(object sender, EventArgs e)
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
                    qrForm.Text = rm.GetString("QrForm_Title", culture);
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
                MessageBox.Show(rm.GetString("Msg_Error_QrGeneration", culture) + ex.Message);
            }
        }
        private void BtnOpenXamppControl_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(@xamppControlPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(rm.GetString("Msg_Error_OpenXamppControl", culture) + ex.Message);
            }
        }

    }

    public class AnimatedProgressBar : Control
    {
        private System.Windows.Forms.Timer animationTimer;
        private float progress = 0f;       // 0-100 %
        private float animationOffset = 0; // animation bande claire


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

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            // Décalage de l'effet bande claire (défilement)
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

            // Calcul largeur barre remplie
            int fillWidth = (int)(rect.Width * (progress / 100f));
            if (fillWidth <= 0) return;

            var fillRect = new Rectangle(rect.X, rect.Y, fillWidth, rect.Height);

            // Remplissage barre principale
            using (var brush = new SolidBrush(Color.FromArgb(0, 68, 136)))
            {
                g.FillRectangle(brush, fillRect);
            }

            // Dessiner la bande claire animée
            using (var path = new GraphicsPath())
            {
                int bandWidth = 60;
                int bandHeight = rect.Height;

                // Création bande décalée
                var bandRect = new Rectangle((int)(animationOffset - bandWidth), 0, bandWidth, bandHeight);
                path.AddRectangle(bandRect);

                // Clip à la zone remplie (pas dépasser la barre remplie)
                g.SetClip(fillRect);

                // Bande claire semi-transparente (blanc avec alpha)
                using (var brush = new LinearGradientBrush(bandRect, Color.FromArgb(120, 255, 255, 255), Color.FromArgb(0, 255, 255, 255), LinearGradientMode.Horizontal))
                {
                    g.FillPath(brush, path);
                }

                g.ResetClip();
            }

            // Bordure arrondie (optionnel)
            using (var pen = new Pen(Color.DarkGray))
            {
                g.DrawRectangle(pen, 0, 0, rect.Width - 1, rect.Height - 1);
            }
        }
    }

}
