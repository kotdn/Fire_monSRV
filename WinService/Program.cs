using System;
using System.ServiceProcess;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Data.Sqlite;

#pragma warning disable CA1416 // Suppress Windows-only API warnings

class Program
{
#pragma warning disable CA1416
    static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                string command = args[0].ToLower();
                if (command == "install")
                {
                    InstallService();
                    return;
                }
                else if (command == "uninstall")
                {
                    UninstallService();
                    return;
                }
            }

            ServiceBase[] servicesToRun = new ServiceBase[] { new RDPSecurityService() };
            ServiceBase.Run(servicesToRun);
        }

        static void InstallService()
        {
            try
            {
                string serviceName = "RDPSecurityService";
                string displayName = "RDP Security Service - Auth Failures Blocker";
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"create {serviceName} binPath= \"{exePath}\" DisplayName= \"{displayName}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                        Console.WriteLine("Service installed successfully");
                    else
                        Console.WriteLine($"Failed to install service (exit code: {process.ExitCode})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void UninstallService()
        {
            try
            {
                string serviceName = "RDPSecurityService";
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"delete {serviceName}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(processInfo))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                        Console.WriteLine("Service uninstalled successfully");
                    else
                        Console.WriteLine($"Failed to uninstall service (exit code: {process.ExitCode})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    public class RDPSecurityService : ServiceBase
    {
        // Defaults; overridden by C:\ProgramData\RDPSecurityService\config.json
        private const int DEFAULT_FAILED_ATTEMPTS_THRESHOLD = 5;
        private const int DEFAULT_BLOCK_MINUTES = 20;
        private const int DEFAULT_RDP_PORT = 3389;
        private const int CHECK_INTERVAL = 1000; // 1 second (real-time mode)
        private EventLog securityLog = null!;
        private Dictionary<string, int> failedAttempts = new Dictionary<string, int>();
        private Thread monitorThread = null!;
        private bool isRunning = false;
        private string logDirectory = "";
        private string accessLogPath = "";
        private string blockListLogPath = "";
        private string whitelistPath = "";
        private string configPath = "";
        private object logLock = new object();
        private object configLock = new object();
        private FileSystemWatcher logWatcher = null!;
        private DateTime lastProcessedFailureTime = DateTime.MinValue;
        private int lastProcessedRecordIndex = 0;

        private volatile int failedAttemptsThreshold = DEFAULT_FAILED_ATTEMPTS_THRESHOLD;
        private volatile int blockMinutes = DEFAULT_BLOCK_MINUTES;
        private volatile int rdpPort = DEFAULT_RDP_PORT;
        private volatile List<BlockLevel> blockLevels = new List<BlockLevel> { new BlockLevel { Attempts = 3, BlockMinutes = 20 } };
        private volatile TelegramConfig? telegramConfig = null;
        // private volatile GateConfig gateConfig = new GateConfig { Enabled = false, ListenPort = 3389, TargetHost = "127.0.0.1", TargetPort = 3389 };

        private string banDbPath = "";
        private SqliteConnection banDb = null!;
        private readonly object dbLock = new object();
        // private TcpListener gateListener;
        // private Thread gateThread;

        private sealed class BanState
        {
            public int AppliedAttempts;
            public DateTime UntilLocal;
        }

        private readonly Dictionary<string, BanState> bans = new Dictionary<string, BanState>(StringComparer.OrdinalIgnoreCase);

        public RDPSecurityService()
        {
            ServiceName = "RDPSecurityService";
            this.ServiceName = "RDPSecurityService";
            CanStop = true;
            CanPauseAndContinue = false;
            AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            isRunning = true;
            logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RDPSecurityService");
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            accessLogPath = Path.Combine(logDirectory, "access.log");
            blockListLogPath = Path.Combine(logDirectory, "block_list.log");
            whitelistPath = Path.Combine(logDirectory, "whiteList.log");
            configPath = Path.Combine(logDirectory, "config.json");
            banDbPath = Path.Combine(logDirectory, "s.db");

            // Чтобы монитор мог писать/редактировать файлы без запуска от Администратора,
            // даём группе "Пользователи" права Modify на папку и существующие файлы.
            EnsureWritableByUsers(logDirectory);

            InitBanDb();

            LoadOrCreateConfig();
            WriteLog($"Config: Port={rdpPort}; Levels={string.Join(",", blockLevels.Select(l => $"{l.Attempts}->{l.BlockMinutes}m"))}");

            securityLog = new EventLog("Security", ".");
            lastProcessedFailureTime = DateTime.UtcNow.AddMinutes(-5);
            try
            {
                if (securityLog.Entries.Count > 0)
                    lastProcessedRecordIndex = securityLog.Entries[securityLog.Entries.Count - 1].Index;
            }
            catch (Exception ex)
            {
                WriteLog($"Failed to initialize last record index: {ex.Message}");
            }

            WriteLog("RDP Security Service started. Monitoring authentication failures...");
            WriteLog($"Logs directory: {logDirectory}");
            WriteLog($"Telegram notifications: {(telegramConfig?.Enabled == true ? "ENABLED" : "DISABLED")}");

            monitorThread = new Thread(MonitorAuthenticationFailures);
            monitorThread.IsBackground = true;
            monitorThread.Start();

            // Watch for changes in whitelist and blocklist to update firewall rule
            try
            {
                logWatcher = new FileSystemWatcher(logDirectory);
                logWatcher.Filter = "*.*";
                logWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
                logWatcher.Changed += OnLogChanged;
                logWatcher.Created += OnLogChanged;
                logWatcher.Deleted += OnLogChanged;
                logWatcher.EnableRaisingEvents = true;
            }
            catch { }

            try
            {
                UpdateFirewallRuleFromBlockList();
            }
            catch (Exception ex)
            {
                WriteLog($"Initial firewall sync error: {ex.Message}");
            }

            WriteLog("Authentication monitoring thread started.");
            // TryStartGate(); // Legacy gate functionality disabled
            
            // Send start notification to Telegram
            SendServiceNotification("🚀 СТАРТАНУЛ");
        }

        protected override void OnStop()
        {
            isRunning = false;
            
            // Send stop notification to Telegram
            SendServiceNotification("⛔ УПАЛ");
            // try { gateListener?.Stop(); } catch { }

            if (monitorThread != null)
                monitorThread.Join(5000);

            // Gate functionality disabled
            // try
            // {
            //     if (gateThread != null && gateThread.IsAlive)
            //         gateThread.Join(2000);
            // }
            // catch { }

            try
            {
                lock (dbLock)
                {
                    banDb?.Dispose();
                    banDb = null;
                }
            }
            catch { }
            
            WriteLog("RDP Security Service stopping...");

            try
            {
                WriteLog("RDP Security Service stopped.");
            }
            catch
            {
                WriteLog($"Error stopping service");
            }
        }
        private void MonitorAuthenticationFailures()
        {
            while (isRunning)
            {
                try
                {
                    DateTime maxSeenTime = lastProcessedFailureTime;
                    int maxSeenRecordIndex = lastProcessedRecordIndex;

                    foreach (EventLogEntry entry in securityLog.Entries)
                    {
                        if (entry.Index <= lastProcessedRecordIndex)
                            continue;

                        if (IsFailedLogonEvent(entry))
                        {
                            string sourceIP = ExtractSourceIP(entry);

                            if (!string.IsNullOrEmpty(sourceIP) && sourceIP != "::1" && sourceIP != "127.0.0.1" && sourceIP != "-")
                            {
                                if (!failedAttempts.ContainsKey(sourceIP))
                                    failedAttempts[sourceIP] = 0;

                                failedAttempts[sourceIP]++;

                                if (IsIPWhitelisted(sourceIP))
                                {
                                    failedAttempts[sourceIP] = 0;
                                    continue;
                                }

                                WriteAccessLog(sourceIP, entry.TimeGenerated, failedAttempts[sourceIP]);

                                var level = GetLevelForAttempts(failedAttempts[sourceIP]);
                                if (level != null && ShouldApplyBan(sourceIP, failedAttempts[sourceIP], level, DateTime.Now))
                                {
                                    DateTime until = DateTime.Now.AddMinutes(Math.Max(1, level.BlockMinutes));
                                    WriteBlockLog(sourceIP, entry.TimeGenerated, failedAttempts[sourceIP], level.BlockMinutes, until);

                                    bans[sourceIP] = new BanState
                                    {
                                        AppliedAttempts = level.Attempts,
                                        UntilLocal = until
                                    };
                                }
                            }
                            else
                            {
                                WriteLog($"4625 without valid source IP (Record={entry.Index})");
                            }

                            DateTime eventTime = entry.TimeGenerated.ToUniversalTime();
                            if (eventTime > maxSeenTime)
                                maxSeenTime = eventTime;
                        }

                        if (entry.Index > maxSeenRecordIndex)
                            maxSeenRecordIndex = entry.Index;
                    }

                    lastProcessedFailureTime = maxSeenTime;
                    lastProcessedRecordIndex = maxSeenRecordIndex;
                }
                catch (Exception ex)
                {
                    WriteLog($"Monitor error: {ex.Message}");
                    SendServiceNotification($"🔴 УПАЛ: {ex.Message}");
                }

                Thread.Sleep(CHECK_INTERVAL);
            }
        }
        
        private void SendServiceNotification(string message)
        {
            try
            {
                var cfg = telegramConfig;
                if (cfg == null || !cfg.Enabled || string.IsNullOrWhiteSpace(cfg.BotToken) || string.IsNullOrWhiteSpace(cfg.ChatId))
                {
                    WriteLog($"Telegram disabled or not configured. Message not sent: {message}");
                    return;
                }

                WriteLog($"Sending Telegram notification: {message}");
                
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var url = $"https://api.telegram.org/bot{cfg.BotToken}/sendMessage";
                    var content = new System.Net.Http.FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("chat_id", cfg.ChatId),
                        new KeyValuePair<string, string>("text", message)
                    });
                    
                    var task = client.PostAsync(url, content);
                    task.Wait(TimeSpan.FromSeconds(10));
                    
                    if (task.IsCompletedSuccessfully)
                    {
                        var response = task.Result;
                        if (response.IsSuccessStatusCode)
                        {
                            WriteLog($"Telegram notification sent successfully");
                        }
                        else
                        {
                            WriteLog($"Service notification failed: {response.StatusCode}");
                        }
                    }
                    else if (task.IsFaulted)
                    {
                        WriteLog($"Service notification request failed: {task.Exception?.InnerException?.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Service notification error: {ex.Message}");
            }
        }
        
        private bool IsFailedLogonEvent(EventLogEntry entry)
        {
            try
            {
                if (entry.InstanceId == 4625)
                    return true;

                return ((int)entry.InstanceId & 0xFFFF) == 4625;
            }
            catch
            {
                return false;
            }
        }

        private string ExtractSourceIP(EventLogEntry entry)
        {
            try
            {
                var rs = entry.ReplacementStrings;
                if (rs != null && rs.Length > 19)
                {
                    string candidate = NormalizeIpCandidate(rs[19]);
                    if (!string.IsNullOrWhiteSpace(candidate))
                        return candidate;
                }

                if (rs != null)
                {
                    foreach (string value in rs)
                    {
                        string candidate = NormalizeIpCandidate(value);
                        if (!string.IsNullOrWhiteSpace(candidate))
                            return candidate;
                    }
                }

                string[] markers = new[]
                {
                    "Source Network Address",
                    "Source Network Adress",
                    "Network Address",
                    "Source Address",
                    "Сетевой адрес источника",
                    "Адрес источника",
                    "РЎРµС‚РµРІРѕР№ Р°РґСЂРµСЃ РёСЃС‚РѕС‡РЅРёРєР°",
                    "РђРґСЂРµСЃ РёСЃС‚РѕС‡РЅРёРєР°"
                };

                string eventMessage = entry.Message ?? string.Empty;
                string[] lines = eventMessage.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    if (markers.Any(m => line.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        foreach (string token in SplitIpCandidates(line))
                        {
                            string candidate = NormalizeIpCandidate(token);
                            if (!string.IsNullOrWhiteSpace(candidate))
                                return candidate;
                        }
                    }
                }

                foreach (string token in SplitIpCandidates(eventMessage))
                {
                    string candidate = NormalizeIpCandidate(token);
                    if (!string.IsNullOrWhiteSpace(candidate))
                        return candidate;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"IP parse error: {ex.Message}");
            }
            return null;
        }

        private IEnumerable<string> SplitIpCandidates(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Enumerable.Empty<string>();

            return text.Split(new[] { ' ', '\t', '\r', '\n', ',', ';', '|', '(', ')', '{', '}', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private string NormalizeIpCandidate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string candidate = raw.Trim().Trim('.', ':');
            if (candidate == "-" || candidate.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                return null;

            if (candidate.StartsWith("[", StringComparison.Ordinal) && candidate.Contains("]", StringComparison.Ordinal))
            {
                int close = candidate.IndexOf(']');
                candidate = candidate.Substring(1, close - 1);
            }

            if (candidate.Count(c => c == ':') == 1 && candidate.Contains('.') && candidate.LastIndexOf(':') > 0)
            {
                string withoutPort = candidate.Substring(0, candidate.LastIndexOf(':'));
                if (IPAddress.TryParse(withoutPort, out _))
                    candidate = withoutPort;
            }

            if (candidate.StartsWith("::ffff:", StringComparison.OrdinalIgnoreCase))
            {
                string mapped = candidate.Substring(7);
                if (IPAddress.TryParse(mapped, out IPAddress mappedIp))
                    candidate = mappedIp.ToString();
            }

            if (!IPAddress.TryParse(candidate, out IPAddress ip))
                return null;

            if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.IPv6Any))
                return null;

            if (ip.IsIPv4MappedToIPv6)
                return ip.MapToIPv4().ToString();

            return ip.ToString();
        }

        private void OnLogChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // small debounce
                Thread.Sleep(200);
                if (e.Name.Equals("block_list.log", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateFirewallRuleFromBlockList();
                }
                else if (e.Name.Equals("whiteList.log", StringComparison.OrdinalIgnoreCase))
                {
                    // whitelist changed: ensure no whitelisted IP remains in firewall block list
                    UpdateFirewallRuleFromBlockList();
                }
                else if (e.Name.Equals("config.json", StringComparison.OrdinalIgnoreCase))
                {
                    LoadOrCreateConfig();
                    UpdateFirewallRuleFromBlockList();
                    WriteLog($"Config reloaded: Port={rdpPort}; Levels={string.Join(",", blockLevels.Select(l => $"{l.Attempts}->{l.BlockMinutes}m"))}");
                }
            }
            catch { }
        }

        private bool IsIPWhitelisted(string ipAddress)
        {
            try
            {
                return LoadWhitelistSet().Contains(ipAddress);
            }
            catch (Exception ex)
            {
                WriteLog($"Error checking whitelist: {ex.Message}");
                return false;
            }
        }

        private void BlockIP(string ipAddress)
        {
            // Deprecated: we update firewall rules centrally from block_list.log
            // Keep this method for compatibility but simply update the aggregate rule
            try
            {
                UpdateFirewallRuleFromBlockList();
            }
            catch (Exception ex)
            {
                WriteLog($"Error updating aggregated firewall rule: {ex.Message}");
            }
        }

        private void WriteAccessLog(string ipAddress, DateTime timestamp, int attemptCount)
        {
            try
            {
                lock (logLock)
                {
                    string logEntry = $"[{timestamp:yyyy-MM-dd HH:mm:ss}] IP: {ipAddress} | Attempts: {attemptCount}";
                    File.AppendAllText(accessLogPath, logEntry + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }

        private void WriteBlockLog(string ipAddress, DateTime timestamp, int attemptCount, int blockMinutes, DateTime untilLocal)
        {
            try
            {
                lock (logLock)
                {
                    string logEntry =
                        $"[{timestamp:yyyy-MM-dd HH:mm:ss}] BLOCKED IP: {ipAddress} | Failed Attempts: {attemptCount} | BlockMinutes: {blockMinutes} | Until: {untilLocal:yyyy-MM-dd HH:mm:ss}";
                    File.AppendAllText(blockListLogPath, logEntry + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }

            // РџРѕСЃР»Рµ Р·Р°РїРёСЃРё РІ С„Р°Р№Р» РѕР±РЅРѕРІР»СЏРµРј РµРґРёРЅРѕРµ РїСЂР°РІРёР»Рѕ
            try { UpdateFirewallRuleFromBlockList(); } catch { }

            // Send Telegram notification
            try { SendTelegramNotification(ipAddress, attemptCount, blockMinutes, untilLocal); } catch { }
        }

        private async void SendTelegramNotification(string ipAddress, int attemptCount, int blockMinutes, DateTime untilLocal)
        {
            try
            {
                var cfg = telegramConfig;
                if (cfg == null || !cfg.Enabled || string.IsNullOrWhiteSpace(cfg.BotToken) || string.IsNullOrWhiteSpace(cfg.ChatId))
                    return;

                // Determine which level template to use
                string templateKey = "default";
                var levels = blockLevels;
                if (levels != null && levels.Count > 0)
                {
                    for (int i = 0; i < levels.Count; i++)
                    {
                        if (attemptCount <= levels[i].Attempts)
                        {
                            templateKey = $"level{i + 1}";
                            break;
                        }
                    }
                    // If attempts exceed all levels, use the last level
                    if (templateKey == "default" && attemptCount > levels[levels.Count - 1].Attempts)
                    {
                        templateKey = $"level{levels.Count}";
                    }
                }

                // Get template
                string template;
                if (cfg.MessageTemplates != null && cfg.MessageTemplates.ContainsKey(templateKey))
                {
                    template = cfg.MessageTemplates[templateKey];
                }
                else if (cfg.MessageTemplates != null && cfg.MessageTemplates.ContainsKey("default"))
                {
                    template = cfg.MessageTemplates["default"];
                }
                else
                {
                    // Fallback template
                    template = "🚨 RDP Security Alert\\n\\nBlocked IP: {ip}\\nAttempts: {attempts}\\nBan: {duration} min";
                }

                // Replace placeholders
                string message = template
                    .Replace("{ip}", ipAddress)
                    .Replace("{attempts}", attemptCount.ToString())
                    .Replace("{duration}", blockMinutes.ToString())
                    .Replace("\\n", "\n");

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var url = $"https://api.telegram.org/bot{cfg.BotToken}/sendMessage";
                    var content = new System.Net.Http.FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("chat_id", cfg.ChatId),
                        new KeyValuePair<string, string>("text", message),
                        new KeyValuePair<string, string>("parse_mode", "Markdown")
                    });
                    
                    var response = await client.PostAsync(url, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        WriteLog($"Telegram notification failed: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Telegram send error: {ex.Message}");
            }
        }

        private void UpdateFirewallRuleFromBlockList()
        {
            try
            {
                var whitelist = LoadWhitelistSet();
                List<string> blockedIPs = new List<string>();
                bool blockListPruned = false;
                bool expiredPruned = false;
                DateTime now = DateTime.Now;
                int defaultTtlMinutes = Math.Max(1, blockMinutes);

                if (File.Exists(blockListLogPath))
                {
                    lock (logLock)
                    {
                        var lines = File.ReadAllLines(blockListLogPath).ToList();
                        var keptLines = new List<string>(lines.Count);

                        foreach (var line in lines)
                        {
                            if (string.IsNullOrWhiteSpace(line))
                                continue;

                            // Expiration: prefer explicit "Until", otherwise fallback to timestamp + default TTL.
                            if (TryParseUntilFromBlockLogLine(line, out DateTime untilLocal))
                            {
                                if (now > untilLocal)
                                {
                                    expiredPruned = true;
                                    continue;
                                }
                            }
                            else if (TryParseBlockLogTimestamp(line, out DateTime ts))
                            {
                                if (now - ts > TimeSpan.FromMinutes(defaultTtlMinutes))
                                {
                                    expiredPruned = true;
                                    continue; // expired block entry
                                }
                            }

                            string ip = ExtractBlockedIpFromLine(line);
                            if (!string.IsNullOrWhiteSpace(ip) && whitelist.Contains(ip))
                            {
                                blockListPruned = true;
                                continue; // whitelist has priority; remove from block list file
                            }

                            keptLines.Add(line);
                            if (!string.IsNullOrWhiteSpace(ip) && !blockedIPs.Contains(ip))
                                blockedIPs.Add(ip);
                        }

                        if (blockListPruned || expiredPruned)
                            File.WriteAllLines(blockListLogPath, keptLines);
                    }
                }

                if (blockListPruned)
                    WriteLog("Pruned whitelisted IPs from block_list.log");
                if (expiredPruned)
                    WriteLog($"Pruned expired IPs from block_list.log (default TTL={defaultTtlMinutes}m)");

                string remoteIPList = string.Join(",", blockedIPs);

                if (string.IsNullOrWhiteSpace(remoteIPList))
                {
                    // Delete the rule if it exists
                    try
                    {
                        // Check if rule exists first
                        var checkPsi = new ProcessStartInfo
                        {
                            FileName = "netsh.exe",
                            Arguments = "advfirewall firewall show rule name=\"RDP_BLOCK_ALL\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using (var checkProc = Process.Start(checkPsi))
                        {
                            checkProc.WaitForExit(3000);
                            if (checkProc.ExitCode != 0)
                            {
                                // Rule doesn't exist, nothing to do
                                return;
                            }
                        }

                        // Rule exists, delete it
                        var psi = new ProcessStartInfo
                        {
                            FileName = "netsh.exe",
                            Arguments = "advfirewall firewall delete rule name=\"RDP_BLOCK_ALL\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };
                        using (var p = Process.Start(psi))
                        {
                            p.WaitForExit(5000);
                            if (p.ExitCode == 0)
                            {
                                WriteLog("RDP_BLOCK_ALL deleted (no blocked IPs)");
                            }
                        }
                    }
                    catch
                    {
                        // Ignore errors - rule may already be deleted
                    }
                    return;
                }

                int port = Math.Max(1, rdpPort);
                
                // Use netsh for reliable firewall update (no PowerShell cmdlet issues)
                try
                {
                    // Delete old rule
                    var delPsi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = @"/c netsh advfirewall firewall delete rule name=""RDP_BLOCK_ALL""",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using (var p = Process.Start(delPsi))
                    {
                        p.WaitForExit(3000);
                    }

                    // Build IP list with /32 for each IP
                    string[] ips = blockedIPs.ToArray();
                    string remoteIpList = string.Join(",", ips.Select(ip => ip + "/32"));

                    // Create new rule with netsh
                    var addPsi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $@"/c netsh advfirewall firewall add rule name=""RDP_BLOCK_ALL"" dir=in action=block protocol=tcp localport={port} remoteip=""{remoteIpList}"" profile=any enable=yes",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    using (var p = Process.Start(addPsi))
                    {
                        string output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                        p.WaitForExit(5000);
                        if (p.ExitCode == 0 || output.IndexOf("Ok", StringComparison.OrdinalIgnoreCase) >= 0)
                            WriteLog($"RDP_BLOCK_ALL upserted via netsh: {remoteIPList}");
                        else
                            WriteLog($"RDP_BLOCK_ALL upsert failed (exit {p.ExitCode}): {output}");
                    }
                }
                catch (Exception ex)
                {
                    WriteLog($"Error updating firewall rule via netsh: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Error updating firewall rule from blocklist: {ex.Message}");
            }
        }

        private bool TryParseUntilFromBlockLogLine(string line, out DateTime untilLocal)
        {
            untilLocal = default;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            int idx = line.IndexOf("Until:", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return false;

            string tail = line.Substring(idx + 6).Trim();
            if (tail.Contains("|"))
                tail = tail.Split('|')[0].Trim();

            return DateTime.TryParseExact(
                tail,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out untilLocal);
        }

        private bool RunPowerShell(string script)
        {
            string _;
            return RunPowerShell(script, out _);
        }

        private bool RunPowerShell(string script, out string output)
        {
            output = string.Empty;
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (var p = Process.Start(psi))
                {
                    output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
                    p.WaitForExit(10000);
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        private void WriteLog(string message)
        {
            try
            {
                string logPath = Path.Combine(logDirectory, "service.log");
                lock (logLock)
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
                    File.AppendAllText(logPath, logEntry + Environment.NewLine, Encoding.UTF8);
                }
            }
            catch { }
        }

        private void EnsureWritableByUsers(string dir)
        {
            try
            {
                var usersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                var rule = new FileSystemAccessRule(
                    usersSid,
                    FileSystemRights.Modify | FileSystemRights.Synchronize,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                    PropagationFlags.None,
                    AccessControlType.Allow);

                var di = new DirectoryInfo(dir);
                var ds = di.GetAccessControl();
                bool modified;
                ds.ModifyAccessRule(AccessControlModification.Add, rule, out modified);
                if (modified)
                    di.SetAccessControl(ds);

                // На случай если файлы уже созданы с "жёсткими" ACL, добавляем правило и на них.
                string[] paths =
                {
                    accessLogPath,
                    blockListLogPath,
                    whitelistPath,
                    Path.Combine(dir, "service.log"),
                    Path.Combine(dir, "current_log.log"),
                    configPath
                };

                foreach (var p in paths)
                {
                    try
                    {
                        if (!File.Exists(p))
                            continue;

                        var fi = new FileInfo(p);
                        var fs = fi.GetAccessControl();
                        bool fm;
                        fs.ModifyAccessRule(AccessControlModification.Add, rule, out fm);
                        if (fm)
                            fi.SetAccessControl(fs);
                    }
                    catch { }
                }

                WriteLog("ACL: granted Modify to Builtin Users for logs/config");
            }
            catch (Exception ex)
            {
                try { WriteLog("ACL error: " + ex.Message); } catch { }
            }
        }

        private HashSet<string> LoadWhitelistSet()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(whitelistPath))
                return set;

            lock (logLock)
            {
                foreach (string raw in File.ReadAllLines(whitelistPath))
                {
                    if (string.IsNullOrWhiteSpace(raw))
                        continue;

                    string line = raw.Trim();
                    int i = line.IndexOf("IP:", StringComparison.OrdinalIgnoreCase);
                    if (i >= 0)
                        line = line.Substring(i + 3).Trim();

                    if (System.Net.IPAddress.TryParse(line, out _))
                        set.Add(line);
                }
            }

            return set;
        }

        private string ExtractBlockedIpFromLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            int idx = line.IndexOf("BLOCKED IP:", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                return null;

            string ip = line.Substring(idx + 11).Split('|')[0].Trim();
            return System.Net.IPAddress.TryParse(ip, out _) ? ip : null;
        }

        private bool TryParseBlockLogTimestamp(string line, out DateTime timestampLocal)
        {
            timestampLocal = default;
            if (string.IsNullOrWhiteSpace(line))
                return false;

            int open = line.IndexOf('[');
            int close = line.IndexOf(']');
            if (open < 0 || close <= open + 1)
                return false;

            string ts = line.Substring(open + 1, close - open - 1).Trim();
            return DateTime.TryParseExact(
                ts,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out timestampLocal);
        }

        private void LoadOrCreateConfig()
        {
            try
            {
                lock (configLock)
                {
                    ServiceConfig cfg = null;
                    if (File.Exists(configPath))
                    {
                        try
                        {
                            string json = File.ReadAllText(configPath);
                            cfg = JsonSerializer.Deserialize<ServiceConfig>(json, ServiceConfigJson.Options);
                        }
                        catch (Exception ex)
                        {
                            WriteLog($"Config parse error: {ex.Message}");
                        }
                    }

                    if (cfg == null)
                    {
                        cfg = ServiceConfig.CreateDefault();
                        File.WriteAllText(configPath, JsonSerializer.Serialize(cfg, ServiceConfigJson.Options));
                    }

                    var levels = (cfg.Levels ?? new List<BlockLevel>())
                        .Where(l => l != null && l.Attempts > 0 && l.BlockMinutes > 0)
                        .OrderBy(l => l.Attempts)
                        .Select(l => new BlockLevel { Attempts = l.Attempts, BlockMinutes = l.BlockMinutes })
                        .ToList();

                    if (levels.Count == 0)
                    {
                        levels = ServiceConfig.CreateDefault().Levels;
                        File.WriteAllText(configPath, JsonSerializer.Serialize(new ServiceConfig { Levels = levels }, ServiceConfigJson.Options));
                    }

                    blockLevels = levels;

                    // For legacy entries without explicit Until, use the smallest configured block duration.
                    failedAttemptsThreshold = levels[0].Attempts;
                    blockMinutes = levels[0].BlockMinutes;

                    int port = cfg.Port.HasValue ? cfg.Port.Value : DEFAULT_RDP_PORT;
                    rdpPort = Math.Max(1, port);

                    // Load Telegram configuration
                    telegramConfig = cfg.Telegram ?? new TelegramConfig { Enabled = false, BotToken = "", ChatId = "" };
                }
            }
            catch (Exception ex)
            {
                WriteLog($"Config load error: {ex.Message}");
                failedAttemptsThreshold = DEFAULT_FAILED_ATTEMPTS_THRESHOLD;
                blockMinutes = DEFAULT_BLOCK_MINUTES;
                blockLevels = new List<BlockLevel> { new BlockLevel { Attempts = 3, BlockMinutes = 20 } };
            }
        }

        private BlockLevel GetLevelForAttempts(int attempts)
        {
            if (attempts <= 0)
                return null;

            // Highest level that matches current attempts count (or lower)
            // This way, if we missed some attempts due to polling intervals, we still apply the right (strongest) ban.
            var levels = blockLevels;
            BlockLevel match = null;
            for (int i = 0; i < levels.Count; i++)
            {
                var l = levels[i];
                if (attempts >= l.Attempts)
                    match = l;
            }
            return match;
        }

        private void InitBanDb()
        {
            try
            {
                lock (dbLock)
                {
                    try
                    {
                        banDb?.Dispose();
                    }
                    catch { }

                    banDb = new SqliteConnection($"Data Source={banDbPath}");
                    banDb.Open();

                    using (var cmd = banDb.CreateCommand())
                    {
                        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS bans (
    id INTEGER PRIMARY KEY,
    ip TEXT NOT NULL,
    applied_attempts INTEGER,
    until_local TEXT
)";
                        cmd.ExecuteNonQuery();
                    }

                    WriteLog($"Ban DB initialized at {banDbPath}");
                }
            }
            catch (Exception ex)
            {
                WriteLog($"InitBanDb error: {ex.Message}");
            }
        }

        private bool ShouldApplyBan(string ip, int attempts, BlockLevel level, DateTime nowLocal)
        {
            try
            {
                BanState s;
                if (bans.TryGetValue(ip, out s))
                {
                    if (nowLocal <= s.UntilLocal)
                    {
                        // Already banned; only extend if a higher level is reached.
                        return level.Attempts > s.AppliedAttempts;
                    }

                    // expired in-memory state; allow a new ban
                    bans.Remove(ip);
                }

                // Not banned: apply when we reached the first level or any higher one.
                return true;
            }
            catch
            {
                return true;
            }
        }
    }

    public class ServiceConfig
    {
        [JsonPropertyName("port")]
        public int? Port { get; set; }

        [JsonPropertyName("levels")]
        public List<BlockLevel> Levels { get; set; } = new List<BlockLevel>();

        [JsonPropertyName("telegram")]
        public TelegramConfig? Telegram { get; set; }

        public static ServiceConfig CreateDefault()
        {
            return new ServiceConfig
            {
                Port = 3389,
                Levels = new List<BlockLevel>
                {
                    new BlockLevel { Attempts = 3, BlockMinutes = 30 },
                    new BlockLevel { Attempts = 5, BlockMinutes = 180 },
                    new BlockLevel { Attempts = 7, BlockMinutes = 2880 }
                },
                Telegram = new TelegramConfig
                {
                    Enabled = false,
                    BotToken = "",
                    ChatId = "",
                    MessageTemplates = new Dictionary<string, string>
                    {
                        ["level1"] = "🟡 RDP Alert LEVEL 1\n\nIP: {ip}\nAttempts: {attempts}\nBan: {duration} min",
                        ["level2"] = "🟠 RDP Alert LEVEL 2\n\nIP: {ip}\nAttempts: {attempts}\nBan: {duration} min",
                        ["level3"] = "🔴 RDP Alert LEVEL 3\n\nIP: {ip}\nAttempts: {attempts}\nBan: {duration} min",
                        ["default"] = "🚨 RDP Security Alert\n\nBlocked IP: {ip}\nAttempts: {attempts}\nBan: {duration} min"
                    }
                }
            };
        }
    }

    public class TelegramConfig
    {
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("botToken")]
        public string BotToken { get; set; } = "";

        [JsonPropertyName("chatId")]
        public string ChatId { get; set; } = "";

        [JsonPropertyName("messageTemplates")]
        public Dictionary<string, string> MessageTemplates { get; set; } = new Dictionary<string, string>();
    }

    public class BlockLevel
    {
        [JsonPropertyName("attempts")]
        public int Attempts { get; set; }

        [JsonPropertyName("blockMinutes")]
        public int BlockMinutes { get; set; }
    }

    internal static class ServiceConfigJson
    {
        public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

