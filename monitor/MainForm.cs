using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Runtime.InteropServices;

namespace RDPMonitor
{
    public class MainForm : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        private const string SERVICE_NAME = "RDPSecurityService";
        private const string LOG_DIR = @"C:\ProgramData\RDPSecurityService";
        private const string LOCAL_SERVICE_EXE = @"C:\Users\samoilenkod\source\repos\Winservice\artifacts\final\winservice\WinService.exe";
        
        // Top panels
        private Panel pnlTopContainer;
        private Panel pnlServiceStatusContainer;
        private Panel pnlConfigurationContainer;
        private Label lblServiceStatusHeader;
        private Label lblConfigurationHeader;
        private Label lblServiceStatus;
        private Label lblConfig;
        private Button btnRefresh;
        private Button btnStartService;
        private Button btnStopService;
        private MenuStrip mainMenu;
        private ToolStripMenuItem menuLanguage;
        private ToolStripMenuItem menuLanguageUa;
        private ToolStripMenuItem menuLanguageEn;
        
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
        private CheckBox chkAntiBruteEnabled;
        private CheckBox chkSprayEnabled;
        private TextBox txtSprayWindowMinutes;
        private TextBox txtSprayUniqueIpsThreshold;
        private TextBox txtSprayBlockMinutes;
        private CheckBox chkRecurrenceEnabled;
        private TextBox txtRecurrenceLookbackHours;
        private TextBox txtRecurrenceStepMultiplier;
        private TextBox txtRecurrenceMaxMultiplier;
        private CheckBox chkSubnetEnabled;
        private TextBox txtSubnetWindowMinutes;
        private TextBox txtSubnetUniqueIpsThreshold;
        private TextBox txtSubnetBlockMinutes;
        
        // Tab: Telegram/Alerts
        private CheckBox chkTelegramEnabled;
        private TextBox txtTelegramBotToken;
        private TextBox txtTelegramChatId;
        private Button btnTestTelegram;
        private Button btnSaveTelegram;
        private Label lblTelegramStatus;
        private Dictionary<string, TextBox> txtMessageTemplates = new Dictionary<string, TextBox>();
        
        // Tab: Message Settings
        private CheckBox chkNotifyMonitorStart;
        private CheckBox chkNotifyMonitorClose;
        private CheckBox chkNotifyServiceStart;
        private CheckBox chkNotifyServiceStop;
        private CheckBox chkNotifyConfigSave;
        private Button btnSaveMessageSettings;
        
        // Timers and watchers
        private System.Windows.Forms.Timer refreshTimer;
        private FileSystemWatcher fileWatcher;
        private long lastAccessLogPosition = 0;
        private long lastBlockLogPosition = 0;
        private long lastServiceLogPosition = 0;
        private readonly Dictionary<string, DateTime> recentFileLogEntries = new Dictionary<string, DateTime>();
        private static readonly TimeSpan DuplicateLogWindow = TimeSpan.FromSeconds(20);
        private bool suppressIpMaskUpdate = false;

        public MainForm()
        {
            InitializeComponents();
            LoadInitialData();
            SetupFileWatcher();
            StartAutoRefresh();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
        }

        private void InitializeComponents()
        {
            this.Text = Lang.Get("MAIN_TITLE");
            this.Size = new Size(1000, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("Segoe UI", 9);

            this.ShowIcon = true;

            var appIcon = LoadApplicationIcon();
            if (appIcon != null)
            {
                this.Icon = appIcon;
            }

            mainMenu = new MenuStrip
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 9),
                ImageScalingSize = new Size(24, 16)
            };

            var uaFlag = CreateLanguageFlagImage("UA");
            var enFlag = CreateLanguageFlagImage("EN");

            menuLanguage = new ToolStripMenuItem("Мова");
            menuLanguageUa = new ToolStripMenuItem("UA") { Image = uaFlag };
            menuLanguageEn = new ToolStripMenuItem("EN") { Image = enFlag };
            menuLanguageUa.Click += (s, e) => SetLanguage("UA");
            menuLanguageEn.Click += (s, e) => SetLanguage("EN");
            menuLanguage.DropDownItems.Add(menuLanguageUa);
            menuLanguage.DropDownItems.Add(menuLanguageEn);
            mainMenu.Items.Add(menuLanguage);

            this.Controls.Add(mainMenu);
            SyncLanguageMenuChecks();

            // ===== TOP STATUS PANEL =====
            pnlTopContainer = new Panel
            {
                Location = new Point(10, 34),
                Size = new Size(980, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Left panel - Service Status
            pnlServiceStatusContainer = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(500, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlTopContainer.Controls.Add(pnlServiceStatusContainer);

            // Right panel - Configuration
            pnlConfigurationContainer = new Panel
            {
                Location = new Point(515, 0),
                Size = new Size(465, 120),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlTopContainer.Controls.Add(pnlConfigurationContainer);

            lblServiceStatusHeader = new Label
            {
                Text = Lang.Get("SERVICE_STATUS_HEADER"),
                Location = new Point(10, 10),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnlServiceStatusContainer.Controls.Add(lblServiceStatusHeader);

            lblServiceStatus = new Label
            {
                Location = new Point(10, 35),
                Size = new Size(400, 25),
                Font = new Font("Segoe UI", 11),
                Text = Lang.Get("SERVICE_CHECKING"),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            pnlServiceStatusContainer.Controls.Add(lblServiceStatus);

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
            pnlServiceStatusContainer.Controls.Add(btnStartService);

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
            pnlServiceStatusContainer.Controls.Add(btnStopService);

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
            pnlServiceStatusContainer.Controls.Add(btnRefresh);

            // Config on right side of top panel
            lblConfigurationHeader = new Label
            {
                Text = Lang.Get("CONFIG_HEADER"),
                Location = new Point(10, 10),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnlConfigurationContainer.Controls.Add(lblConfigurationHeader);

            lblConfig = new Label
            {
                Location = new Point(10, 35),
                Size = new Size(450, 75),
                Font = new Font("Consolas", 8),
                Text = "Loading...",
                AutoSize = false,
                BackColor = Color.FromArgb(250, 250, 250)
            };
            pnlConfigurationContainer.Controls.Add(lblConfig);

            this.Controls.Add(pnlTopContainer);

            // ===== TAB CONTROL =====
            tabControl = new TabControl
            {
                Location = new Point(10, 164),
                Size = new Size(980, 560),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
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

            // Tab 6: Alerts (Telegram)
            var tabAlerts = new TabPage(Lang.Get("TAB_ALERTS"));
            CreateAlertsTab(tabAlerts);
            tabControl.TabPages.Add(tabAlerts);

            // Tab 7: Message Settings
            var tabMessageSettings = new TabPage(Lang.Get("TAB_MESSAGE_SETTINGS"));
            CreateMessageSettingsTab(tabMessageSettings);
            tabControl.TabPages.Add(tabMessageSettings);

            this.Controls.Add(tabControl);

            this.Resize += (s, e) => ApplyMainLayout();
            ApplyMainLayout();

            // ===== BACKGROUND SHIELD IMAGE (ADD LAST SO IT STAYS BEHIND) =====
            AddBackgroundShield();
        }

        private void ApplyMainLayout()
        {
            if (mainMenu == null || pnlTopContainer == null || pnlServiceStatusContainer == null || pnlConfigurationContainer == null || tabControl == null)
                return;

            int margin = 10;
            int topY = mainMenu.Bottom + 6;
            int topHeight = 120;
            int totalWidth = Math.Max(500, this.ClientSize.Width - margin * 2);
            int gap = 15;

            pnlTopContainer.Location = new Point(margin, topY);
            pnlTopContainer.Size = new Size(totalWidth, topHeight);

            int leftWidth = (totalWidth - gap) / 2;
            int rightWidth = totalWidth - gap - leftWidth;

            pnlServiceStatusContainer.Location = new Point(0, 0);
            pnlServiceStatusContainer.Size = new Size(leftWidth, topHeight);

            pnlConfigurationContainer.Location = new Point(leftWidth + gap, 0);
            pnlConfigurationContainer.Size = new Size(rightWidth, topHeight);

            if (lblServiceStatusHeader != null)
                lblServiceStatusHeader.Size = new Size(Math.Max(120, leftWidth - 20), 20);
            if (lblServiceStatus != null)
                lblServiceStatus.Size = new Size(Math.Max(120, leftWidth - 20), 25);
            if (lblConfigurationHeader != null)
                lblConfigurationHeader.Size = new Size(Math.Max(120, rightWidth - 20), 20);
            if (lblConfig != null)
                lblConfig.Size = new Size(Math.Max(120, rightWidth - 20), 75);

            int tabY = pnlTopContainer.Bottom + 10;
            int tabHeight = Math.Max(200, this.ClientSize.Height - tabY - margin);
            tabControl.Location = new Point(margin, tabY);
            tabControl.Size = new Size(totalWidth, tabHeight);
        }

        private Icon? LoadApplicationIcon()
        {
            try
            {
                var candidates = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "app.png"),
                    Path.Combine(Application.StartupPath, "app.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "app.png"),
                    Path.Combine(AppContext.BaseDirectory, "app.ico"),
                    Path.Combine(Application.StartupPath, "app.ico"),
                    Path.Combine(Directory.GetCurrentDirectory(), "app.ico")
                };

                foreach (var iconPath in candidates)
                {
                    if (File.Exists(iconPath))
                    {
                        if (iconPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                        {
                            var pngIcon = CreateIconFromPng(iconPath);
                            if (pngIcon != null)
                            {
                                return pngIcon;
                            }
                        }

                        return new Icon(iconPath);
                    }
                }

                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                return null;
            }
        }

        private Icon? CreateIconFromPng(string filePath)
        {
            try
            {
                using var source = new Bitmap(filePath);
                using var resized = new Bitmap(source, new Size(32, 32));
                var iconHandle = resized.GetHicon();

                try
                {
                    using var temporaryIcon = Icon.FromHandle(iconHandle);
                    return (Icon)temporaryIcon.Clone();
                }
                finally
                {
                    DestroyIcon(iconHandle);
                }
            }
            catch
            {
                return null;
            }
        }

        private void AddBackgroundShield()
        {
            try
            {
                var resourcePath = Path.Combine(AppContext.BaseDirectory, "Resources", "shield_bg.png");
                if (!File.Exists(resourcePath))
                    return;

                var backgroundImage = Image.FromFile(resourcePath);
                
                // Calculate centered position and scaled size
                int imageSize = 400; // Max size for the shield
                int x = (this.ClientSize.Width - imageSize) / 2;
                int y = (this.ClientSize.Height - imageSize) / 2 + 50; // Offset down slightly

                var picShield = new TransparentPictureBox
                {
                    Image = backgroundImage,
                    Location = new Point(x, y),
                    Size = new Size(imageSize, imageSize),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };

                // Add as first control (behind everything)
                this.Controls.Add(picShield);
                picShield.SendToBack();

                // Reposition on resize
                this.Resize += (s, e) =>
                {
                    int newX = (this.ClientSize.Width - imageSize) / 2;
                    int newY = (this.ClientSize.Height - imageSize) / 2 + 50;
                    picShield.Location = new Point(newX, newY);
                };
            }
            catch
            {
                // Silently fail if image not found
            }
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
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 20, 20, 50), AutoScroll = true };
            pnl.HorizontalScroll.Enabled = false;
            pnl.HorizontalScroll.Visible = false;

            var lbl = new Label
            {
                Text = Lang.Get("SECTION_BLOCKED_IPS_FIREWALL"),
                AutoSize = true,
                Location = new Point(10, 10),
                MaximumSize = new Size(950, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lbl);

            lstBannedIPs = new ListBox
            {
                Location = new Point(10, 45),
                Size = new Size(950, 330),
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            lstBannedIPs.DoubleClick += LstBannedIPs_DoubleClick;
            lstBannedIPs.MouseDown += LstBannedIPs_MouseDown;
            pnl.Controls.Add(lstBannedIPs);

            var lblUnblock = new Label
            {
                Text = Lang.Get("SECTION_IP_TO_UNBLOCK"),
                Location = new Point(10, 385),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            pnl.Controls.Add(lblUnblock);

            txtIPToUnblock = new TextBox
            {
                Location = new Point(10, 410),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9),
                Padding = new Padding(5)
            };
            AttachIpInputMask(txtIPToUnblock);
            pnl.Controls.Add(txtIPToUnblock);

            btnUnblockIP = new Button
            {
                Text = "🔓 " + Lang.Get("BTN_UNBLOCK_IP"),
                Location = new Point(220, 410),
                Size = new Size(160, 25),
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
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 20, 20, 50), AutoScroll = true };
            pnl.HorizontalScroll.Enabled = false;
            pnl.HorizontalScroll.Visible = false;

            var lbl = new Label
            {
                Text = Lang.Get("SECTION_WHITE_LIST"),
                AutoSize = true,
                Location = new Point(10, 10),
                MaximumSize = new Size(950, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lbl);

            lstWhiteList = new ListBox
            {
                Location = new Point(10, 45),
                Size = new Size(950, 330),
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(250, 250, 250)
            };
            
            // Context menu for whitelist
            var contextMenu = new ContextMenuStrip();
            var deleteItem = new ToolStripMenuItem("🗑️ Видалити", null, (s, e) => 
            {
                if (lstWhiteList.SelectedIndex >= 0)
                {
                    string ip = lstWhiteList.SelectedItem.ToString();
                    if (ip != Lang.Get("MSG_NO_WHITELISTED_IPS"))
                    {
                        txtNewWhiteIP.Text = ip;
                        BtnRemoveWhiteIP_Click(null, null);
                    }
                }
            });
            contextMenu.Items.Add(deleteItem);
            lstWhiteList.ContextMenuStrip = contextMenu;
            
            pnl.Controls.Add(lstWhiteList);

            var lblAdd = new Label
            {
                Text = Lang.Get("SECTION_ADD_WHITELIST_IP"),
                Location = new Point(10, 385),
                AutoSize = true,
                Font = new Font("Segoe UI", 9)
            };
            pnl.Controls.Add(lblAdd);

            txtNewWhiteIP = new TextBox
            {
                Location = new Point(10, 410),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "192.168.1.100"
            };
            AttachIpInputMask(txtNewWhiteIP);
            pnl.Controls.Add(txtNewWhiteIP);

            btnAddWhiteIP = new Button
            {
                Text = Lang.Get("BTN_ADD_WITH_PLUS"),
                Location = new Point(220, 410),
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
                Text = Lang.Get("BTN_REMOVE_WITH_X"),
                Location = new Point(310, 410),
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
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 20, 20, 50), AutoScroll = true };
            pnl.HorizontalScroll.Enabled = false;
            pnl.HorizontalScroll.Visible = false;

            var lblTitle = new Label
            {
                Text = Lang.Get("SECTION_MANUAL_BLOCK"),
                AutoSize = true,
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(244, 67, 54)
            };
            pnl.Controls.Add(lblTitle);

            var lblIP = new Label
            {
                Text = Lang.Get("LABEL_IP_ADDRESS"),
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
            AttachIpInputMask(txtIPToBlock);
            pnl.Controls.Add(txtIPToBlock);

            var lblMinutes = new Label
            {
                Text = Lang.Get("LABEL_BLOCK_DURATION_FULL"),
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
                PlaceholderText = "60 / 12h / 7d / 2w"
            };
            pnl.Controls.Add(txtBlockMinutes);

            btnManualBlock = new Button
            {
                Text = "🔒 " + Lang.Get("BTN_BLOCK_THIS_IP"),
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
                Text = Lang.Get("LABEL_BLOCK_STATUS_PLACEHOLDER"),
                AutoSize = false
            };
            pnl.Controls.Add(lblBlockStatus);

            tab.Controls.Add(pnl);
        }

        private void AttachIpInputMask(TextBox textBox)
        {
            textBox.KeyPress += IpTextBox_KeyPress;
            textBox.TextChanged += IpTextBox_TextChanged;
        }

        private void IpTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar) || e.KeyChar == '.')
                return;

            e.Handled = true;
        }

        private void IpTextBox_TextChanged(object sender, EventArgs e)
        {
            if (suppressIpMaskUpdate || sender is not TextBox textBox)
                return;

            string original = textBox.Text;
            string normalized = NormalizePartialIpv4(original);
            if (normalized == original)
                return;

            int caret = textBox.SelectionStart;
            string leftPart = NormalizePartialIpv4(original.Substring(0, Math.Min(caret, original.Length)));

            suppressIpMaskUpdate = true;
            textBox.Text = normalized;
            textBox.SelectionStart = Math.Min(leftPart.Length, textBox.Text.Length);
            suppressIpMaskUpdate = false;
        }

        private string NormalizePartialIpv4(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var output = new StringBuilder(input.Length);
            int dots = 0;
            int octetDigits = 0;
            int octetValue = 0;
            bool hasDigitInCurrentOctet = false;

            foreach (char ch in input)
            {
                if (char.IsDigit(ch))
                {
                    if (dots > 3 || octetDigits >= 3)
                        continue;

                    int digit = ch - '0';
                    int candidate = octetDigits == 0 ? digit : (octetValue * 10) + digit;
                    if (candidate > 255)
                        continue;

                    output.Append(ch);
                    octetValue = candidate;
                    octetDigits++;
                    hasDigitInCurrentOctet = true;
                    continue;
                }

                if (ch == '.')
                {
                    if (!hasDigitInCurrentOctet || dots >= 3)
                        continue;

                    output.Append(ch);
                    dots++;
                    octetDigits = 0;
                    octetValue = 0;
                    hasDigitInCurrentOctet = false;
                }
            }

            return output.ToString();
        }

        private void CreateSettingsTab(TabPage tab)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20, 20, 20, 50), AutoScroll = true };
            pnl.HorizontalScroll.Enabled = false;
            pnl.HorizontalScroll.Visible = false;

            var lblTitle = new Label
            {
                Text = Lang.Get("SECTION_SERVICE_CONFIG"),
                AutoSize = true,
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnl.Controls.Add(lblTitle);

            // RDP Port
            var lblPort = new Label
            {
                Text = Lang.Get("LABEL_RDP_PORT"),
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
                Text = Lang.Get("LABEL_BLOCK_LEVELS_TABLE") + " (60 / 12h / 7d / 2w)",
                AutoSize = true,
                Location = new Point(10, 120),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lblLevels);

            dgvBlockLevels = new DataGridView
            {
                Location = new Point(10, 145),
                Size = new Size(400, 200),
                Font = new Font("Segoe UI", 9),
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false
            };
            dgvBlockLevels.Columns.Add("Attempts", Lang.Get("COL_ATTEMPTS"));
            dgvBlockLevels.Columns.Add("BlockMinutes", Lang.Get("COL_BLOCK_MINUTES"));

            for (int i = 0; i < dgvBlockLevels.Columns.Count; i++)
            {
                dgvBlockLevels.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            pnl.Controls.Add(dgvBlockLevels);

            btnAddLevel = new Button
            {
                Text = Lang.Get("BTN_ADD_LEVEL_WITH_PLUS"),
                Location = new Point(10, 355),
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
                Text = Lang.Get("BTN_REMOVE_WITH_X"),
                Location = new Point(120, 355),
                Size = new Size(100, 28),
                BackColor = Color.FromArgb(244, 67, 54),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnRemoveLevel.FlatAppearance.BorderSize = 0;
            btnRemoveLevel.Click += BtnRemoveLevel_Click;
            pnl.Controls.Add(btnRemoveLevel);

            var lblAntiBrute = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_SECTION"),
                AutoSize = true,
                Location = new Point(440, 50),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnl.Controls.Add(lblAntiBrute);

            chkAntiBruteEnabled = new CheckBox
            {
                Text = Lang.Get("ANTI_BRUTE_ENABLED"),
                AutoSize = true,
                Location = new Point(440, 78),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(chkAntiBruteEnabled);

            var lblSpray = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_SPRAY"),
                AutoSize = true,
                Location = new Point(440, 110),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lblSpray);

            chkSprayEnabled = new CheckBox
            {
                Text = Lang.Get("ANTI_BRUTE_ENABLED_SHORT"),
                AutoSize = true,
                Location = new Point(440, 132)
            };
            pnl.Controls.Add(chkSprayEnabled);

            var lblSprayWindow = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_WINDOW_MIN"),
                AutoSize = true,
                Location = new Point(440, 158)
            };
            pnl.Controls.Add(lblSprayWindow);

            txtSprayWindowMinutes = new TextBox
            {
                Location = new Point(690, 154),
                Size = new Size(90, 24),
                Text = "10"
            };
            pnl.Controls.Add(txtSprayWindowMinutes);

            var lblSprayThreshold = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_UNIQUE_IPS"),
                AutoSize = true,
                Location = new Point(440, 186)
            };
            pnl.Controls.Add(lblSprayThreshold);

            txtSprayUniqueIpsThreshold = new TextBox
            {
                Location = new Point(690, 182),
                Size = new Size(90, 24),
                Text = "4"
            };
            pnl.Controls.Add(txtSprayUniqueIpsThreshold);

            var lblSprayBlock = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_BLOCK_MIN"),
                AutoSize = true,
                Location = new Point(440, 214)
            };
            pnl.Controls.Add(lblSprayBlock);

            txtSprayBlockMinutes = new TextBox
            {
                Location = new Point(690, 210),
                Size = new Size(90, 24),
                Text = "240"
            };
            pnl.Controls.Add(txtSprayBlockMinutes);

            var lblRecurrence = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_RECURRENCE"),
                AutoSize = true,
                Location = new Point(440, 246),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lblRecurrence);

            chkRecurrenceEnabled = new CheckBox
            {
                Text = Lang.Get("ANTI_BRUTE_ENABLED_SHORT"),
                AutoSize = true,
                Location = new Point(440, 268)
            };
            pnl.Controls.Add(chkRecurrenceEnabled);

            var lblRecurrenceLookback = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_LOOKBACK_H"),
                AutoSize = true,
                Location = new Point(440, 294)
            };
            pnl.Controls.Add(lblRecurrenceLookback);

            txtRecurrenceLookbackHours = new TextBox
            {
                Location = new Point(690, 290),
                Size = new Size(90, 24),
                Text = "24"
            };
            pnl.Controls.Add(txtRecurrenceLookbackHours);

            var lblRecurrenceStep = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_STEP"),
                AutoSize = true,
                Location = new Point(440, 322)
            };
            pnl.Controls.Add(lblRecurrenceStep);

            txtRecurrenceStepMultiplier = new TextBox
            {
                Location = new Point(690, 318),
                Size = new Size(90, 24),
                Text = "0.5"
            };
            pnl.Controls.Add(txtRecurrenceStepMultiplier);

            var lblRecurrenceMax = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_MAX"),
                AutoSize = true,
                Location = new Point(440, 350)
            };
            pnl.Controls.Add(lblRecurrenceMax);

            txtRecurrenceMaxMultiplier = new TextBox
            {
                Location = new Point(690, 346),
                Size = new Size(90, 24),
                Text = "4.0"
            };
            pnl.Controls.Add(txtRecurrenceMaxMultiplier);

            var lblSubnet = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_SUBNET"),
                AutoSize = true,
                Location = new Point(440, 382),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lblSubnet);

            chkSubnetEnabled = new CheckBox
            {
                Text = Lang.Get("ANTI_BRUTE_ENABLED_SHORT"),
                AutoSize = true,
                Location = new Point(440, 404)
            };
            pnl.Controls.Add(chkSubnetEnabled);

            var lblSubnetWindow = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_WINDOW_MIN"),
                AutoSize = true,
                Location = new Point(440, 430)
            };
            pnl.Controls.Add(lblSubnetWindow);

            txtSubnetWindowMinutes = new TextBox
            {
                Location = new Point(690, 426),
                Size = new Size(90, 24),
                Text = "30"
            };
            pnl.Controls.Add(txtSubnetWindowMinutes);

            var lblSubnetThreshold = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_UNIQUE_IPS"),
                AutoSize = true,
                Location = new Point(440, 458)
            };
            pnl.Controls.Add(lblSubnetThreshold);

            txtSubnetUniqueIpsThreshold = new TextBox
            {
                Location = new Point(690, 454),
                Size = new Size(90, 24),
                Text = "3"
            };
            pnl.Controls.Add(txtSubnetUniqueIpsThreshold);

            var lblSubnetBlock = new Label
            {
                Text = Lang.Get("ANTI_BRUTE_BLOCK_MIN"),
                AutoSize = true,
                Location = new Point(440, 486)
            };
            pnl.Controls.Add(lblSubnetBlock);

            txtSubnetBlockMinutes = new TextBox
            {
                Location = new Point(690, 482),
                Size = new Size(90, 24),
                Text = "240"
            };
            pnl.Controls.Add(txtSubnetBlockMinutes);

            btnSaveConfig = new Button
            {
                Text = "💾 " + Lang.Get("BTN_SAVE_CONFIGURATION"),
                Location = new Point(10, 540),
                Size = new Size(900, 42),
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

        private void CreateAlertsTab(TabPage tab)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(20, 20, 20, 50),
                AutoScroll = true
            };
            pnl.HorizontalScroll.Enabled = false;
            pnl.HorizontalScroll.Visible = false;

            int yPos = 10;

            // Section header
            var lblHeader = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(900, 30),
                Text = Lang.Get("TELEGRAM_SECTION_HEADER"),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnl.Controls.Add(lblHeader);
            yPos += 40;

            // Enable checkbox
            chkTelegramEnabled = new CheckBox
            {
                Location = new Point(10, yPos),
                Size = new Size(400, 25),
                Text = Lang.Get("TELEGRAM_ENABLE"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            chkTelegramEnabled.CheckedChanged += ChkTelegramEnabled_CheckedChanged;
            pnl.Controls.Add(chkTelegramEnabled);
            yPos += 35;

            // Bot Token label
            var lblBotToken = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(150, 25),
                Text = Lang.Get("TELEGRAM_BOT_TOKEN"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lblBotToken);
            yPos += 25;

            // Bot Token textbox
            txtTelegramBotToken = new TextBox
            {
                Location = new Point(10, yPos),
                Size = new Size(850, 25),
                Font = new Font("Consolas", 9),
                PlaceholderText = Lang.Get("TELEGRAM_BOT_TOKEN_PLACEHOLDER")
            };
            pnl.Controls.Add(txtTelegramBotToken);
            yPos += 35;

            // Chat ID label
            var lblChatId = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(150, 25),
                Text = Lang.Get("TELEGRAM_CHAT_ID"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            pnl.Controls.Add(lblChatId);
            yPos += 25;

            // Chat ID textbox
            txtTelegramChatId = new TextBox
            {
                Location = new Point(10, yPos),
                Size = new Size(300, 25),
                Font = new Font("Consolas", 9),
                PlaceholderText = Lang.Get("TELEGRAM_CHAT_ID_PLACEHOLDER")
            };
            pnl.Controls.Add(txtTelegramChatId);
            yPos += 40;

            // --- MESSAGE TEMPLATES SECTION ---
            var lblTemplatesHeader = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(900, 30),
                Text = Lang.Get("TELEGRAM_MESSAGE_TEMPLATES"),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnl.Controls.Add(lblTemplatesHeader);
            yPos += 35;

            // Placeholders info
            var lblPlaceholders = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(900, 20),
                Text = Lang.Get("TELEGRAM_PLACEHOLDERS"),
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            pnl.Controls.Add(lblPlaceholders);
            yPos += 30;

            // Get current config to determine how many levels exist
            ServiceConfig? config = null;
            try 
            {
                string configPath = Path.Combine(LOG_DIR, "config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath, Encoding.UTF8);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    config = JsonSerializer.Deserialize<ServiceConfig>(json, options);
                }
            }
            catch { }
            int levelCount = config?.Levels?.Count ?? 3;
            
            // Create template editors for each level
            txtMessageTemplates.Clear();
            for (int i = 0; i < levelCount; i++)
            {
                string key = $"level{i + 1}";
                
                var lblLevel = new Label
                {
                    Location = new Point(10, yPos),
                    Size = new Size(900, 22),
                    Text = $"{Lang.Get("TELEGRAM_TEMPLATE_LEVEL")} {i + 1}:",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = Color.FromArgb(70, 70, 70)
                };
                pnl.Controls.Add(lblLevel);
                yPos += 25;

                var txtTemplate = new TextBox
                {
                    Location = new Point(10, yPos),
                    Size = new Size(900, 60),
                    Font = new Font("Consolas", 8),
                    Multiline = true,
                    ScrollBars = ScrollBars.Vertical
                };
                txtMessageTemplates[key] = txtTemplate;
                pnl.Controls.Add(txtTemplate);
                yPos += 70;
            }

            // Default template
            var lblDefault = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(900, 22),
                Text = Lang.Get("TELEGRAM_TEMPLATE_DEFAULT"),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 70, 70)
            };
            pnl.Controls.Add(lblDefault);
            yPos += 25;

            var txtDefaultTemplate = new TextBox
            {
                Location = new Point(10, yPos),
                Size = new Size(900, 60),
                Font = new Font("Consolas", 8),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            txtMessageTemplates["default"] = txtDefaultTemplate;
            pnl.Controls.Add(txtDefaultTemplate);
            yPos += 75;

            // Buttons panel
            var btnPanel = new FlowLayoutPanel
            {
                Location = new Point(10, yPos),
                Size = new Size(900, 40),
                FlowDirection = FlowDirection.LeftToRight
            };

            btnTestTelegram = new Button
            {
                Size = new Size(180, 35),
                Text = Lang.Get("TELEGRAM_TEST_BUTTON"),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnTestTelegram.FlatAppearance.BorderSize = 0;
            btnTestTelegram.Click += BtnTestTelegram_Click;
            btnPanel.Controls.Add(btnTestTelegram);

            btnSaveTelegram = new Button
            {
                Size = new Size(180, 35),
                Text = Lang.Get("TELEGRAM_SAVE_BUTTON"),
                BackColor = Color.FromArgb(76, 175, 80),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Margin = new Padding(10, 0, 0, 0)
            };
            btnSaveTelegram.FlatAppearance.BorderSize = 0;
            btnSaveTelegram.Click += BtnSaveTelegram_Click;
            btnPanel.Controls.Add(btnSaveTelegram);

            pnl.Controls.Add(btnPanel);
            yPos += 50;

            // Status label
            lblTelegramStatus = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(900, 25),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = Color.Gray
            };
            pnl.Controls.Add(lblTelegramStatus);
            yPos += 40;

            // Help section
            var lblHelpTitle = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(900, 25),
                Text = Lang.Get("TELEGRAM_HELP_TITLE"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            pnl.Controls.Add(lblHelpTitle);
            yPos += 30;

            for (int i = 1; i <= 5; i++)
            {
                var step = new Label
                {
                    Location = new Point(20, yPos),
                    Size = new Size(880, 20),
                    Text = Lang.Get($"TELEGRAM_HELP_STEP{i}"),
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(80, 80, 80)
                };
                pnl.Controls.Add(step);
                yPos += 25;
            }

            tab.Controls.Add(pnl);

            // Load initial Telegram config
            LoadTelegramConfig();
        }

        private void ChkTelegramEnabled_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = chkTelegramEnabled.Checked;
            txtTelegramBotToken.Enabled = enabled;
            txtTelegramChatId.Enabled = enabled;
            btnTestTelegram.Enabled = enabled;
            lblTelegramStatus.Text = enabled ? Lang.Get("TELEGRAM_STATUS_ENABLED") : Lang.Get("TELEGRAM_STATUS_DISABLED");
        }

        private async void BtnTestTelegram_Click(object sender, EventArgs e)
        {
            try
            {
                string botToken = txtTelegramBotToken.Text.Trim();
                string chatId = txtTelegramChatId.Text.Trim();

                if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
                {
                    lblTelegramStatus.Text = "Please fill Bot Token and Chat ID";
                    lblTelegramStatus.ForeColor = Color.Red;
                    return;
                }

                string message = "✅ *RDP Security Service*\n\nTelegram notifications are working!\n\n_This is a test message._";

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
                    var content = new System.Net.Http.FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("chat_id", chatId),
                        new KeyValuePair<string, string>("text", message),
                        new KeyValuePair<string, string>("parse_mode", "Markdown")
                    });

                    var response = await client.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        lblTelegramStatus.Text = Lang.Get("TELEGRAM_TEST_SUCCESS");
                        lblTelegramStatus.ForeColor = Color.Green;
                    }
                    else
                    {
                        lblTelegramStatus.Text = $"{Lang.Get("TELEGRAM_TEST_FAIL")} HTTP {response.StatusCode}";
                        lblTelegramStatus.ForeColor = Color.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                lblTelegramStatus.Text = $"{Lang.Get("TELEGRAM_TEST_FAIL")} {ex.Message}";
                lblTelegramStatus.ForeColor = Color.Red;
            }
        }

        private void BtnSaveTelegram_Click(object sender, EventArgs e)
        {
            try
            {
                string configPath = Path.Combine(LOG_DIR, "config.json");
                if (!File.Exists(configPath))
                {
                    lblTelegramStatus.Text = Lang.Get("TELEGRAM_SAVE_FAIL");
                    lblTelegramStatus.ForeColor = Color.Red;
                    return;
                }

                var json = File.ReadAllText(configPath, Encoding.UTF8);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<ServiceConfig>(json, options);
                
                if (config == null)
                {
                    lblTelegramStatus.Text = Lang.Get("TELEGRAM_SAVE_FAIL");
                    lblTelegramStatus.ForeColor = Color.Red;
                    return;
                }

                // Collect message templates
                var templates = new Dictionary<string, string>();
                foreach (var kvp in txtMessageTemplates)
                {
                    templates[kvp.Key] = kvp.Value.Text.Trim();
                }

                config.Telegram = new TelegramConfig
                {
                    Enabled = chkTelegramEnabled.Checked,
                    BotToken = txtTelegramBotToken.Text.Trim(),
                    ChatId = txtTelegramChatId.Text.Trim(),
                    MessageTemplates = templates
                };

                var saveOptions = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var saveJson = JsonSerializer.Serialize(config, saveOptions);
                File.WriteAllText(configPath, saveJson, Encoding.UTF8);

                lblTelegramStatus.Text = Lang.Get("TELEGRAM_SAVE_SUCCESS");
                lblTelegramStatus.ForeColor = Color.Green;
                LoadConfiguration(); // Refresh display
            }
            catch (Exception ex)
            {
                lblTelegramStatus.Text = $"{Lang.Get("TELEGRAM_SAVE_FAIL")} {ex.Message}";
                lblTelegramStatus.ForeColor = Color.Red;
            }
        }

        private void LoadTelegramConfig()
        {
            try
            {
                ServiceConfig? config = null;
                string configPath = Path.Combine(LOG_DIR, "config.json");
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath, Encoding.UTF8);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    config = JsonSerializer.Deserialize<ServiceConfig>(json, options);
                }
                
                if (config?.Telegram != null)
                {
                    chkTelegramEnabled.Checked = config.Telegram.Enabled;
                    txtTelegramBotToken.Text = config.Telegram.BotToken ?? "";
                    txtTelegramChatId.Text = config.Telegram.ChatId ?? "";

                    // Load message templates
                    if (config.Telegram.MessageTemplates != null)
                    {
                        foreach (var kvp in txtMessageTemplates)
                        {
                            if (config.Telegram.MessageTemplates.ContainsKey(kvp.Key))
                            {
                                kvp.Value.Text = config.Telegram.MessageTemplates[kvp.Key];
                            }
                            else
                            {
                                // Set default templates if not found
                                kvp.Value.Text = GetDefaultTemplate(kvp.Key);
                            }
                        }
                    }
                    else
                    {
                        // No templates in config, use defaults
                        foreach (var kvp in txtMessageTemplates)
                        {
                            kvp.Value.Text = GetDefaultTemplate(kvp.Key);
                        }
                    }
                }
                else
                {
                    chkTelegramEnabled.Checked = false;
                    txtTelegramBotToken.Text = "";
                    txtTelegramChatId.Text = "";
                    
                    // Load default templates
                    foreach (var kvp in txtMessageTemplates)
                    {
                        kvp.Value.Text = GetDefaultTemplate(kvp.Key);
                    }
                }
            }
            catch { }
        }

        private string GetDefaultTemplate(string key)
        {
            switch (key)
            {
                case "level1":
                    return "🟡 RDP ALERT - Рівень 1\n\nЗаблоковано IP: {ip}\nСпроб входу: {attempts}\nБлокування: {duration} хв";
                case "level2":
                    return "🟠 RDP ALERT - Рівень 2\n\nЗаблоковано IP: {ip}\nСпроб входу: {attempts}\nБлокування: {duration} хв";
                case "level3":
                    return "🔴 RDP ALERT - Рівень 3\n\nЗаблоковано IP: {ip}\nСпроб входу: {attempts}\nБлокування: {duration} хв";
                case "default":
                    return "🚨 RDP Security Alert\n\nЗаблоковано IP: {ip}\nСпроб атаки: {attempts}\nБлокування: {duration} хв";
                default:
                    return $"🔴 RDP Alert {key.ToUpper()}\n\nIP: {{ip}}\nСпроб: {{attempts}}\nБлокування: {{duration}} хв";
            }
        }

        private void SendSimpleTelegramMessage(string message)
        {
            try
            {
                string configPath = Path.Combine(LOG_DIR, "config.json");
                if (!File.Exists(configPath))
                    return;

                var json = File.ReadAllText(configPath, Encoding.UTF8);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var config = JsonSerializer.Deserialize<ServiceConfig>(json, options);

                var telegram = config?.Telegram;
                if (telegram == null || !telegram.Enabled || string.IsNullOrWhiteSpace(telegram.BotToken) || string.IsNullOrWhiteSpace(telegram.ChatId))
                    return;
                
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var url = $"https://api.telegram.org/bot{telegram.BotToken}/sendMessage";
                    var content = new System.Net.Http.FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("chat_id", telegram.ChatId),
                        new KeyValuePair<string, string>("text", message)
                    });

                    var response = client.PostAsync(url, content).GetAwaiter().GetResult();
                    if (!response.IsSuccessStatusCode)
                        AppendLog($"[TELEGRAM] Startup notification failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[TELEGRAM] Startup notification error: {ex.Message}");
            }
        }

        private void CreateMessageSettingsTab(TabPage tab)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(20, 20, 20, 50),
                AutoScroll = true
            };
            pnl.HorizontalScroll.Enabled = false;
            pnl.HorizontalScroll.Visible = false;

            int yPos = 10;

            // Section header
            var lblHeader = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(900, 30),
                Text = Lang.Get("MSG_SETTINGS_HEADER"),
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 102, 204)
            };
            pnl.Controls.Add(lblHeader);
            yPos += 45;

            // Monitor section
            var lblMonitor = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(300, 25),
                Text = Lang.Get("MSG_SETTINGS_MONITOR"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            pnl.Controls.Add(lblMonitor);
            yPos += 30;

            chkNotifyMonitorStart = new CheckBox
            {
                Location = new Point(30, yPos),
                Size = new Size(500, 25),
                Text = Lang.Get("MSG_SETTINGS_MONITOR_START"),
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            pnl.Controls.Add(chkNotifyMonitorStart);
            yPos += 30;

            chkNotifyMonitorClose = new CheckBox
            {
                Location = new Point(30, yPos),
                Size = new Size(500, 25),
                Text = Lang.Get("MSG_SETTINGS_MONITOR_CLOSE"),
                Font = new Font("Segoe UI", 9),
                Checked = false
            };
            pnl.Controls.Add(chkNotifyMonitorClose);
            yPos += 45;

            // Service section
            var lblService = new Label
            {
                Location = new Point(10, yPos),
                Size = new Size(300, 25),
                Text = Lang.Get("MSG_SETTINGS_SERVICE"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50)
            };
            pnl.Controls.Add(lblService);
            yPos += 30;

            chkNotifyServiceStart = new CheckBox
            {
                Location = new Point(30, yPos),
                Size = new Size(500, 25),
                Text = Lang.Get("MSG_SETTINGS_SERVICE_START"),
                Font = new Font("Segoe UI", 9),
                Checked = true
            };
            pnl.Controls.Add(chkNotifyServiceStart);
            yPos += 30;

            chkNotifyServiceStop = new CheckBox
            {
                Location = new Point(30, yPos),
                Size = new Size(500, 25),
                Text = Lang.Get("MSG_SETTINGS_SERVICE_STOP"),
                Font = new Font("Segoe UI", 9),
                Checked = true
            };
            pnl.Controls.Add(chkNotifyServiceStop);
            yPos += 45;

            // Configuration section
            chkNotifyConfigSave = new CheckBox
            {
                Location = new Point(10, yPos),
                Size = new Size(500, 25),
                Text = Lang.Get("MSG_SETTINGS_CONFIG_SAVE"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 50, 50),
                Checked = false
            };
            pnl.Controls.Add(chkNotifyConfigSave);
            yPos += 50;

            // Save button
            btnSaveMessageSettings = new Button
            {
                Location = new Point(10, yPos),
                Size = new Size(200, 35),
                Text = Lang.Get("MSG_SETTINGS_SAVE_BTN"),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSaveMessageSettings.FlatAppearance.BorderSize = 0;
            btnSaveMessageSettings.Click += BtnSaveMessageSettings_Click;
            pnl.Controls.Add(btnSaveMessageSettings);

            tab.Controls.Add(pnl);

            // Load initial settings
            LoadMessageNotificationSettings();
        }

        private void BtnSaveMessageSettings_Click(object sender, EventArgs e)
        {
            try
            {
                var settings = new MessageNotificationSettings
                {
                    MonitorStart = chkNotifyMonitorStart.Checked,
                    MonitorClose = chkNotifyMonitorClose.Checked,
                    ServiceStart = chkNotifyServiceStart.Checked,
                    ServiceStop = chkNotifyServiceStop.Checked,
                    ConfigSave = chkNotifyConfigSave.Checked
                };

                string settingsPath = Path.Combine(LOG_DIR, "monitor-notifications.json");
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(settingsPath, json, Encoding.UTF8);

                bool isUa = string.Equals(Program.CurrentLanguage, "UA", StringComparison.OrdinalIgnoreCase);
                string message = isUa ? "Налаштування збережено!" : "Settings saved!";
                MessageBox.Show(message, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppendLog("[MONITOR] Message notification settings saved");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                AppendLog($"[ERROR] Failed to save message notification settings: {ex.Message}");
            }
        }

        private void LoadMessageNotificationSettings()
        {
            try
            {
                string settingsPath = Path.Combine(LOG_DIR, "monitor-notifications.json");
                if (File.Exists(settingsPath))
                {
                    var json = File.ReadAllText(settingsPath, Encoding.UTF8);
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var settings = JsonSerializer.Deserialize<MessageNotificationSettings>(json, options);

                    if (settings != null)
                    {
                        chkNotifyMonitorStart.Checked = settings.MonitorStart;
                        chkNotifyMonitorClose.Checked = settings.MonitorClose;
                        chkNotifyServiceStart.Checked = settings.ServiceStart;
                        chkNotifyServiceStop.Checked = settings.ServiceStop;
                        chkNotifyConfigSave.Checked = settings.ConfigSave;
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[WARN] Failed to load message notification settings: {ex.Message}");
            }
        }

        private bool ShouldNotify(string eventType)
        {
            try
            {
                string settingsPath = Path.Combine(LOG_DIR, "monitor-notifications.json");
                if (!File.Exists(settingsPath))
                    return false; // Default: don't send

                var json = File.ReadAllText(settingsPath, Encoding.UTF8);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var settings = JsonSerializer.Deserialize<MessageNotificationSettings>(json, options);

                if (settings == null)
                    return false;

                return eventType switch
                {
                    "MonitorStart" => settings.MonitorStart,
                    "MonitorClose" => settings.MonitorClose,
                    "ServiceStart" => settings.ServiceStart,
                    "ServiceStop" => settings.ServiceStop,
                    "ConfigSave" => settings.ConfigSave,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
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

        private bool IsServiceAccessDenied(Exception exception)
        {
            if (exception is UnauthorizedAccessException)
                return true;

            var details = exception.ToString();
            return details.IndexOf("Access is denied", StringComparison.OrdinalIgnoreCase) >= 0
                || details.IndexOf("Отказано в доступе", StringComparison.OrdinalIgnoreCase) >= 0
                || details.IndexOf("Cannot open", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool TryStopServiceElevated()
        {
            try
            {
                var arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Stop-Service -Name '{SERVICE_NAME}' -Force -ErrorAction Stop\"";
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    return false;

                process.WaitForExit(15000);

                if (process.ExitCode == 0)
                {
                    AppendLog("[MONITOR] Service stopped with administrator rights");
                    return true;
                }

                AppendLog($"[ERROR] Elevated stop failed with exit code: {process.ExitCode}");
                return false;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                AppendLog("[MONITOR] Stop service with administrator rights was canceled");
                return false;
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Elevated stop failed: {ex.Message}");
                return false;
            }
        }

        private bool TryStartServiceElevated()
        {
            try
            {
                var arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"Start-Service -Name '{SERVICE_NAME}' -ErrorAction Stop\"";
                var processInfo = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                    return false;

                process.WaitForExit(15000);

                if (process.ExitCode == 0)
                {
                    AppendLog("[MONITOR] Service started with administrator rights");
                    return true;
                }

                AppendLog($"[ERROR] Elevated start failed with exit code: {process.ExitCode}");
                return false;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                AppendLog("[MONITOR] Start service with administrator rights was canceled");
                return false;
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] Elevated start failed: {ex.Message}");
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
                    levelsText.AppendLine($"Refresh: 1 sec (anti-DDoS)");
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
                var savedIP = txtIPToUnblock.Text; // Preserve user input
                lstBannedIPs.Items.Clear();
                var blockedIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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

                        // Extract all valid IP addresses from output (regardless of encoding)
                        var ipMatches = Regex.Matches(output, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");
                        foreach (Match ipMatch in ipMatches)
                        {
                            var ip = ipMatch.Groups[1].Value;
                            if (System.Net.IPAddress.TryParse(ip, out _))
                            {
                                blockedIps.Add(ip);
                            }
                        }
                    }
                }

                foreach (var ip in blockedIps.OrderBy(x => x))
                    lstBannedIPs.Items.Add(ip);

                if (lstBannedIPs.Items.Count == 0)
                    lstBannedIPs.Items.Add(Lang.Get("MSG_NO_BLOCKED_IPS"));

                txtIPToUnblock.Text = savedIP; // Restore user input
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR] LoadBannedIPs: {ex.Message}");
            }
        }

        private void LoadWhiteList()
        {
            try
            {
                EnsureLocalIpsInWhitelistFile();

                lstWhiteList.Items.Clear();
                string whitelistPath = Path.Combine(LOG_DIR, "whiteList.log");
                var whitelistItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                                whitelistItems.Add(ip);
                        }
                    }
                }

                foreach (var ip in whitelistItems.OrderBy(x => x))
                    lstWhiteList.Items.Add(ip);

                if (lstWhiteList.Items.Count == 0)
                    lstWhiteList.Items.Add(Lang.Get("MSG_NO_WHITELISTED_IPS"));
            }
            catch { }
        }

        private static bool IsLocalOrPrivateIp(string ipAddress)
        {
            if (!System.Net.IPAddress.TryParse(ipAddress, out var parsedIp))
                return false;

            var ip = parsedIp.IsIPv4MappedToIPv6 ? parsedIp.MapToIPv4() : parsedIp;
            if (System.Net.IPAddress.IsLoopback(ip))
                return true;

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = ip.GetAddressBytes();
                if (bytes.Length != 4)
                    return false;

                return bytes[0] == 10
                    || (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                    || (bytes[0] == 192 && bytes[1] == 168)
                    || (bytes[0] == 169 && bytes[1] == 254);
            }

            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal || ip.IsIPv6SiteLocal)
                    return true;

                var bytes = ip.GetAddressBytes();
                return bytes.Length > 0 && (bytes[0] & 0xFE) == 0xFC;
            }

            return false;
        }

        private HashSet<string> GetLocalAutoWhitelistIps()
        {
            var localIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "127.0.0.1"
            };

            try
            {
                foreach (var ip in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
                {
                    if (ip.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                        continue;

                    string text = ip.ToString();
                    if (IsLocalOrPrivateIp(text))
                        localIps.Add(text);
                }
            }
            catch
            {
            }

            return localIps;
        }

        private void EnsureLocalIpsInWhitelistFile()
        {
            try
            {
                string whitelistPath = Path.Combine(LOG_DIR, "whiteList.log");
                var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (File.Exists(whitelistPath))
                {
                    foreach (var raw in File.ReadAllLines(whitelistPath, Encoding.UTF8))
                    {
                        if (string.IsNullOrWhiteSpace(raw))
                            continue;

                        string line = raw.Trim();
                        int idx = line.IndexOf("IP:", StringComparison.OrdinalIgnoreCase);
                        if (idx >= 0)
                            line = line.Substring(idx + 3).Trim();

                        if (System.Net.IPAddress.TryParse(line, out _))
                            existing.Add(line);
                    }
                }

                var toAdd = GetLocalAutoWhitelistIps().Where(ip => !existing.Contains(ip)).ToList();
                if (toAdd.Count == 0)
                    return;

                Directory.CreateDirectory(LOG_DIR);
                var entries = toAdd.Select(ip => $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] IP: {ip}");
                File.AppendAllLines(whitelistPath, entries, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                AppendLog($"[WARN] Failed to auto-whitelist local IPs: {ex.Message}");
            }
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

            if (IsDuplicateFileLogEntry(message))
                return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txtLogs.AppendText($"[{timestamp}] {message}\r\n");
            txtLogs.SelectionStart = txtLogs.Text.Length;
            txtLogs.ScrollToCaret();
        }

        private bool IsDuplicateFileLogEntry(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return false;

            bool isFileLog = message.StartsWith("[SERVICE] ", StringComparison.Ordinal)
                || message.StartsWith("[ACCESS] ", StringComparison.Ordinal)
                || message.StartsWith("[BLOCK] ", StringComparison.Ordinal);

            if (!isFileLog)
                return false;

            if (recentFileLogEntries.ContainsKey(message))
                return true;

            recentFileLogEntries[message] = DateTime.UtcNow;
            return false;
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

        private void SetLanguage(string languageCode)
        {
            if (Program.CurrentLanguage == languageCode)
            {
                SyncLanguageMenuChecks();
                return;
            }

            Program.CurrentLanguage = languageCode;
            SyncLanguageMenuChecks();
            
            RefreshAllUITexts();
            
            LoadInitialData();
        }

        private void SyncLanguageMenuChecks()
        {
            if (menuLanguageUa == null || menuLanguageEn == null || menuLanguage == null)
                return;

            bool isUa = string.Equals(Program.CurrentLanguage, "UA", StringComparison.OrdinalIgnoreCase);
            menuLanguageUa.Checked = isUa;
            menuLanguageEn.Checked = !isUa;
            menuLanguage.Text = isUa ? "Мова" : "Language";
            menuLanguage.Image = isUa ? menuLanguageUa.Image : menuLanguageEn.Image;
        }

        private Bitmap CreateLanguageFlagImage(string languageCode)
        {
            var bitmap = new Bitmap(24, 16);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);

                if (string.Equals(languageCode, "UA", StringComparison.OrdinalIgnoreCase))
                {
                    using var uaBlueBrush = new SolidBrush(Color.FromArgb(0, 87, 183));
                    using var uaYellowBrush = new SolidBrush(Color.FromArgb(255, 215, 0));
                    g.FillRectangle(uaBlueBrush, 0, 0, 24, 8);
                    g.FillRectangle(uaYellowBrush, 0, 8, 24, 8);
                }
                else
                {
                    using var ukBlueBrush = new SolidBrush(Color.FromArgb(1, 33, 105));
                    using var ukRedBrush = new SolidBrush(Color.FromArgb(200, 16, 46));
                    g.FillRectangle(ukBlueBrush, 0, 0, 24, 16);
                    g.FillRectangle(Brushes.White, 9, 0, 6, 16);
                    g.FillRectangle(Brushes.White, 0, 5, 24, 6);
                    g.FillRectangle(ukRedBrush, 10, 0, 4, 16);
                    g.FillRectangle(ukRedBrush, 0, 6, 24, 4);
                }

                g.DrawRectangle(Pens.Gray, 0, 0, 23, 15);
            }

            return bitmap;
        }

        private void RefreshAllUITexts()
        {
            // Window title
            this.Text = Lang.Get("MAIN_TITLE");
            SyncLanguageMenuChecks();

            if (lblServiceStatusHeader != null)
                lblServiceStatusHeader.Text = Lang.Get("SERVICE_STATUS_HEADER");
            if (lblConfigurationHeader != null)
                lblConfigurationHeader.Text = Lang.Get("CONFIG_HEADER");
            
            // Tab titles
            tabControl.TabPages[0].Text = Lang.Get("TAB_CURRENT_LOGS");
            tabControl.TabPages[1].Text = Lang.Get("TAB_BANNED_IPS");
            tabControl.TabPages[2].Text = Lang.Get("TAB_WHITE_LIST");
            tabControl.TabPages[3].Text = Lang.Get("TAB_MANUAL_BLOCK");
            tabControl.TabPages[4].Text = Lang.Get("TAB_SETTINGS");
            tabControl.TabPages[5].Text = Lang.Get("TAB_ALERTS");
            tabControl.TabPages[6].Text = Lang.Get("TAB_MESSAGE_SETTINGS");
            
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
            
            if (btnSaveMessageSettings != null)
                btnSaveMessageSettings.Text = Lang.Get("MSG_SETTINGS_SAVE_BTN");
            if (chkNotifyMonitorStart != null)
                chkNotifyMonitorStart.Text = Lang.Get("MSG_SETTINGS_MONITOR_START");
            if (chkNotifyMonitorClose != null)
                chkNotifyMonitorClose.Text = Lang.Get("MSG_SETTINGS_MONITOR_CLOSE");
            if (chkNotifyServiceStart != null)
                chkNotifyServiceStart.Text = Lang.Get("MSG_SETTINGS_SERVICE_START");
            if (chkNotifyServiceStop != null)
                chkNotifyServiceStop.Text = Lang.Get("MSG_SETTINGS_SERVICE_STOP");
            if (chkNotifyConfigSave != null)
                chkNotifyConfigSave.Text = Lang.Get("MSG_SETTINGS_CONFIG_SAVE");

            if (chkAntiBruteEnabled != null)
                chkAntiBruteEnabled.Text = Lang.Get("ANTI_BRUTE_ENABLED");
            if (chkSprayEnabled != null)
                chkSprayEnabled.Text = Lang.Get("ANTI_BRUTE_ENABLED_SHORT");
            if (chkRecurrenceEnabled != null)
                chkRecurrenceEnabled.Text = Lang.Get("ANTI_BRUTE_ENABLED_SHORT");
            if (chkSubnetEnabled != null)
                chkSubnetEnabled.Text = Lang.Get("ANTI_BRUTE_ENABLED_SHORT");
            
            // DataGridView columns
            dgvBlockLevels.Columns[0].HeaderText = Lang.Get("GRID_ATTEMPTS");
            dgvBlockLevels.Columns[1].HeaderText = Lang.Get("GRID_BLOCK_MINUTES");

            ApplyMainLayout();
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
                serviceController.Refresh();
                if (serviceController.Status == ServiceControllerStatus.Running)
                {
                    UpdateServiceStatus();
                    AppendLog("[MONITOR] Service already running");
                    return;
                }

                serviceController.Start();
                AppendLog("[MONITOR] Starting service...");
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                UpdateServiceStatus();
                AppendLog("[MONITOR] Service started successfully");
                if (ShouldNotify("ServiceStart"))
                    SendSimpleTelegramMessage("✅ СЕРВІС ЗАПУЩЕНО");
            }
            catch (Exception ex)
            {
                if (IsServiceAccessDenied(ex))
                {
                    AppendLog("[WARN] Service start requires administrator rights. Requesting UAC...");

                    if (TryStartServiceElevated())
                    {
                        UpdateServiceStatus();
                        AppendLog("[MONITOR] Service started successfully");
                        if (ShouldNotify("ServiceStart"))
                            SendSimpleTelegramMessage("✅ СЕРВІС ЗАПУЩЕНО");
                        return;
                    }

                    AppendLog("[WARN] Service start canceled or access denied");
                    MessageBox.Show(
                        "Administrator privileges are required to start the service. Confirm the UAC prompt or run Monitor as Administrator.",
                        "Access denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (TryStartLocalEngine())
                {
                    AppendLog("[MONITOR] Local engine started");
                    UpdateServiceStatus();
                    return;
                }

                AppendLog($"[ERROR] Service start failed: {ex.Message}");
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
                serviceController.Refresh();
                if (serviceController.Status == ServiceControllerStatus.Stopped)
                {
                    UpdateServiceStatus();
                    AppendLog("[MONITOR] Service already stopped");
                    return;
                }

                serviceController.Stop();
                AppendLog("[MONITOR] Stopping service...");
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                UpdateServiceStatus();
                AppendLog("[MONITOR] Service stopped successfully");
                if (ShouldNotify("ServiceStop"))
                    SendSimpleTelegramMessage("🛑 СЕРВІС ЗУПИНЕНО");
            }
            catch (Exception ex)
            {
                if (IsServiceAccessDenied(ex))
                {
                    AppendLog("[WARN] Service stop requires administrator rights. Requesting UAC...");

                    if (TryStopServiceElevated())
                    {
                        UpdateServiceStatus();
                        AppendLog("[MONITOR] Service stopped successfully");
                        if (ShouldNotify("ServiceStop"))
                            SendSimpleTelegramMessage("🛑 СЕРВІС ЗУПИНЕНО");
                        return;
                    }

                    AppendLog("[WARN] Service stop canceled or access denied");
                    MessageBox.Show(
                        "Administrator privileges are required to stop the service. Confirm the UAC prompt or run Monitor as Administrator.",
                        "Access denied",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                if (TryStopLocalEngine())
                {
                    AppendLog("[MONITOR] Local engine stopped");
                    UpdateServiceStatus();
                    return;
                }

                AppendLog($"[ERROR] Service stop failed: {ex.Message}");
                MessageBox.Show($"Failed to stop service: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LstBannedIPs_DoubleClick(object sender, EventArgs e)
        {
            if (lstBannedIPs.SelectedItem != null)
            {
                var selectedIP = lstBannedIPs.SelectedItem.ToString();
                if (selectedIP != Lang.Get("MSG_NO_BLOCKED_IPS"))
                {
                    txtIPToUnblock.Text = selectedIP;
                    txtIPToUnblock.Focus();
                    txtIPToUnblock.SelectAll();
                }
            }
        }

        private void LstBannedIPs_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = lstBannedIPs.IndexFromPoint(e.Location);
                if (index >= 0 && index < lstBannedIPs.Items.Count)
                {
                    lstBannedIPs.SelectedIndex = index;
                    var selectedIP = lstBannedIPs.Items[index].ToString();
                    if (selectedIP != Lang.Get("MSG_NO_BLOCKED_IPS"))
                    {
                        txtIPToUnblock.Text = selectedIP;
                        txtIPToUnblock.Focus();
                        txtIPToUnblock.SelectAll();
                    }
                }
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
                SendSimpleTelegramMessage($"🔓 РОЗБЛОКОВАНО: {ip}");
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
                SendSimpleTelegramMessage($"➕ БІЛИЙ СПИСОК: {ip}");
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
            if (ip == Lang.Get("MSG_NO_WHITELISTED_IPS")) return;

            if (IsLocalOrPrivateIp(ip))
            {
                bool isUa = string.Equals(Program.CurrentLanguage, "UA", StringComparison.OrdinalIgnoreCase);
                MessageBox.Show(
                    isUa ? "Локальні IP захищені: їх не можна видаляти з білого списку." : "Local IPs are protected and cannot be removed from whitelist.",
                    isUa ? "Захищено" : "Protected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            try
            {
                string whitelistPath = Path.Combine(LOG_DIR, "whiteList.log");
                var lines = File.ReadAllLines(whitelistPath, Encoding.UTF8).Where(l => !l.Contains(ip)).ToList();
                File.WriteAllLines(whitelistPath, lines, Encoding.UTF8);

                LoadWhiteList();
                AppendLog($"[MONITOR] IP {ip} removed from whitelist");
                SendSimpleTelegramMessage($"➖ ВИДАЛЕНО З БІЛОГО СПИСКУ: {ip}");
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

            if (IsLocalOrPrivateIp(ip))
            {
                bool isUa = string.Equals(Program.CurrentLanguage, "UA", StringComparison.OrdinalIgnoreCase);
                string message = isUa
                    ? "Локальні IP автоматично у білому списку. Блокування заборонено."
                    : "Local IPs are auto-whitelisted and cannot be blocked.";
                lblBlockStatus.Text = message;
                lblBlockStatus.ForeColor = Color.FromArgb(255, 152, 0);
                MessageBox.Show(message, isUa ? "Захищено" : "Protected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!TryParseBlockDurationMinutes(txtBlockMinutes.Text.Trim(), out int minutes))
            {
                MessageBox.Show("Enter valid duration: 60, 12h, 7d, 2w", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string durationText = FormatDurationForDisplay(minutes);

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
                lblBlockStatus.Text = $"✓ Successfully blocked IP {ip} for {durationText} ({minutes} minutes)\nUntil: {until:yyyy-MM-dd HH:mm:ss}\n\nIP has been added to firewall rule.";
                lblBlockStatus.ForeColor = Color.FromArgb(76, 175, 80);
                AppendLog($"[MONITOR] Manually blocked IP {ip} for {durationText} ({minutes}m)");
                SendSimpleTelegramMessage($"🚫 ЗАБЛОКОВАНО: {ip} на {durationText} (до {until:HH:mm})");
                txtIPToBlock.Clear();
            }
            catch (Exception ex)
            {
                lblBlockStatus.Text = $"✗ Error: {ex.Message}";
                lblBlockStatus.ForeColor = Color.FromArgb(244, 67, 54);
                AppendLog($"[ERROR] Manual block failed: {ex.Message}");
            }
        }

        private bool TryParseBlockDurationMinutes(string rawInput, out int minutes)
        {
            minutes = 0;
            if (string.IsNullOrWhiteSpace(rawInput))
                return false;

            string input = rawInput.Trim().ToLowerInvariant().Replace(" ", string.Empty);
            var match = Regex.Match(input, @"^(?<value>\d+)(?<unit>[a-zа-яіїєґ]*)$");
            if (!match.Success)
                return false;

            if (!long.TryParse(match.Groups["value"].Value, out var value) || value < 1)
                return false;

            string unit = match.Groups["unit"].Value;
            long multiplier = unit switch
            {
                "" or "m" or "min" or "mins" or "minute" or "minutes" or "хв" or "хвилин" or "мин" or "м" => 1,
                "h" or "hr" or "hrs" or "hour" or "hours" or "ч" or "час" or "часа" or "часов" or "год" => 60,
                "d" or "day" or "days" or "д" or "дн" or "день" or "дня" or "дней" => 1440,
                "w" or "wk" or "wks" or "week" or "weeks" or "н" or "нед" or "неделя" or "недель" or "тиж" or "тижд" or "тиждень" => 10080,
                _ => 0
            };

            if (multiplier == 0)
                return false;

            long totalMinutes = value * multiplier;
            if (totalMinutes < 1 || totalMinutes > int.MaxValue)
                return false;

            minutes = (int)totalMinutes;
            return true;
        }

        private string FormatDurationForDisplay(int minutes)
        {
            if (minutes % 10080 == 0)
                return $"{minutes / 10080}w";

            if (minutes % 1440 == 0)
                return $"{minutes / 1440}d";

            if (minutes % 60 == 0)
                return $"{minutes / 60}h";

            return $"{minutes}m";
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
                    txtPort.Text = (config.Port > 0 ? config.Port : 3389).ToString();
                    
                    dgvBlockLevels.Rows.Clear();
                    if (config.Levels != null)
                    {
                        foreach (var level in config.Levels)
                        {
                            dgvBlockLevels.Rows.Add(level.Attempts, level.BlockMinutes);
                        }
                    }

                    var anti = config.AntiBrute ?? AntiBruteConfig.CreateDefault();
                    var spray = anti.Spray ?? SprayConfig.CreateDefault();
                    var recurrence = anti.Recurrence ?? RecurrenceConfig.CreateDefault();
                    var subnet = anti.Subnet ?? SubnetConfig.CreateDefault();

                    chkAntiBruteEnabled.Checked = anti.Enabled;

                    chkSprayEnabled.Checked = spray.Enabled;
                    txtSprayWindowMinutes.Text = Math.Max(1, spray.WindowMinutes).ToString();
                    txtSprayUniqueIpsThreshold.Text = Math.Max(2, spray.UniqueIpsThreshold).ToString();
                    txtSprayBlockMinutes.Text = Math.Max(1, spray.BlockMinutes).ToString();

                    chkRecurrenceEnabled.Checked = recurrence.Enabled;
                    txtRecurrenceLookbackHours.Text = Math.Max(1, recurrence.LookbackHours).ToString();
                    txtRecurrenceStepMultiplier.Text = Math.Max(0.0, recurrence.StepMultiplier).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
                    txtRecurrenceMaxMultiplier.Text = Math.Max(1.0, recurrence.MaxMultiplier).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

                    chkSubnetEnabled.Checked = subnet.Enabled;
                    txtSubnetWindowMinutes.Text = Math.Max(1, subnet.WindowMinutes).ToString();
                    txtSubnetUniqueIpsThreshold.Text = Math.Max(2, subnet.UniqueIpsThreshold).ToString();
                    txtSubnetBlockMinutes.Text = Math.Max(1, subnet.BlockMinutes).ToString();

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

        private bool TryParseIntBox(TextBox textBox, string fieldName, int minValue, out int value)
        {
            value = 0;
            if (!int.TryParse(textBox.Text.Trim(), out int parsed) || parsed < minValue)
            {
                MessageBox.Show($"Invalid value for {fieldName}. Minimum: {minValue}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox.Focus();
                return false;
            }

            value = parsed;
            return true;
        }

        private bool TryParseDoubleBox(TextBox textBox, string fieldName, double minValue, out double value)
        {
            value = 0;
            string raw = (textBox.Text ?? string.Empty).Trim().Replace(',', '.');
            if (!double.TryParse(raw, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsed) || parsed < minValue)
            {
                MessageBox.Show($"Invalid value for {fieldName}. Minimum: {minValue}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox.Focus();
                return false;
            }

            value = parsed;
            return true;
        }

        private bool TryParseDurationBox(TextBox textBox, string fieldName, out int minutes, out string conversionInfo)
        {
            minutes = 0;
            conversionInfo = string.Empty;

            string rawInput = textBox.Text.Trim();
            if (!TryParseBlockDurationMinutes(rawInput, out var parsed) || parsed < 1)
            {
                MessageBox.Show($"Invalid value for {fieldName}. Use: 60, 12h, 7d, 2w", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                textBox.Focus();
                return false;
            }

            minutes = parsed;
            textBox.Text = minutes.ToString();

            if (!string.Equals(rawInput, minutes.ToString(), StringComparison.OrdinalIgnoreCase))
                conversionInfo = $"{fieldName}: {rawInput} = {minutes}m";

            return true;
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

                bool isUa = string.Equals(Program.CurrentLanguage, "UA", StringComparison.OrdinalIgnoreCase);
                var durationConversions = new List<string>();

                var levels = new List<BlockLevel>();
                foreach (DataGridViewRow row in dgvBlockLevels.Rows)
                {
                    if (row.IsNewRow)
                        continue;

                    if (row.Cells[0].Value != null && row.Cells[1].Value != null)
                    {
                        string attemptsRaw = row.Cells[0].Value?.ToString()?.Trim() ?? string.Empty;
                        string minutesRaw = row.Cells[1].Value?.ToString()?.Trim() ?? string.Empty;

                        if (!int.TryParse(attemptsRaw, out int attempts) || attempts < 1)
                        {
                            MessageBox.Show("Invalid attempts value in block levels table", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        if (!TryParseBlockDurationMinutes(minutesRaw, out int minutes) || minutes < 1)
                        {
                            MessageBox.Show("Invalid block duration in table. Use: 60, 12h, 7d, 2w", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        if (!string.Equals(minutesRaw, minutes.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            string levelLabel = isUa
                                ? $"Рівень (спроби {attempts})"
                                : $"Level (attempts {attempts})";
                            durationConversions.Add($"{levelLabel}: {minutesRaw} = {minutes}m");
                        }

                        row.Cells[1].Value = minutes;
                        levels.Add(new BlockLevel { Attempts = attempts, BlockMinutes = minutes });
                    }
                }

                if (levels.Count == 0)
                {
                    MessageBox.Show("Add at least one block level", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!TryParseIntBox(txtSprayWindowMinutes, "Spray windowMinutes", 1, out int sprayWindowMinutes)) return;
                if (!TryParseIntBox(txtSprayUniqueIpsThreshold, "Spray uniqueIpsThreshold", 2, out int sprayUniqueIpsThreshold)) return;
                string sprayLabel = isUa ? "Spray блок" : "Spray block";
                if (!TryParseDurationBox(txtSprayBlockMinutes, sprayLabel, out int sprayBlockMinutes, out string sprayConversion)) return;
                if (!string.IsNullOrWhiteSpace(sprayConversion)) durationConversions.Add(sprayConversion);

                if (!TryParseIntBox(txtRecurrenceLookbackHours, "Recurrence lookbackHours", 1, out int recurrenceLookbackHours)) return;
                if (!TryParseDoubleBox(txtRecurrenceStepMultiplier, "Recurrence stepMultiplier", 0.0, out double recurrenceStepMultiplier)) return;
                if (!TryParseDoubleBox(txtRecurrenceMaxMultiplier, "Recurrence maxMultiplier", 1.0, out double recurrenceMaxMultiplier)) return;

                if (!TryParseIntBox(txtSubnetWindowMinutes, "Subnet windowMinutes", 1, out int subnetWindowMinutes)) return;
                if (!TryParseIntBox(txtSubnetUniqueIpsThreshold, "Subnet uniqueIpsThreshold", 2, out int subnetUniqueIpsThreshold)) return;
                string subnetLabel = isUa ? "Subnet блок" : "Subnet block";
                if (!TryParseDurationBox(txtSubnetBlockMinutes, subnetLabel, out int subnetBlockMinutes, out string subnetConversion)) return;
                if (!string.IsNullOrWhiteSpace(subnetConversion)) durationConversions.Add(subnetConversion);

                string configPath = Path.Combine(LOG_DIR, "config.json");
                ServiceConfig? config;
                if (File.Exists(configPath))
                {
                    var currentJson = File.ReadAllText(configPath, Encoding.UTF8);
                    var currentOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    config = JsonSerializer.Deserialize<ServiceConfig>(currentJson, currentOptions) ?? new ServiceConfig();
                }
                else
                {
                    config = new ServiceConfig();
                }

                config.Port = port;
                config.Levels = levels;
                config.AntiBrute = new AntiBruteConfig
                {
                    Enabled = chkAntiBruteEnabled.Checked,
                    Spray = new SprayConfig
                    {
                        Enabled = chkSprayEnabled.Checked,
                        WindowMinutes = sprayWindowMinutes,
                        UniqueIpsThreshold = sprayUniqueIpsThreshold,
                        BlockMinutes = sprayBlockMinutes
                    },
                    Recurrence = new RecurrenceConfig
                    {
                        Enabled = chkRecurrenceEnabled.Checked,
                        LookbackHours = recurrenceLookbackHours,
                        StepMultiplier = recurrenceStepMultiplier,
                        MaxMultiplier = recurrenceMaxMultiplier
                    },
                    Subnet = new SubnetConfig
                    {
                        Enabled = chkSubnetEnabled.Checked,
                        WindowMinutes = subnetWindowMinutes,
                        UniqueIpsThreshold = subnetUniqueIpsThreshold,
                        BlockMinutes = subnetBlockMinutes
                    }
                };

                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(config, options);

                File.WriteAllText(configPath, json, Encoding.UTF8);

                LoadConfiguration();
                AppendLog("[MONITOR] Configuration saved successfully");

                string languageLabel = isUa ? "Українська (UA)" : "English (EN)";
                var successMessage = new StringBuilder();

                if (isUa)
                {
                    successMessage.AppendLine("Конфігурацію збережено успішно!");
                    successMessage.AppendLine("Сервіс автоматично перезавантажить налаштування.");
                    successMessage.AppendLine();
                    successMessage.AppendLine($"Поточна мова інтерфейсу: {languageLabel}");
                    if (durationConversions.Count > 0)
                    {
                        successMessage.AppendLine();
                        successMessage.AppendLine("Перерахунок тривалості:");
                        foreach (var conversion in durationConversions)
                            successMessage.AppendLine($"- {conversion}");
                    }

                    MessageBox.Show(successMessage.ToString(), "Успіх", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    successMessage.AppendLine("Configuration saved successfully!");
                    successMessage.AppendLine("Service will reload settings automatically.");
                    successMessage.AppendLine();
                    successMessage.AppendLine($"Current UI language: {languageLabel}");
                    if (durationConversions.Count > 0)
                    {
                        successMessage.AppendLine();
                        successMessage.AppendLine("Duration conversion:");
                        foreach (var conversion in durationConversions)
                            successMessage.AppendLine($"- {conversion}");
                    }

                    MessageBox.Show(successMessage.ToString(), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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
        
        [System.Text.Json.Serialization.JsonPropertyName("telegram")]
        public TelegramConfig? Telegram { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("antiBrute")]
        public AntiBruteConfig? AntiBrute { get; set; }
    }

    public class AntiBruteConfig
    {
        [System.Text.Json.Serialization.JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("spray")]
        public SprayConfig? Spray { get; set; } = SprayConfig.CreateDefault();

        [System.Text.Json.Serialization.JsonPropertyName("recurrence")]
        public RecurrenceConfig? Recurrence { get; set; } = RecurrenceConfig.CreateDefault();

        [System.Text.Json.Serialization.JsonPropertyName("subnet")]
        public SubnetConfig? Subnet { get; set; } = SubnetConfig.CreateDefault();

        public static AntiBruteConfig CreateDefault()
        {
            return new AntiBruteConfig
            {
                Enabled = true,
                Spray = SprayConfig.CreateDefault(),
                Recurrence = RecurrenceConfig.CreateDefault(),
                Subnet = SubnetConfig.CreateDefault()
            };
        }
    }

    public class SprayConfig
    {
        [System.Text.Json.Serialization.JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("windowMinutes")]
        public int WindowMinutes { get; set; } = 10;

        [System.Text.Json.Serialization.JsonPropertyName("uniqueIpsThreshold")]
        public int UniqueIpsThreshold { get; set; } = 4;

        [System.Text.Json.Serialization.JsonPropertyName("blockMinutes")]
        public int BlockMinutes { get; set; } = 240;

        public static SprayConfig CreateDefault()
        {
            return new SprayConfig
            {
                Enabled = true,
                WindowMinutes = 10,
                UniqueIpsThreshold = 4,
                BlockMinutes = 240
            };
        }
    }

    public class RecurrenceConfig
    {
        [System.Text.Json.Serialization.JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("lookbackHours")]
        public int LookbackHours { get; set; } = 24;

        [System.Text.Json.Serialization.JsonPropertyName("stepMultiplier")]
        public double StepMultiplier { get; set; } = 0.5;

        [System.Text.Json.Serialization.JsonPropertyName("maxMultiplier")]
        public double MaxMultiplier { get; set; } = 4.0;

        public static RecurrenceConfig CreateDefault()
        {
            return new RecurrenceConfig
            {
                Enabled = true,
                LookbackHours = 24,
                StepMultiplier = 0.5,
                MaxMultiplier = 4.0
            };
        }
    }

    public class SubnetConfig
    {
        [System.Text.Json.Serialization.JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("windowMinutes")]
        public int WindowMinutes { get; set; } = 30;

        [System.Text.Json.Serialization.JsonPropertyName("uniqueIpsThreshold")]
        public int UniqueIpsThreshold { get; set; } = 3;

        [System.Text.Json.Serialization.JsonPropertyName("blockMinutes")]
        public int BlockMinutes { get; set; } = 240;

        public static SubnetConfig CreateDefault()
        {
            return new SubnetConfig
            {
                Enabled = true,
                WindowMinutes = 30,
                UniqueIpsThreshold = 3,
                BlockMinutes = 240
            };
        }
    }

    public class TelegramConfig
    {
        [System.Text.Json.Serialization.JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("botToken")]
        public string BotToken { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("chatId")]
        public string ChatId { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("messageTemplates")]
        public Dictionary<string, string> MessageTemplates { get; set; } = new Dictionary<string, string>();
    }

    public class BlockLevel
    {
        [System.Text.Json.Serialization.JsonPropertyName("attempts")]
        public int Attempts { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("blockMinutes")]
        public int BlockMinutes { get; set; }
    }

    public class MessageNotificationSettings
    {
        [System.Text.Json.Serialization.JsonPropertyName("monitorStart")]
        public bool MonitorStart { get; set; } = false;

        [System.Text.Json.Serialization.JsonPropertyName("monitorClose")]
        public bool MonitorClose { get; set; } = false;

        [System.Text.Json.Serialization.JsonPropertyName("serviceStart")]
        public bool ServiceStart { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("serviceStop")]
        public bool ServiceStop { get; set; } = true;

        [System.Text.Json.Serialization.JsonPropertyName("configSave")]
        public bool ConfigSave { get; set; } = false;
    }

    // Custom transparent PictureBox for background
    public class TransparentPictureBox : PictureBox
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.Image != null)
            {
                var attributes = new System.Drawing.Imaging.ImageAttributes();
                var matrix = new System.Drawing.Imaging.ColorMatrix
                {
                    Matrix33 = 0.08f // Opacity: 8%
                };
                attributes.SetColorMatrix(matrix, System.Drawing.Imaging.ColorMatrixFlag.Default, System.Drawing.Imaging.ColorAdjustType.Bitmap);
                
                e.Graphics.DrawImage(
                    this.Image,
                    new Rectangle(0, 0, this.Width, this.Height),
                    0, 0, this.Image.Width, this.Image.Height,
                    GraphicsUnit.Pixel,
                    attributes);
            }
        }
    }
}
