using System;
using System.Windows.Forms;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace RDPSecurityViewer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private TabControl tabs;
        private DataGridView gridLogs;
        private DataGridView gridBlocked;
        private DataGridView gridWhitelist;
        private DataGridView gridCurrentLog;
        private TextBox txtWhitelistIP;
        private TextBox txtBlockedIP;
        private TextBox txtCurrentPort;
        private Button btnAddWhitelist;
        private Button btnAddBlocked;
        private Button btnDeleteBlocked;
        private Button btnDeleteWhitelist;
        private Button btnRefreshCurrent;
        private Button btnClearCurrent;
        private Button btnRefresh;
        private Label lblStatus;
        private Timer refreshTimer;
        private Timer currentLogTimer;
        private TabPage tabCurrentLog;
        private Dictionary<string, string> currentConnState = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> countedAttemptEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, int> authAttemptCounters = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> currentWhitelistIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> currentBlockedIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private const int CURRENT_BAN_THRESHOLD = 5;
        private string currentLogFilePath;
        private string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RDPSecurityService");
        private string whitelistPath;
        private string blockLogPath;

        public MainForm()
        {
            this.Text = "RDP Security Service Logs Viewer";
            this.Width = 1000;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterScreen;

            whitelistPath = Path.Combine(logDir, "whiteList.log");
            blockLogPath = Path.Combine(logDir, "block_list.log");
            currentLogFilePath = Path.Combine(logDir, "current_log.log");
            if (!File.Exists(whitelistPath))
                File.Create(whitelistPath).Close();

            // TOOLBAR PANEL
            Panel toolbar = new Panel();
            toolbar.Height = 50;
            toolbar.Dock = DockStyle.Top;
            toolbar.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            btnRefresh = new Button();
            btnRefresh.Text = "Refresh";
            btnRefresh.Width = 100;
            btnRefresh.Height = 35;
            btnRefresh.Location = new System.Drawing.Point(10, 7);
            btnRefresh.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnRefresh.ForeColor = System.Drawing.Color.White;
            btnRefresh.Click += (s, e) => LoadLogs();
            toolbar.Controls.Add(btnRefresh);

            lblStatus = new Label();
            lblStatus.Text = "Ready";
            lblStatus.Location = new System.Drawing.Point(120, 15);
            lblStatus.ForeColor = System.Drawing.Color.White;
            lblStatus.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            lblStatus.AutoSize = true;
            toolbar.Controls.Add(lblStatus);

            // TAB CONTROL
            tabs = new TabControl();
            tabs.Dock = DockStyle.Fill;
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.ItemSize = new System.Drawing.Size(100, 30);
            tabs.DrawItem += (s, e) =>
            {
                var brush = e.Index == tabs.SelectedIndex 
                    ? new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(0, 120, 215))
                    : new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(45, 45, 48));
                
                var textBrush = e.Index == tabs.SelectedIndex
                    ? System.Drawing.Brushes.White
                    : System.Drawing.Brushes.LightGray;
                
                e.Graphics.FillRectangle(brush, e.Bounds);
                
                var font = e.Index == tabs.SelectedIndex
                    ? new System.Drawing.Font(tabs.Font, System.Drawing.FontStyle.Bold)
                    : tabs.Font;
                
                var stringFormat = new System.Drawing.StringFormat();
                stringFormat.Alignment = System.Drawing.StringAlignment.Center;
                stringFormat.LineAlignment = System.Drawing.StringAlignment.Center;
                
                e.Graphics.DrawString(tabs.TabPages[e.Index].Text, font, textBrush, e.Bounds, stringFormat);
                e.DrawFocusRectangle();
            };

            // TAB 1 - FAILED ATTEMPTS
            TabPage tab1 = new TabPage("Failed Attempts");
            gridLogs = new DataGridView();
            gridLogs.Dock = DockStyle.Fill;
            gridLogs.AutoGenerateColumns = false;
            gridLogs.ReadOnly = true;
            gridLogs.AllowUserToAddRows = false;
            gridLogs.BackgroundColor = System.Drawing.Color.White;
            gridLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Time", Width = 200 });
            gridLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "IP", HeaderText = "Source IP", Width = 200 });
            gridLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Attempts", HeaderText = "Attempts", Width = 200 });
            tab1.Controls.Add(gridLogs);
            tabs.TabPages.Add(tab1);

            // TAB 2 - BLOCKED IPS
            TabPage tab2 = new TabPage("Blocked IPs");

            Panel panelBlockedInput = new Panel();
            panelBlockedInput.Dock = DockStyle.Top;
            panelBlockedInput.Height = 80;
            panelBlockedInput.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            Label lblBlockedIP = new Label();
            lblBlockedIP.Text = "IP Address:";
            lblBlockedIP.ForeColor = System.Drawing.Color.White;
            lblBlockedIP.Location = new System.Drawing.Point(10, 20);
            lblBlockedIP.AutoSize = true;
            panelBlockedInput.Controls.Add(lblBlockedIP);

            txtBlockedIP = new TextBox();
            txtBlockedIP.Location = new System.Drawing.Point(100, 18);
            txtBlockedIP.Width = 200;
            txtBlockedIP.Height = 22;
            panelBlockedInput.Controls.Add(txtBlockedIP);

            btnAddBlocked = new Button();
            btnAddBlocked.Text = "Add";
            btnAddBlocked.Location = new System.Drawing.Point(310, 18);
            btnAddBlocked.Width = 90;
            btnAddBlocked.Height = 26;
            btnAddBlocked.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnAddBlocked.ForeColor = System.Drawing.Color.White;
            btnAddBlocked.Click += (s, e) => AddToBlocked();
            panelBlockedInput.Controls.Add(btnAddBlocked);

            gridBlocked = new DataGridView();
            gridBlocked.Dock = DockStyle.Fill;
            gridBlocked.AutoGenerateColumns = false;
            gridBlocked.AllowUserToAddRows = false;
            gridBlocked.BackgroundColor = System.Drawing.Color.White;
            gridBlocked.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Select", HeaderText = "Delete", Width = 50 });
            gridBlocked.Columns.Add(new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Time", Width = 130, ReadOnly = true });
            gridBlocked.Columns.Add(new DataGridViewTextBoxColumn { Name = "IP", HeaderText = "Blocked IP", Width = 130, ReadOnly = true });
            gridBlocked.Columns.Add(new DataGridViewTextBoxColumn { Name = "Attempts", HeaderText = "Attempts", Width = 80, ReadOnly = true });
            gridBlocked.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 80, ReadOnly = true });
            gridBlocked.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Conflict", HeaderText = "КОНФЛИКТ", Width = 90, ReadOnly = true });

            Panel panelBlockedButtons = new Panel();
            panelBlockedButtons.Height = 50;
            panelBlockedButtons.Dock = DockStyle.Bottom;
            panelBlockedButtons.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            btnDeleteBlocked = new Button();
            btnDeleteBlocked.Text = "Delete Selected";
            btnDeleteBlocked.Location = new System.Drawing.Point(10, 10);
            btnDeleteBlocked.Width = 120;
            btnDeleteBlocked.Height = 30;
            btnDeleteBlocked.BackColor = System.Drawing.Color.FromArgb(200, 50, 50);
            btnDeleteBlocked.ForeColor = System.Drawing.Color.White;
            btnDeleteBlocked.Click += (s, e) => DeleteSelectedBlocked();
            panelBlockedButtons.Controls.Add(btnDeleteBlocked);

            Button btnRefreshBlocked = new Button();
            btnRefreshBlocked.Text = "Перечитать";
            btnRefreshBlocked.Location = new System.Drawing.Point(140, 10);
            btnRefreshBlocked.Width = 120;
            btnRefreshBlocked.Height = 30;
            btnRefreshBlocked.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnRefreshBlocked.ForeColor = System.Drawing.Color.White;
            btnRefreshBlocked.Click += (s, e) => LoadBlockedIps();
            panelBlockedButtons.Controls.Add(btnRefreshBlocked);

            tab2.Controls.Add(panelBlockedButtons);
            tab2.Controls.Add(gridBlocked);
            tab2.Controls.Add(panelBlockedInput);
            tabs.TabPages.Add(tab2);

            // TAB 3 - WHITELIST
            TabPage tab3 = new TabPage("WhiteList");

            Panel panelInputWL = new Panel();
            panelInputWL.Dock = DockStyle.Top;
            panelInputWL.Height = 80;
            panelInputWL.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            Label lblIP = new Label();
            lblIP.Text = "IP Address:";
            lblIP.ForeColor = System.Drawing.Color.White;
            lblIP.Location = new System.Drawing.Point(10, 20);
            lblIP.AutoSize = true;
            panelInputWL.Controls.Add(lblIP);

            txtWhitelistIP = new TextBox();
            txtWhitelistIP.Location = new System.Drawing.Point(100, 18);
            txtWhitelistIP.Width = 200;
            txtWhitelistIP.Height = 22;
            panelInputWL.Controls.Add(txtWhitelistIP);

            btnAddWhitelist = new Button();
            btnAddWhitelist.Text = "Add";
            btnAddWhitelist.Location = new System.Drawing.Point(310, 18);
            btnAddWhitelist.Width = 90;
            btnAddWhitelist.Height = 26;
            btnAddWhitelist.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnAddWhitelist.ForeColor = System.Drawing.Color.White;
            btnAddWhitelist.Click += (s, e) => AddToWhitelist();
            panelInputWL.Controls.Add(btnAddWhitelist);

            gridWhitelist = new DataGridView();
            gridWhitelist.Dock = DockStyle.Fill;
            gridWhitelist.AutoGenerateColumns = false;
            gridWhitelist.AllowUserToAddRows = false;
            gridWhitelist.BackgroundColor = System.Drawing.Color.White;
            gridWhitelist.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Select", HeaderText = "Delete", Width = 50 });
            gridWhitelist.Columns.Add(new DataGridViewTextBoxColumn { Name = "IP", HeaderText = "Whitelisted IP", Width = 200, ReadOnly = true });
            gridWhitelist.Columns.Add(new DataGridViewTextBoxColumn { Name = "Date", HeaderText = "Added", Width = 250, ReadOnly = true });
            gridWhitelist.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Conflict", HeaderText = "КОНФЛИКТ", Width = 90, ReadOnly = true });

            Panel panelWhitelistButtons = new Panel();
            panelWhitelistButtons.Height = 50;
            panelWhitelistButtons.Dock = DockStyle.Bottom;
            panelWhitelistButtons.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            btnDeleteWhitelist = new Button();
            btnDeleteWhitelist.Text = "Delete Selected";
            btnDeleteWhitelist.Location = new System.Drawing.Point(10, 10);
            btnDeleteWhitelist.Width = 120;
            btnDeleteWhitelist.Height = 30;
            btnDeleteWhitelist.BackColor = System.Drawing.Color.FromArgb(200, 50, 50);
            btnDeleteWhitelist.ForeColor = System.Drawing.Color.White;
            btnDeleteWhitelist.Click += (s, e) => DeleteSelectedWhitelist();
            panelWhitelistButtons.Controls.Add(btnDeleteWhitelist);

            tab3.Controls.Add(panelWhitelistButtons);
            tab3.Controls.Add(gridWhitelist);
            tab3.Controls.Add(panelInputWL);
            tabs.TabPages.Add(tab3);

            // TAB 4 - CURRENT LOG
            tabCurrentLog = new TabPage("Current Log");

            Panel panelCurrentTop = new Panel();
            panelCurrentTop.Dock = DockStyle.Top;
            panelCurrentTop.Height = 50;
            panelCurrentTop.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);

            Label lblPort = new Label();
            lblPort.Text = "Port:";
            lblPort.ForeColor = System.Drawing.Color.White;
            lblPort.Location = new System.Drawing.Point(10, 15);
            lblPort.AutoSize = true;
            panelCurrentTop.Controls.Add(lblPort);

            txtCurrentPort = new TextBox();
            txtCurrentPort.Location = new System.Drawing.Point(50, 12);
            txtCurrentPort.Width = 80;
            txtCurrentPort.Text = "3389";
            panelCurrentTop.Controls.Add(txtCurrentPort);

            btnRefreshCurrent = new Button();
            btnRefreshCurrent.Text = "Refresh Current";
            btnRefreshCurrent.Location = new System.Drawing.Point(140, 10);
            btnRefreshCurrent.Width = 120;
            btnRefreshCurrent.Height = 28;
            btnRefreshCurrent.BackColor = System.Drawing.Color.FromArgb(0, 120, 215);
            btnRefreshCurrent.ForeColor = System.Drawing.Color.White;
            btnRefreshCurrent.Click += (s, e) => LoadCurrentConnections();
            panelCurrentTop.Controls.Add(btnRefreshCurrent);

            btnClearCurrent = new Button();
            btnClearCurrent.Text = "Clear";
            btnClearCurrent.Location = new System.Drawing.Point(270, 10);
            btnClearCurrent.Width = 80;
            btnClearCurrent.Height = 28;
            btnClearCurrent.BackColor = System.Drawing.Color.FromArgb(200, 50, 50);
            btnClearCurrent.ForeColor = System.Drawing.Color.White;
            btnClearCurrent.Click += (s, e) => ClearCurrentLog();
            panelCurrentTop.Controls.Add(btnClearCurrent);

            gridCurrentLog = new DataGridView();
            gridCurrentLog.Dock = DockStyle.Fill;
            gridCurrentLog.AutoGenerateColumns = false;
            gridCurrentLog.ReadOnly = true;
            gridCurrentLog.AllowUserToAddRows = false;
            gridCurrentLog.BackgroundColor = System.Drawing.Color.White;
            gridCurrentLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "SeenAt", HeaderText = "Seen At", Width = 160 });
            gridCurrentLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Local", HeaderText = "Local Endpoint", Width = 220 });
            gridCurrentLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Remote", HeaderText = "Remote Endpoint", Width = 220 });
            gridCurrentLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "Attempts", HeaderText = "Attempts", Width = 90 });
            gridCurrentLog.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Banned", HeaderText = "Banned", Width = 80, ReadOnly = true });
            gridCurrentLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "DlWl", HeaderText = "Dl\\Wl", Width = 80 });
            gridCurrentLog.Columns.Add(new DataGridViewTextBoxColumn { Name = "State", HeaderText = "State", Width = 140 });

            tabCurrentLog.Controls.Add(gridCurrentLog);
            tabCurrentLog.Controls.Add(panelCurrentTop);
            tabs.TabPages.Add(tabCurrentLog);
            tabs.SelectedIndexChanged += (s, e) =>
            {
                tabs.Invalidate();
                if (tabs.SelectedTab == tabCurrentLog)
                    LoadCurrentConnections();
            };

            // ADD TO FORM
            this.Controls.Add(tabs);
            this.Controls.Add(toolbar);

            refreshTimer = new Timer();
            refreshTimer.Interval = 3000;
            refreshTimer.Tick += (s, e) => LoadLogs();
            refreshTimer.Start();

            currentLogTimer = new Timer();
            currentLogTimer.Interval = 2000;
            currentLogTimer.Tick += (s, e) =>
            {
                if (tabs.SelectedTab == tabCurrentLog)
                    LoadCurrentConnections();
            };
            currentLogTimer.Start();

            LoadLogs();
            LoadBlockedIps();
        }

        private void DeleteSelectedBlocked()
        {
            try
            {
                List<string> rowsToDelete = new List<string>();

                foreach (DataGridViewRow row in gridBlocked.Rows)
                {
                    if (row.Cells["Select"].Value != null && (bool)row.Cells["Select"].Value)
                    {
                        rowsToDelete.Add(row.Cells["IP"].Value.ToString());
                    }
                }

                if (rowsToDelete.Count == 0)
                {
                    MessageBox.Show("Select items to delete", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (File.Exists(blockLogPath))
                {
                    string[] lines = File.ReadAllLines(blockLogPath);
                    List<string> newLines = new List<string>();

                    foreach (string line in lines)
                    {
                        bool shouldDelete = false;
                        foreach (string ip in rowsToDelete)
                        {
                            if (line.Contains(ip))
                            {
                                shouldDelete = true;
                                break;
                            }
                        }

                        if (!shouldDelete)
                            newLines.Add(line);
                    }

                    File.WriteAllLines(blockLogPath, newLines);
                    LoadBlockedIps();
                    MessageBox.Show("Records deleted", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("No write access to block_list.log. Run viewer as Administrator.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("block_list.log is busy or locked.\n" + ex.Message, "I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DeleteSelectedWhitelist()
        {
            try
            {
                List<string> rowsToDelete = new List<string>();

                foreach (DataGridViewRow row in gridWhitelist.Rows)
                {
                    if (row.Cells["Select"].Value != null && (bool)row.Cells["Select"].Value)
                    {
                        rowsToDelete.Add(row.Cells["IP"].Value.ToString());
                    }
                }

                if (rowsToDelete.Count == 0)
                {
                    MessageBox.Show("Select items to delete", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (File.Exists(whitelistPath))
                {
                    string[] lines = File.ReadAllLines(whitelistPath);
                    List<string> newLines = new List<string>();

                    foreach (string line in lines)
                    {
                        bool shouldDelete = false;
                        foreach (string ip in rowsToDelete)
                        {
                            if (line.Contains(ip))
                            {
                                shouldDelete = true;
                                break;
                            }
                        }

                        if (!shouldDelete)
                            newLines.Add(line);
                    }

                    File.WriteAllLines(whitelistPath, newLines);
                    LoadLogs();
                    LoadBlockedIps();
                    MessageBox.Show("Records deleted", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("No write access to whiteList.log. Run viewer as Administrator.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("whiteList.log is busy or locked.\n" + ex.Message, "I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddToBlocked()
        {
            try
            {
                string ip = txtBlockedIP.Text.Trim();

                if (string.IsNullOrWhiteSpace(ip))
                {
                    MessageBox.Show("Enter IP address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!IsValidIP(ip))
                {
                    MessageBox.Show("Invalid IP format", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string[] lines = File.Exists(blockLogPath) ? File.ReadAllLines(blockLogPath) : new string[0];

                foreach (string line in lines)
                {
                    if (line.Contains(ip))
                    {
                        MessageBox.Show("This IP already in blocklist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                string entry = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | BLOCKED IP: " + ip + " | Failed Attempts: 1";
                File.AppendAllText(blockLogPath, entry + Environment.NewLine);

                MessageBox.Show("IP added to blocklist", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtBlockedIP.Clear();
                LoadBlockedIps();
                gridBlocked.Refresh();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("No write access to block_list.log. Run viewer as Administrator.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("block_list.log is busy or locked.\n" + ex.Message, "I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddToWhitelist()
        {
            try
            {
                string ip = txtWhitelistIP.Text.Trim();

                if (string.IsNullOrWhiteSpace(ip))
                {
                    MessageBox.Show("Enter IP address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!IsValidIP(ip))
                {
                    MessageBox.Show("Invalid IP format", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string[] lines = File.Exists(whitelistPath) ? File.ReadAllLines(whitelistPath) : new string[0];

                foreach (string line in lines)
                {
                    if (line.Contains(ip))
                    {
                        MessageBox.Show("This IP already in whitelist", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                string entry = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | IP: " + ip;
                File.AppendAllText(whitelistPath, entry + Environment.NewLine);
                string cleanupError;
                if (!RemoveIpFromBlocklistAndFirewall(ip, out cleanupError))
                {
                    MessageBox.Show("Added to whitelist, but cleanup of blocklist/firewall failed:\n" + cleanupError,
                        "Partial Success", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                MessageBox.Show("IP added to whitelist", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtWhitelistIP.Clear();
                LoadLogs();
                LoadBlockedIps();
                gridWhitelist.Refresh();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("No write access to whiteList.log. Run viewer as Administrator.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show("whiteList.log is busy or locked.\n" + ex.Message, "I/O Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsValidIP(string ip)
        {
            string[] parts = ip.Split('.');
            if (parts.Length != 4) return false;

            foreach (string part in parts)
            {
                if (!int.TryParse(part, out int num) || num < 0 || num > 255)
                    return false;
            }

            return true;
        }

        private void LoadLogs()
        {
            try
            {
                string accessLogPath = Path.Combine(logDir, "access.log");

                gridLogs.Rows.Clear();
                if (File.Exists(accessLogPath))
                {
                    var totalAttemptsByIp = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var lastCounterByIp = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                    var lastTimeByIp = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

                    foreach (string line in File.ReadAllLines(accessLogPath))
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("IP:")) continue;
                        try
                        {
                            var parts = line.Split(new string[] { "|" }, StringSplitOptions.None);
                            if (parts.Length >= 2)
                            {
                                string ip = line.Split(new string[] { "IP:" }, StringSplitOptions.None)[1].Split('|')[0].Trim();
                                int attempts = int.Parse(parts[1].Split(':')[1].Trim());
                                DateTime time = DateTime.Parse(line.Substring(1, 19));

                                if (!totalAttemptsByIp.ContainsKey(ip))
                                {
                                    totalAttemptsByIp[ip] = 0;
                                    lastCounterByIp[ip] = 0;
                                    lastTimeByIp[ip] = time;
                                }

                                int previous = lastCounterByIp[ip];
                                int delta = attempts >= previous ? (attempts - previous) : attempts;
                                if (delta < 0) delta = 0;

                                totalAttemptsByIp[ip] += delta;
                                lastCounterByIp[ip] = attempts;
                                lastTimeByIp[ip] = time;
                            }
                        }
                        catch { }
                    }

                    foreach (var ip in totalAttemptsByIp.Keys.OrderByDescending(k => lastTimeByIp[k]))
                    {
                        gridLogs.Rows.Add(lastTimeByIp[ip].ToString("yyyy-MM-dd HH:mm:ss"), ip, totalAttemptsByIp[ip].ToString());
                    }
                }

                var whitelistIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(whitelistPath))
                {
                    foreach (string line in File.ReadAllLines(whitelistPath))
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("IP:")) continue;
                        try
                        {
                            string ip = line.Split(new string[] { "IP:" }, StringSplitOptions.None)[1].Trim();
                            if (System.Net.IPAddress.TryParse(ip, out _)) whitelistIps.Add(ip);
                        }
                        catch { }
                    }
                }

                var blockedIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(blockLogPath))
                {
                    foreach (string line in File.ReadAllLines(blockLogPath))
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("BLOCKED IP:")) continue;
                        try
                        {
                            string ip = line.Split(new string[] { "BLOCKED IP:" }, StringSplitOptions.None)[1].Split('|')[0].Trim();
                            if (System.Net.IPAddress.TryParse(ip, out _)) blockedIps.Add(ip);
                        }
                        catch { }
                    }
                }

                gridWhitelist.Rows.Clear();
                if (File.Exists(whitelistPath))
                {
                    foreach (string line in File.ReadAllLines(whitelistPath))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        try
                        {
                            if (line.Length >= 19 && line.Contains("IP:"))
                            {
                                string date = line.Substring(0, 19);
                                int ipIndex = line.IndexOf("IP:");
                                if (ipIndex >= 0)
                                {
                                    string ip = line.Substring(ipIndex + 4).Trim();
                                    bool conflict = blockedIps.Contains(ip);
                                    gridWhitelist.Rows.Add(false, ip, date, conflict);
                                }
                            }
                        }
                        catch { }
                    }
                }

                lblStatus.Text = "Updated: " + DateTime.Now.ToString("HH:mm:ss");
                LoadCurrentConnections();
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error: " + ex.Message;
            }
        }

        private void LoadBlockedIps()
        {
            try
            {
                var whitelistIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                if (File.Exists(whitelistPath))
                {
                    foreach (string line in File.ReadAllLines(whitelistPath))
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("IP:")) continue;
                        try
                        {
                            string ip = line.Split(new string[] { "IP:" }, StringSplitOptions.None)[1].Trim();
                            if (System.Net.IPAddress.TryParse(ip, out _)) whitelistIps.Add(ip);
                        }
                        catch { }
                    }
                }

                // Dictionary to group by IP: IP -> (LatestTime, MaxAttempts, Status, Until)
                var groupedByIP = new Dictionary<string, (string Time, int MaxAttempts, string Status, DateTime Until)>(StringComparer.OrdinalIgnoreCase);
                
                if (File.Exists(blockLogPath))
                {
                    foreach (string line in File.ReadAllLines(blockLogPath))
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("BLOCKED IP:")) continue;
                        try
                        {
                            var parts = line.Split(new string[] { "|" }, StringSplitOptions.None);
                            string time = line.Substring(1, 19);
                            string ip = line.Split(new string[] { "BLOCKED IP:" }, StringSplitOptions.None)[1].Split('|')[0].Trim();
                            string attemptsStr = parts.Length > 1 ? parts[1].Split(':')[1].Trim() : "5";
                            int attempts = int.TryParse(attemptsStr, out int a) ? a : 5;
                            
                            // Parse Until timestamp
                            DateTime until = DateTime.MaxValue;
                            foreach (var part in parts)
                            {
                                if (part.Contains("Until:"))
                                {
                                    string untilStr = part.Split("Until:")[1].Trim();
                                    if (DateTime.TryParseExact(untilStr, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeLocal, out DateTime parsed))
                                        until = parsed;
                                    break;
                                }
                            }
                            
                            string status = DateTime.Now <= until ? "ACTIVE" : "EXPIRED";
                            
                            // Keep latest time and max attempts for this IP
                            if (groupedByIP.TryGetValue(ip, out var existing))
                            {
                                // Keep the most recent time (later timestamp)
                                string newTime = time.CompareTo(existing.Time) > 0 ? time : existing.Time;
                                int maxAttempts = Math.Max(attempts, existing.MaxAttempts);
                                DateTime latestUntil = until > existing.Until ? until : existing.Until;
                                groupedByIP[ip] = (newTime, maxAttempts, status, latestUntil);
                            }
                            else
                            {
                                groupedByIP[ip] = (time, attempts, status, until);
                            }
                        }
                        catch { }
                    }
                }

                // Sort: ACTIVE first, then by Until time
                var sorted = groupedByIP
                    .OrderBy(kvp => kvp.Value.Status == "ACTIVE" ? 0 : 1)
                    .ThenBy(kvp => kvp.Value.Until)
                    .ToList();

                gridBlocked.Rows.Clear();
                foreach (var entry in sorted)
                {
                    string ip = entry.Key;
                    bool conflict = whitelistIps.Contains(ip);
                    gridBlocked.Rows.Add(false, entry.Value.Time, ip, entry.Value.MaxAttempts.ToString(), entry.Value.Status, conflict);
                }

                lblStatus.Text = "Blocked IPs grouped: " + DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error loading blocked IPs: " + ex.Message;
            }
        }

        private void LoadCurrentConnections()
        {
            if (gridCurrentLog == null || txtCurrentPort == null)
                return;

            if (!int.TryParse(txtCurrentPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                lblStatus.Text = "Error: invalid port";
                return;
            }

            try
            {
                RefreshListFlags();
                UpdateCurrentGridDlWlFlags();

                var ipProps = IPGlobalProperties.GetIPGlobalProperties();
                var active = ipProps.GetActiveTcpConnections();
                var listeners = ipProps.GetActiveTcpListeners();
                string seenAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                foreach (var ep in listeners.Where(ep => ep.Port == port))
                {
                    string local = ep.Address + ":" + ep.Port;
                    AppendCurrentLogEntry(seenAt, local, "-", "Listen");
                }

                foreach (var c in active.Where(c => c.LocalEndPoint.Port == port || c.RemoteEndPoint.Port == port))
                {
                    string local = c.LocalEndPoint.Address + ":" + c.LocalEndPoint.Port;
                    string remote = c.RemoteEndPoint.Address + ":" + c.RemoteEndPoint.Port;
                    string state = c.State.ToString();
                    AppendCurrentLogEntry(seenAt, local, remote, state);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "CurrentLog error: " + ex.Message;
            }
        }

        private void AppendCurrentLogEntry(string seenAt, string local, string remote, string state)
        {
            string key = local + "|" + remote;
            if (currentConnState.TryGetValue(key, out string previousState) && string.Equals(previousState, state, StringComparison.OrdinalIgnoreCase))
                return;

            currentConnState[key] = state;
            string remoteIp = ExtractRemoteIp(remote);
            int attempts = 0;
            bool banned = false;

            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                string attemptKey = local + "|" + remote;
                if (!countedAttemptEndpoints.Contains(attemptKey))
                {
                    countedAttemptEndpoints.Add(attemptKey);
                    if (!authAttemptCounters.ContainsKey(remoteIp))
                        authAttemptCounters[remoteIp] = 0;
                    authAttemptCounters[remoteIp]++;
                }

                attempts = authAttemptCounters[remoteIp];
                banned = attempts >= CURRENT_BAN_THRESHOLD;
            }

            string dlWl = "-";
            if (!string.IsNullOrWhiteSpace(remoteIp))
            {
                bool inWl = currentWhitelistIps.Contains(remoteIp);
                bool inBl = currentBlockedIps.Contains(remoteIp);
                if (inWl && inBl) dlWl = "BW";
                else if (inWl) dlWl = "W";
                else if (inBl) dlWl = "B";
            }

            gridCurrentLog.Rows.Add(seenAt, local, remote, attempts, banned, dlWl, state);
            gridCurrentLog.Sort(gridCurrentLog.Columns["SeenAt"], System.ComponentModel.ListSortDirection.Descending);

            try
            {
                Directory.CreateDirectory(logDir);
                File.AppendAllText(currentLogFilePath, $"[{seenAt}] {local} -> {remote} | Attempts={attempts} | Banned={banned} | {state}{Environment.NewLine}");
            }
            catch
            {
            }
        }

        private void ClearCurrentLog()
        {
            currentConnState.Clear();
            countedAttemptEndpoints.Clear();
            authAttemptCounters.Clear();
            gridCurrentLog.Rows.Clear();

            try
            {
                if (File.Exists(currentLogFilePath))
                    File.Delete(currentLogFilePath);
            }
            catch
            {
            }

            lblStatus.Text = "Current Log cleared";
        }

        private string ExtractRemoteIp(string remoteEndpoint)
        {
            if (string.IsNullOrWhiteSpace(remoteEndpoint) || remoteEndpoint == "-")
                return null;

            int lastColon = remoteEndpoint.LastIndexOf(':');
            if (lastColon <= 0)
                return null;

            string ipPart = remoteEndpoint.Substring(0, lastColon).Trim();
            if (System.Net.IPAddress.TryParse(ipPart, out _))
                return ipPart;

            return null;
        }

        private void RefreshListFlags()
        {
            currentWhitelistIps.Clear();
            currentBlockedIps.Clear();

            try
            {
                if (File.Exists(whitelistPath))
                {
                    foreach (string line in ReadAllLinesSharedWithRetry(whitelistPath))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.Contains("IP:"))
                        {
                            string ip = line.Split(new[] { "IP:" }, StringSplitOptions.None)[1].Trim();
                            if (System.Net.IPAddress.TryParse(ip, out _)) currentWhitelistIps.Add(ip);
                        }
                    }
                }
            }
            catch
            {
            }

            try
            {
                if (File.Exists(blockLogPath))
                {
                    foreach (string line in ReadAllLinesSharedWithRetry(blockLogPath))
                    {
                        if (string.IsNullOrWhiteSpace(line) || !line.Contains("BLOCKED IP:")) continue;
                        string ip = line.Split(new[] { "BLOCKED IP:" }, StringSplitOptions.None)[1].Split('|')[0].Trim();
                        if (System.Net.IPAddress.TryParse(ip, out _)) currentBlockedIps.Add(ip);
                    }
                }
            }
            catch
            {
            }
        }

        private void UpdateCurrentGridDlWlFlags()
        {
            foreach (DataGridViewRow row in gridCurrentLog.Rows)
            {
                if (row.IsNewRow) continue;
                string remote = row.Cells["Remote"]?.Value?.ToString();
                string remoteIp = ExtractRemoteIp(remote);

                string dlWl = "-";
                if (!string.IsNullOrWhiteSpace(remoteIp))
                {
                    bool inWl = currentWhitelistIps.Contains(remoteIp);
                    bool inBl = currentBlockedIps.Contains(remoteIp);
                    if (inWl && inBl) dlWl = "BW";
                    else if (inWl) dlWl = "W";
                    else if (inBl) dlWl = "B";
                }

                row.Cells["DlWl"].Value = dlWl;
            }
        }

        private string[] ReadAllLinesSharedWithRetry(string path)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        var lines = new List<string>();
                        while (!sr.EndOfStream)
                            lines.Add(sr.ReadLine());
                        return lines.ToArray();
                    }
                }
                catch (IOException)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }

            return new string[0];
        }

        private bool RemoveIpFromBlocklistAndFirewall(string ip, out string error)
        {
            error = string.Empty;
            bool fileOk = false;
            bool fwOk = false;

            try
            {
                if (File.Exists(blockLogPath))
                {
                    string[] lines = ReadAllLinesSharedWithRetry(blockLogPath);
                    var kept = new List<string>(lines.Length);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        if (line.Contains("BLOCKED IP:") && line.Contains(ip)) continue;
                        kept.Add(line);
                    }

                    File.WriteAllLines(blockLogPath, kept);
                }
                fileOk = true;
            }
            catch (Exception ex)
            {
                error += "block_list.log: " + ex.Message + "\n";
            }

            try
            {
                string script =
                    "$r = Get-NetFirewallRule -Name 'RDP_BLOCK_ALL' -ErrorAction SilentlyContinue;" +
                    "if ($null -eq $r) { exit 0 };" +
                    "$af = Get-NetFirewallRule -Name 'RDP_BLOCK_ALL' | Get-NetFirewallAddressFilter;" +
                    "$cur = @($af.RemoteAddress);" +
                    "$new = @($cur | Where-Object { $_ -and $_ -ne '" + ip + "' });" +
                    "if ($new.Count -eq 0) { Remove-NetFirewallRule -Name 'RDP_BLOCK_ALL' -ErrorAction SilentlyContinue | Out-Null } " +
                    "else { Get-NetFirewallRule -Name 'RDP_BLOCK_ALL' | Set-NetFirewallAddressFilter -RemoteAddress ($new -join ',') | Out-Null }";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"" + script + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var p = System.Diagnostics.Process.Start(psi))
                {
                    p.WaitForExit(5000);
                    fwOk = p.ExitCode == 0;
                    if (!fwOk)
                    {
                        error += p.StandardError.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                error += "firewall: " + ex.Message;
            }

            return fileOk && fwOk;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            currentLogTimer?.Stop();
            base.OnFormClosing(e);
        }
    }
}
