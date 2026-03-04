using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Management.Automation;

namespace RDPMonitor
{
    public class MainForm : Form
    {
        private const string SERVICE_NAME = "RDPSecurityService";
        private const string LOG_DIR = @"C:\ProgramData\RDPSecurityService";
        private const string LOCAL_SERVICE_EXE = @"C:\Users\samoilenkod\source\repos\Winservice\artifacts\final\winservice\WinService.exe";
        
        // Top panels
        private Label lblServiceStatus;
        private Label lblConfig;
        private Button btnRefresh;
        private Button btnStartService;
        private Button btnStopService;
        private ComboBox cmbLanguage;
        
        // TabControl
        private TabControl tabControl;
        
        // Tab: Current Logs
        private TextBox txtLogs;
        
        // Tab: Banned IPs  
        private ListBox lstBannedIPs;
        private Button btnUnblockIP;
        private TextBox txtIPToUnblock;
        
        // Tab: White List
        private ListBox lstWhiteList;
        private TextBox txtNewWhiteIP;
        private Button btnAddWhiteIP;
        private Button btnRemoveWhiteIP;
        
        // Tab: Manual Block
        private TextBox txtIPToBlock;
        private TextBox txtBlockMinutes;
        private Button btnManualBlock;
        private Label lblBlockStatus;

        // Tab: Settings
        private TextBox txtPort;
        private DataGridView dgvBlockLevels;
        private Button btnSaveConfig;
        private Button btnAddLevel;
        private Button btnRemoveLevel;
        
        // Timers and watchers
        private System.Windows.Forms.Timer refreshTimer;
        private FileSystemWatcher fileWatcher;
        private long lastAccessLogPosition = 0;
        private long lastBlockLogPosition = 0;
        private long lastServiceLogPosition = 0;

        public MainForm()
        {
            InitializeComponents();
            SetupFileWatcher();
            LoadInitialData();
            StartAutoRefresh();
        }

        private void InitializeComponents()
        {
            this.Text = Lang.Get("MAIN_TITLE");
            this.Size = new Size(1000, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("Segoe UI", 9);

            // ===== TOP STATUS PANEL =====
            var pnlTop = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(980, 180),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Left panel - Service Status
            var pnlServiceStatus = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(500, 100),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlTop.Controls.Add(pnlServiceStatus);

            // Right panel - Configuration
            var pnlConfig = new Panel
            {
                Location = new Point(515, 0),
                Size = new Size(465, 180),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlTop.Controls.Add(pnlConfig);

            var lblStatusHeader = new Label
            {
                Text = Lang.Get("SERVICE_STATUS_HEADER"),
                Location = new Point(10, 10),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnlServiceStatus.Controls.Add(lblStatusHeader);

            lblServiceStatus = new Label
            {
                Location = new Point(10, 35),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11),
                Text = Lang.Get("SERVICE_CHECKING"),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            pnlServiceStatus.Controls.Add(lblServiceStatus);

            btnStartService = new Button
            {
                Text = Lang.Get("BTN_START"),
                Location = new Point(10, 65),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnStartService.FlatAppearance.BorderSize = 0;
            btnStartService.Click += BtnStartService_Click;
            pnlServiceStatus.Controls.Add(btnStartService);

            btnStopService = new Button
            {
                Text = Lang.Get("BTN_STOP"),
                Location = new Point(100, 65),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnStopService.FlatAppearance.BorderSize = 0;
            btnStopService.Click += BtnStopService_Click;
            pnlServiceStatus.Controls.Add(btnStopService);

            btnRefresh = new Button
            {
                Text = Lang.Get("BTN_REFRESH"),
                Location = new Point(190, 65),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += BtnRefresh_Click;
            pnlServiceStatus.Controls.Add(btnRefresh);

            // Config on right side of top panel
            var lblConfigHeader = new Label
            {
                Text = Lang.Get("CONFIG_HEADER"),
                Location = new Point(10, 10),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnlConfig.Controls.Add(lblConfigHeader);

            // Language selector
            cmbLanguage = new ComboBox
            {
                Location = new Point(340, 8),
                Size = new Size(110, 25),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204),
                BackColor = Color.White
            };
            cmbLanguage.Items.AddRange(new object[] { "🇺🇦 UA", "🇬🇧 EN" });
            cmbLanguage.SelectedIndex = Program.CurrentLanguage == "UA" ? 0 : 1;
            cmbLanguage.SelectedIndexChanged += CmbLanguage_SelectedIndexChanged;
            pnlConfig.Controls.Add(cmbLanguage);

            lblConfig = new Label
            {
                Location = new Point(10, 35),
                Size = new Size(450, 140),
                Font = new Font("Consolas", 8),
                Text = "Loading...",
                AutoSize = false,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            pnlConfig.Controls.Add(lblConfig);

            this.Controls.Add(pnlTop);

            // ===== TAB CONTROL =====
            tabControl = new TabControl
            {
                Location = new Point(10, 210),
                Size = new Size(980, 530),
                Font = new Font("Segoe UI", 9)
            };

            // Tab 1: Current Logs
            var tabLogs = new TabPage(Lang.Get("TAB_CURRENT_LOGS"));
            CreateCurrentLogsTab(tabLogs);
            tabControl.TabPages.Add(tabLogs);

            // Tab 2: Banned IPs
            var tabBanned = new TabPage(Lang.Get("TAB_BANNED_IPS"));
            CreateBannedIPsTab(tabBanned);
            tabControl.TabPages.Add(tabBanned);

            // Tab 3: White List
            var tabWhite = new TabPage(Lang.Get("TAB_WHITE_LIST"));
            CreateWhiteListTab(tabWhite);
            tabControl.TabPages.Add(tabWhite);

            // Tab 4: Manual Block
            var tabManual = new TabPage(Lang.Get("TAB_MANUAL_BLOCK"));
            CreateManualBlockTab(tabManual);
            tabControl.TabPages.Add(tabManual);

            // Tab 5: Settings
            var tabSettings = new TabPage(Lang.Get("TAB_SETTINGS"));
            CreateSettingsTab(tabSettings);
            tabControl.TabPages.Add(tabSettings);

            this.Controls.Add(tabControl);
        }

        private void CreateCurrentLogsTab(TabPage tab)
        {
            txtLogs = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(0, 255, 0),
                BorderStyle = BorderStyle.None
            };
            tab.Controls.Add(txtLogs);
        }

        private void CreateBannedIPsTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var lbl = new Label
            {
                Text = "Поточно заблоковані IP (з правила брандмауера):",
                AutoSize = true,
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lbl);

            lstBannedIPs = new ListBox
            {
                Location = new Point(10, 30),
                Size = new Size(950, 400),
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            pnl.Controls.Add(lstBannedIPs);

            var lblUnblock = new Label
            {
                Text = "IP для розблокування:",
                Location = new Point(10, 440),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            pnl.Controls.Add(lblUnblock);

            txtIPToUnblock = new TextBox
            {
                Location = new Point(10, 465),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9),
                Padding = new Padding(5)
            };
            pnl.Controls.Add(txtIPToUnblock);

            btnUnblockIP = new Button
            {
                Text = "🔓 Розблокувати IP",
                Location = new Point(220, 465),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(255, 152, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnUnblockIP.FlatAppearance.BorderSize = 0;
            btnUnblockIP.Click += BtnUnblockIP_Click;
            pnl.Controls.Add(btnUnblockIP);

            tab.Controls.Add(pnl);
        }

        private void CreateWhiteListTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var lbl = new Label
            {
                Text = "Пили IP у білому списку (виключені з блокування):",
                AutoSize = true,
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lbl);

            lstWhiteList = new ListBox
            {
                Location = new Point(10, 30),
                Size = new Size(950, 350),
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            pnl.Controls.Add(lstWhiteList);

            var lblAdd = new Label
            {
                Text = "Додати IP до білого списку:",
                Location = new Point(10, 390),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            pnl.Controls.Add(lblAdd);

            txtNewWhiteIP = new TextBox
            {
                Location = new Point(10, 415),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "192.168.1.100"
            };
            pnl.Controls.Add(txtNewWhiteIP);

            btnAddWhiteIP = new Button
            {
                Text = "+ Додати",
                Location = new Point(220, 415),
                Size = new Size(80, 25),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnAddWhiteIP.FlatAppearance.BorderSize = 0;
            btnAddWhiteIP.Click += BtnAddWhiteIP_Click;
            pnl.Controls.Add(btnAddWhiteIP);

            btnRemoveWhiteIP = new Button
            {
                Text = "✕ Remove",
                Location = new Point(310, 415),
                Size = new Size(100, 25),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnRemoveWhiteIP.FlatAppearance.BorderSize = 0;
            btnRemoveWhiteIP.Click += BtnRemoveWhiteIP_Click;
            pnl.Controls.Add(btnRemoveWhiteIP);

            tab.Controls.Add(pnl);
        }

        private void CreateManualBlockTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var lblTitle = new Label
            {
                Text = "Почна блокування IP-адреси",
                AutoSize = true,
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54)
            };
            pnl.Controls.Add(lblTitle);

            var lblIP = new Label
            {
                Text = "IP-адреса:",
                AutoSize = true,
                Location = new Point(10, 50),
                Font = new Font("Segoe UI", 9)
            };
            pnl.Controls.Add(lblIP);

            txtIPToBlock = new TextBox
            {
                Location = new Point(10, 75),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "192.168.1.50"
            };
            pnl.Controls.Add(txtIPToBlock);

            var lblMinutes = new Label
            {
                Text = "Block Duration (minutes):",
                AutoSize = true,
                Location = new Point(10, 115),
                Font = new Font("Segoe UI", 9)
            };
            pnl.Controls.Add(lblMinutes);

            txtBlockMinutes = new TextBox
            {
                Location = new Point(10, 140),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 10),
                Text = "60",
                PlaceholderText = "60"
            };
            pnl.Controls.Add(txtBlockMinutes);

            btnManualBlock = new Button
            {
                Text = "🔒 BLOCK THIS IP",
                Location = new Point(10, 185),
                Size = new Size(300, 40),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnManualBlock.FlatAppearance.BorderSize = 0;
            btnManualBlock.Click += BtnManualBlock_Click;
            pnl.Controls.Add(btnManualBlock);

            lblBlockStatus = new Label
            {
                Location = new Point(10, 240),
                Size = new Size(900, 180),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(250, 250, 250),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Status will appear here...",
                AutoSize = false
            };
            pnl.Controls.Add(lblBlockStatus);

            tab.Controls.Add(pnl);
        }

        private void CreateSettingsTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };

            var lblTitle = new Label
            {
                Text = "Service Configuration",
                AutoSize = true,
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnl.Controls.Add(lblTitle);

            // RDP Port
            var lblPort = new Label
            {
                Text = "RDP Port:",
                AutoSize = true,
                Location = new Point(10, 50),
                Font = new Font("Segoe UI", 9)
            };
            pnl.Controls.Add(lblPort);

            txtPort = new TextBox
            {
                Location = new Point(10, 75),
                Size = new Size(150, 28),
                Font = new Font("Segoe UI", 10),
                Text = "3389"
            };
            pnl.Controls.Add(txtPort);

            // Block Levels
            var lblLevels = new Label
            {
                Text = "Block Levels (Attempts → Minutes):",
                AutoSize = true,
                Location = new Point(10, 120),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lblLevels);

            dgvBlockLevels = new DataGridView
            {
                Location = new Point(10, 145),
                Size = new Size(400, 250),
                Font = new Font("Segoe UI", 9),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            dgvBlockLevels.Columns.Add("Attempts", "Attempts");
            dgvBlockLevels.Columns.Add("BlockMinutes", "Block Minutes");

            for (int i = 0; i < dgvBlockLevels.Columns.Count; i++)
            {
                dgvBlockLevels.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            pnl.Controls.Add(dgvBlockLevels);

            btnAddLevel = new Button
            {
                Text = "+ Add Level",
                Location = new Point(10, 405),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnAddLevel.FlatAppearance.BorderSize = 0;
            btnAddLevel.Click += BtnAddLevel_Click;
            pnl.Controls.Add(btnAddLevel);

            btnRemoveLevel = new Button
            {
                Text = "✕ Remove",
                Location = new Point(120, 405),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnRemoveLevel.FlatAppearance.BorderSize = 0;
            btnRemoveLevel.Click += BtnRemoveLevel_Click;
            pnl.Controls.Add(btnRemoveLevel);

            btnSaveConfig = new Button
            {
                Text = "💾 SAVE CONFIGURATION",
                Location = new Point(10, 450),
                Size = new Size(400, 40),
                BackColor = Color.FromArgb(33, 150, 243),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            btnSaveConfig.FlatAppearance.BorderSize = 0;
            btnSaveConfig.Click += BtnSaveConfig_Click;
            pnl.Controls.Add(btnSaveConfig);

            tab.Controls.Add(pnl);
            
            // Load initial config
            LoadConfigToSettings();
        }

        private void SetupFileWatcher()
        {
            try
            {
                if (!Directory.Exists(LOG_DIR)) return;
                fileWatcher = new FileSystemWatcher(LOG_DIR)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    Filter = "*.log"
                };
                fileWatcher.Changed += (s, e) => this.BeginInvoke(new Action(() => OnLogFileChanged(e.FullPath)));
                fileWatcher.EnableRaisingEvents = true;
            }
            catch { }
        }

        private void OnLogFileChanged(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath).ToLower();
                if (fileName == "access.log")
                    ReadLogTail(filePath, ref lastAccessLogPosition, "[ACCESS]");
                else if (fileName == "block_list.log")
                {
                    ReadLogTail(filePath, ref lastBlockLogPosition, "[BLOCK]");
                    LoadBannedIPs();
                }
                else if (fileName == "service.log")
                    ReadLogTail(filePath, ref lastServiceLogPosition, "[SERVICE]");
            }
            catch { }
        }

        private void ReadLogTail(string filePath, ref long lastPosition, string prefix)
        {
            try
            {
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    if (lastPosition > fs.Length) lastPosition = 0;
                    fs.Seek(lastPosition, SeekOrigin.Begin);
                    using (var sr = new StreamReader(fs, Encoding.UTF8))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                AppendLog($"{prefix} {line}");
                        }
                    }
                    lastPosition = fs.Position;
                }
            }
            catch { }
        }

        private void LoadInitialData()
        {
            UpdateServiceStatus();
            LoadConfiguration();
            LoadBannedIPs();
            LoadWhiteList();
            LoadRecentLogs();
        }

        private void UpdateServiceStatus()
        {
            try
            {
                var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == SERVICE_NAME);
                if (service == null)
                {
                    if (IsLocalEngineRunning())
                    {
                        lblServiceStatus.Text = "Local mode: RUNNING (service not installed)";
                        lblServiceStatus.ForeColor = Color.FromArgb(255, 152, 0);
                        btnStartService.Enabled = false;
                        btnStopService.Enabled = true;
                    }
                    else
                    {
                        lblServiceStatus.Text = "⚠ Service NOT INSTALLED";
                        lblServiceStatus.ForeColor = Color.FromArgb(244, 67, 54);
                        btnStartService.Enabled = true;
                        btnStopService.Enabled = false;
                    }
                    return;
                }

                service.Refresh();
                lblServiceStatus.Text = $"Status: {service.Status} | Startup: {service.StartType}";
                lblServiceStatus.ForeColor = service.Status == ServiceControllerStatus.Running
                    ? Color.FromArgb(76, 175, 80)
                    : Color.FromArgb(244, 67, 54);
                btnStartService.Enabled = service.Status != ServiceControllerStatus.Running;
                btnStopService.Enabled = service.Status == ServiceControllerStatus.Running;
            }
            catch (Exception ex)
            {
                lblServiceStatus.Text = $"Error: {ex.Message}";
                lblServiceStatus.ForeColor = Color.FromArgb(244, 67, 54);
            }
        }

        private bool IsLocalEngineRunning()
        {
            try
            {
                return Process.GetProcessesByName("WinService").Any();
            }
            catch
            {
                return false;
            }
        }

        private bool TryStartLocalEngine()
        {
            try
            {
                if (!File.Exists(LOCAL_SERVICE_EXE))
                {
                    AppendLog($"[ERROR] Local engine not found: {LOCAL_SERVICE_EXE}");
                    return false;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = LOCAL_SERVICE_EXE,
                    Arguments = "run",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                AppendLog("[MONITOR] Local engine started (WinService.exe run)");
                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Failed to start local engine: {ex.Message}");
                return false;
            }
        }

        private bool TryStopLocalEngine()
        {
            try
            {
                var processes = Process.GetProcessesByName("WinService");
                foreach (var process in processes)
                {
                    process.Kill();
                }

                if (processes.Length > 0)
                    AppendLog($"[MONITOR] Local engine stopped ({processes.Length} proc)");

                return true;
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Failed to stop local engine: {ex.Message}");
                return false;
            }
        }

        private void LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(LOG_DIR, "config.json");
                if (!File.Exists(configPath))
                {
                    lblConfig.Text = "Config not found";
                    return;
                }
                
                var json = File.ReadAllText(configPath, Encoding.UTF8);
                var config = JsonSerializer.Deserialize<ServiceConfig>(json);
                
                if (config != null)
                {
                    var levelsText = new System.Text.StringBuilder();
                    levelsText.AppendLine($"RDP Port: {config.Port}");
                    levelsText.AppendLine("Block Levels:");
                    
                    foreach (var level in config.Levels)
                    {
                        levelsText.AppendLine($"  • {level.Attempts} attempts → {level.BlockMinutes} minutes");
                    }
                    
                    lblConfig.Text = levelsText.ToString().TrimEnd();
                }
                else
                {
                    lblConfig.Text = "Config parse error";
                }
            }
            catch (Exception ex)
            {
                lblConfig.Text = $"Error loading config: {ex.Message}";
            }
        }

        private void LoadBannedIPs()
        {
            try
            {
                lstBannedIPs.Items.Clear();
                var blockedIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                using (var ps = PowerShell.Create())
                {
                    ps.AddScript("Get-NetFirewallRule -Name 'RDP_BLOCK_ALL' -ErrorAction SilentlyContinue | Get-NetFirewallAddressFilter | Select-Object -ExpandProperty RemoteAddress");
                    var results = ps.Invoke();
                    foreach (var result in results)
                    {
                        var ips = result?.ToString()?.Split(',') ?? Array.Empty<string>();
                        foreach (var ip in ips)
                        {
                            var trimmed = ip?.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed) && trimmed != "Any")
                            {
                                blockedIps.Add(trimmed);
                            }
                        }
                    }
                }

                if (blockedIps.Count == 0)
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "netsh",
                        Arguments = "advfirewall firewall show rule name=\"RDP_BLOCK_ALL\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(psi))
                    {
                        if (process != null)
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit(3000);

                            var match = Regex.Match(output, @"Удаленный IP-адрес:\s*(.+)$", RegexOptions.Multiline);
                            if (!match.Success)
                                match = Regex.Match(output, @"RemoteIP:\s*(.+)$", RegexOptions.Multiline);

                            if (match.Success)
                            {
                                var raw = match.Groups[1].Value.Trim();
                                var netshIps = raw.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var ip in netshIps)
                                {
                                    var cleaned = ip.Trim();
                                    if (!string.IsNullOrWhiteSpace(cleaned) && cleaned != "Any")
                                    {
                                        if (cleaned.EndsWith("/32", StringComparison.Ordinal))
                                            cleaned = cleaned.Substring(0, cleaned.Length - 3);
                                        blockedIps.Add(cleaned);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var ip in blockedIps.OrderBy(x => x))
                    lstBannedIPs.Items.Add(ip);

                if (lstBannedIPs.Items.Count == 0)
                    lstBannedIPs.Items.Add("(no blocked IPs)");
            }
            catch { }
        }

        private void LoadWhiteList()
        {
            try
            {
                lstWhiteList.Items.Clear();
                string whitelistPath = Path.Combine(LOG_DIR, "whiteList.log");
                if (File.Exists(whitelistPath))
                {
                    foreach (var line in File.ReadAllLines(whitelistPath, Encoding.UTF8))
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;
                        int idx = line.IndexOf("IP:", StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                        {
                            string ip = line.Substring(idx + 3).Trim();
                            if (System.Net.IPAddress.TryParse(ip, out _))
                                lstWhiteList.Items.Add(ip);
                        }
                    }
                }
                if (lstWhiteList.Items.Count == 0)
                    lstWhiteList.Items.Add("(no whitelisted IPs)");
            }
            catch { }
        }

        private void LoadRecentLogs()
        {
            txtLogs.Clear();
            AppendLog("=== RDP Security Monitor Started ===");
            AppendLog($"Log directory: {LOG_DIR}\n");

            LoadLogFile("service.log", "[SERVICE]", 10);
            LoadLogFile("access.log", "[ACCESS]", 10);
            LoadLogFile("block_list.log", "[BLOCK]", 15);
        }

        private void LoadLogFile(string fileName, string prefix, int tailLines)
        {
            try
            {
                string filePath = Path.Combine(LOG_DIR, fileName);
                if (!File.Exists(filePath)) return;

                var lines = File.ReadAllLines(filePath, Encoding.UTF8).Reverse().Take(tailLines).Reverse();
                foreach (var line in lines)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        AppendLog($"{prefix} {line}");
                }

                var fi = new FileInfo(filePath);
                if (fileName == "access.log") lastAccessLogPosition = fi.Length;
                else if (fileName == "block_list.log") lastBlockLogPosition = fi.Length;
                else if (fileName == "service.log") lastServiceLogPosition = fi.Length;
            }
            catch { }
        }

        private void AppendLog(string message)
        {
            if (txtLogs.InvokeRequired)
            {
                txtLogs.Invoke(new Action(() => AppendLog(message)));
                return;
            }
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLogs.AppendText($"[{timestamp}] {message}\r\n");
            txtLogs.SelectionStart = txtLogs.Text.Length;
            txtLogs.ScrollToCaret();
        }

        private void StartAutoRefresh()
        {
            refreshTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            refreshTimer.Tick += (s, e) =>
            {
                UpdateServiceStatus();
                LoadBannedIPs();
                PollLogs();
            };
            refreshTimer.Start();
        }

        private void PollLogs()
        {
            try
            {
                string accessPath = Path.Combine(LOG_DIR, "access.log");
                string blockPath = Path.Combine(LOG_DIR, "block_list.log");
                string servicePath = Path.Combine(LOG_DIR, "service.log");

                if (File.Exists(accessPath))
                    ReadLogTail(accessPath, ref lastAccessLogPosition, "[ACCESS]");

                if (File.Exists(blockPath))
                    ReadLogTail(blockPath, ref lastBlockLogPosition, "[BLOCK]");

                if (File.Exists(servicePath))
                    ReadLogTail(servicePath, ref lastServiceLogPosition, "[SERVICE]");
            }
            catch { }
        }

        private void CmbLanguage_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update current language
            Program.CurrentLanguage = cmbLanguage.SelectedIndex == 0 ? "UA" : "EN";
            
            // Reload all UI texts
            RefreshAllUITexts();
            
            // Reload data to update with new language
            LoadInitialData();
        }

        private void RefreshAllUITexts()
        {
            // Window title
            this.Text = Lang.Get("MAIN_TITLE");
            
            // Tab titles
            tabControl.TabPages[0].Text = Lang.Get("TAB_CURRENT_LOGS");
            tabControl.TabPages[1].Text = Lang.Get("TAB_BANNED_IPS");
            tabControl.TabPages[2].Text = Lang.Get("TAB_WHITE_LIST");
            tabControl.TabPages[3].Text = Lang.Get("TAB_MANUAL_BLOCK");
            tabControl.TabPages[4].Text = Lang.Get("TAB_SETTINGS");
            
            // Buttons
            btnStartService.Text = Lang.Get("BTN_START");
            btnStopService.Text = Lang.Get("BTN_STOP");
            btnRefresh.Text = Lang.Get("BTN_REFRESH");
            btnSaveConfig.Text = Lang.Get("BTN_SAVE_CONFIGURATION");
            btnAddLevel.Text = Lang.Get("BTN_ADD_LEVEL_WITH_PLUS");
            btnRemoveLevel.Text = Lang.Get("BTN_REMOVE_WITH_X");
            btnManualBlock.Text = Lang.Get("BTN_BLOCK_THIS_IP");
            btnUnblockIP.Text = Lang.Get("BTN_UNBLOCK_IP");
            btnAddWhiteIP.Text = Lang.Get("BTN_ADD_WITH_PLUS");
            btnRemoveWhiteIP.Text = Lang.Get("BTN_REMOVE_WITH_X");
            
            // DataGridView columns
            dgvBlockLevels.Columns[0].HeaderText = Lang.Get("GRID_ATTEMPTS");
            dgvBlockLevels.Columns[1].HeaderText = Lang.Get("GRID_BLOCK_MINUTES");
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            LoadInitialData();
            AppendLog("[MONITOR] Manual refresh triggered");
        }

        private void BtnStartService_Click(object sender, EventArgs e)
        {
            try
            {
                var existingService = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == SERVICE_NAME);
                if (existingService == null)
                {
                    if (TryStartLocalEngine())
                    {
                        UpdateServiceStatus();
                    }
                    return;
                }

                var serviceController = new ServiceController(SERVICE_NAME);
                serviceController.Start();
                AppendLog("[MONITOR] Starting service...");
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                UpdateServiceStatus();
                AppendLog("[MONITOR] Service started successfully");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Service start failed: {ex.Message}");

                if (TryStartLocalEngine())
                {
                    UpdateServiceStatus();
                    return;
                }

                MessageBox.Show($"Failed to start service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStopService_Click(object sender, EventArgs e)
        {
            try
            {
                var existingService = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == SERVICE_NAME);
                if (existingService == null)
                {
                    TryStopLocalEngine();
                    UpdateServiceStatus();
                    return;
                }

                var serviceController = new ServiceController(SERVICE_NAME);
                serviceController.Stop();
                AppendLog("[MONITOR] Stopping service...");
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                UpdateServiceStatus();
                AppendLog("[MONITOR] Service stopped successfully");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Service stop failed: {ex.Message}");

                if (TryStopLocalEngine())
                {
                    UpdateServiceStatus();
                    return;
                }

                MessageBox.Show($"Failed to stop service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUnblockIP_Click(object sender, EventArgs e)
        {
            string ip = txtIPToUnblock.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip))
            {
                MessageBox.Show("Enter IP to unlock", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Remove from block_list.log
                string blockLogPath = Path.Combine(LOG_DIR, "block_list.log");
                if (File.Exists(blockLogPath))
                {
                    var lines = File.ReadAllLines(blockLogPath, Encoding.UTF8).Where(l => !l.Contains(ip)).ToList();
                    File.WriteAllLines(blockLogPath, lines, Encoding.UTF8);
                }

                // Refresh firewall rule
                using (var ps = PowerShell.Create())
                {
                    ps.AddScript($"Remove-NetFirewallRule -Name 'RDP_BLOCK_ALL' -ErrorAction SilentlyContinue");
                    ps.Invoke();
                }

                LoadBannedIPs();
                txtIPToUnblock.Clear();
                AppendLog($"[MONITOR] IP {ip} unlocked manually");
                MessageBox.Show($"IP {ip} has been unlocked", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"[ERROR] Unlock failed: {ex.Message}");
            }
        }

        private void BtnAddWhiteIP_Click(object sender, EventArgs e)
        {
            string ip = txtNewWhiteIP.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip) || !System.Net.IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("Enter valid IP address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string whitelistPath = Path.Combine(LOG_DIR, "whiteList.log");
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] IP: {ip}";
                File.AppendAllText(whitelistPath, entry + Environment.NewLine, Encoding.UTF8);

                LoadWhiteList();
                txtNewWhiteIP.Clear();
                AppendLog($"[MONITOR] IP {ip} added to whitelist");
                MessageBox.Show($"IP {ip} added to whitelist", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRemoveWhiteIP_Click(object sender, EventArgs e)
        {
            if (lstWhiteList.SelectedIndex < 0)
            {
                MessageBox.Show("Select IP to remove", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string ip = lstWhiteList.SelectedItem.ToString();
            if (ip == "(no whitelisted IPs)") return;

            try
            {
                string whitelistPath = Path.Combine(LOG_DIR, "whiteList.log");
                var lines = File.ReadAllLines(whitelistPath, Encoding.UTF8).Where(l => !l.Contains(ip)).ToList();
                File.WriteAllLines(whitelistPath, lines, Encoding.UTF8);

                LoadWhiteList();
                AppendLog($"[MONITOR] IP {ip} removed from whitelist");
                MessageBox.Show($"IP {ip} removed from whitelist", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnManualBlock_Click(object sender, EventArgs e)
        {
            string ip = txtIPToBlock.Text.Trim();
            if (string.IsNullOrWhiteSpace(ip) || !System.Net.IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("Enter valid IP address", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(txtBlockMinutes.Text.Trim(), out int minutes) || minutes < 1)
            {
                MessageBox.Show("Enter valid block duration (minutes)", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Add to block_list.log
                string blockLogPath = Path.Combine(LOG_DIR, "block_list.log");
                DateTime until = DateTime.Now.AddMinutes(minutes);
                string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] BLOCKED IP: {ip} | Failed Attempts: MANUAL | BlockMinutes: {minutes} | Until: {until:yyyy-MM-dd HH:mm:ss}";
                File.AppendAllText(blockLogPath, entry + Environment.NewLine, Encoding.UTF8);

                // Update firewall rule
                using (var ps = PowerShell.Create())
                {
                    ps.AddScript($@"
                        $rule = Get-NetFirewallRule -Name 'RDP_BLOCK_ALL' -ErrorAction SilentlyContinue
                        if ($rule) {{
                            (Get-NetFirewallRule -Name 'RDP_BLOCK_ALL' | Get-NetFirewallAddressFilter) | Set-NetFirewallAddressFilter -RemoteAddress (
                                ((Get-NetFirewallRule -Name 'RDP_BLOCK_ALL' | Get-NetFirewallAddressFilter).RemoteAddress -split ',') + @('{ip}') | Where-Object {{$_ -and $_ -ne 'Any'}} | Select-Object -Unique
                            ) -ja ',' -ErrorAction SilentlyContinue
                        }}
                    ");
                    ps.Invoke();
                }

                LoadBannedIPs();
                lblBlockStatus.Text = $"✓ Successfully blocked IP {ip} for {minutes} minutes\nUntil: {until:yyyy-MM-dd HH:mm:ss}\n\nIP has been added to firewall rule.";
                lblBlockStatus.ForeColor = Color.FromArgb(76, 175, 80);
                AppendLog($"[MONITOR] Manually blocked IP {ip} for {minutes} minutes");
                txtIPToBlock.Clear();
            }
            catch (Exception ex)
            {
                lblBlockStatus.Text = $"✗ Error: {ex.Message}";
                lblBlockStatus.ForeColor = Color.FromArgb(244, 67, 54);
                AppendLog($"[ERROR] Manual block failed: {ex.Message}");
            }
        }


        private void LoadConfigToSettings()
        {
            try
            {
                string configPath = Path.Combine(LOG_DIR, "config.json");
                if (!File.Exists(configPath))
                {
                    MessageBox.Show($"Config not found at: {configPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var json = File.ReadAllText(configPath, Encoding.UTF8);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<ServiceConfig>(json, options);

                if (config != null)
                {
                    txtPort.Text = config.Port.ToString();
                    
                    dgvBlockLevels.Rows.Clear();
                    if (config.Levels != null)
                    {
                        foreach (var level in config.Levels)
                        {
                            dgvBlockLevels.Rows.Add(level.Attempts, level.BlockMinutes);
                        }
                    }
                    AppendLog("[MONITOR] Config loaded to Settings");
                }
                else
                {
                    AppendLog("[ERROR] Config is null after deserialization");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] LoadConfigToSettings failed: {ex.Message}");
                MessageBox.Show($"Error loading config: {ex.Message}\n\nPath: {Path.Combine(LOG_DIR, "config.json")}", "Error");
            }
        }

        private void BtnAddLevel_Click(object sender, EventArgs e)
        {
            dgvBlockLevels.Rows.Add(0, 0);
        }

        private void BtnRemoveLevel_Click(object sender, EventArgs e)
        {
            if (dgvBlockLevels.SelectedRows.Count > 0)
            {
                dgvBlockLevels.Rows.RemoveAt(dgvBlockLevels.SelectedRows[0].Index);
            }
            else
            {
                MessageBox.Show("Select a level to remove", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnSaveConfig_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate input
                if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
                {
                    MessageBox.Show("Invalid port number (1-65535)", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var levels = new List<BlockLevel>();
                foreach (DataGridViewRow row in dgvBlockLevels.Rows)
                {
                    if (row.Cells[0].Value != null && row.Cells[1].Value != null)
                    {
                        if (int.TryParse(row.Cells[0].Value.ToString(), out int attempts) &&
                            int.TryParse(row.Cells[1].Value.ToString(), out int minutes) &&
                            attempts > 0 && minutes > 0)
                        {
                            levels.Add(new BlockLevel { Attempts = attempts, BlockMinutes = minutes });
                        }
                    }
                }

                if (levels.Count == 0)
                {
                    MessageBox.Show("Add at least one block level", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var config = new ServiceConfig { Port = port, Levels = levels };
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(config, options);

                string configPath = Path.Combine(LOG_DIR, "config.json");
                File.WriteAllText(configPath, json, Encoding.UTF8);

                LoadConfiguration();
                AppendLog("[MONITOR] Configuration saved successfully");
                MessageBox.Show("Configuration saved successfully!\nService will reload settings automatically.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving config: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"[ERROR] Config save failed: {ex.Message}");
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            refreshTimer?.Stop();
            fileWatcher?.Dispose();
            base.OnFormClosing(e);
        }
    }

    public class ServiceConfig
    {
        [System.Text.Json.Serialization.JsonPropertyName("port")]
        public int Port { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("levels")]
        public List<BlockLevel> Levels { get; set; } = new List<BlockLevel>();
    }

    public class BlockLevel
    {
        [System.Text.Json.Serialization.JsonPropertyName("attempts")]
        public int Attempts { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("blockMinutes")]
        public int BlockMinutes { get; set; }
    }
}
