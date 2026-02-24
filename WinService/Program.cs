using System;
using System.ServiceProcess;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Data.Sqlite;

class Program
{
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
        private const int CHECK_INTERVAL = 5000; // 5 seconds (test mode)
        private EventLog securityLog;
        private Dictionary<string, int> failedAttempts = new Dictionary<string, int>();
        private Thread monitorThread;
        private bool isRunning = false;
        private string logDirectory;
        private string accessLogPath;
        private string blockListLogPath;
        private string whitelistPath;
        private string configPath;
        private object logLock = new object();
        private object configLock = new object();
        private FileSystemWatcher logWatcher;
        private DateTime lastProcessedFailureTime = DateTime.MinValue;
        private int lastProcessedRecordIndex = 0;

        private volatile int failedAttemptsThreshold = DEFAULT_FAILED_ATTEMPTS_THRESHOLD;
        private volatile int blockMinutes = DEFAULT_BLOCK_MINUTES;
        private volatile int rdpPort = DEFAULT_RDP_PORT;
        private volatile List<BlockLevel> blockLevels = new List<BlockLevel> { new BlockLevel { Attempts = 3, BlockMinutes = 20 } };
        private volatile GateConfig gateConfig = new GateConfig { Enabled = false, ListenPort = 3389, TargetHost = "127.0.0.1", TargetPort = 3389 };

        private string banDbPath;
        private SqliteConnection banDb;
        private readonly object dbLock = new object();

        private TcpListener gateListener;
        private Thread gateThread;

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
            WriteLog($"Config: Levels={string.Join(",", blockLevels.Select(l => $"{l.Attempts}->{l.BlockMinutes}m"))}");

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

            WriteLog("Authentication monitoring thread started.");

            TryStartGate();
        }

        protected override void OnStop()
        {
            isRunning = false;

            try { gateListener?.Stop(); } catch { }

            if (monitorThread != null)
                monitorThread.Join(5000);

            try
            {
                if (gateThread != null && gateThread.IsAlive)
                    gateThread.Join(2000);
            }
            catch { }

            try
            {
                lock (dbLock)
                {
                    banDb?.Dispose();
                    banDb = null;
                }
            }
            catch { }

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
                }

                Thread.Sleep(CHECK_INTERVAL);
            }
        }
        private bool IsFailedLogonEvent(EventLogEntry entry)
        {
            try
            {
                if (entry.EventID == 4625)
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
                    string candidate = rs[19]?.Trim();
                    if (!string.IsNullOrWhiteSpace(candidate) && System.Net.IPAddress.TryParse(candidate, out _))
                        return candidate;
                }

                string[] markers = new[]
                {
                    "Source Network Address",
                    "Source Network Adress",
                    "РЎРµС‚РµРІРѕР№ Р°РґСЂРµСЃ РёСЃС‚РѕС‡РЅРёРєР°",
                    "РђРґСЂРµСЃ РёСЃС‚РѕС‡РЅРёРєР°"
                };

                string eventMessage = entry.Message ?? string.Empty;
                string[] lines = eventMessage.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                foreach (string line in lines)
                {
                    if (markers.Any(m => line.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        Match m = Regex.Match(line, @"\b(?:\d{1,3}\.){3}\d{1,3}\b");
                        if (m.Success) return m.Value;
                    }
                }

                // Fallback: first IPv4 in message body.
                MatchCollection matches = Regex.Matches(eventMessage, @"\b(?:\d{1,3}\.){3}\d{1,3}\b");
                foreach (Match match in matches)
                {
                    if (match.Value != "127.0.0.1" && match.Value != "0.0.0.0")
                        return match.Value;
                }
            }
            catch (Exception ex)
            {
                WriteLog($"IP parse error: {ex.Message}");
            }
            return null;
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
                    WriteLog($"Config reloaded: Levels={string.Join(",", blockLevels.Select(l => $"{l.Attempts}->{l.BlockMinutes}m"))}");
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
                    File.AppendAllText(accessLogPath, logEntry + Environment.NewLine);
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
                    File.AppendAllText(blockListLogPath, logEntry + Environment.NewLine);
                }
            }
            catch { }

            // РџРѕСЃР»Рµ Р·Р°РїРёСЃРё РІ С„Р°Р№Р» РѕР±РЅРѕРІР»СЏРµРј РµРґРёРЅРѕРµ РїСЂР°РІРёР»Рѕ
            try { UpdateFirewallRuleFromBlockList(); } catch { }
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
                    // delete the rule if exists
                    bool removed = RunPowerShell("Remove-NetFirewallRule -Name 'RDP_BLOCK_ALL' -ErrorAction SilentlyContinue | Out-Null");
                    if (removed)
                        WriteLog("RDP_BLOCK_ALL deleted (no blocked IPs)");
                    else
                        WriteLog("RDP_BLOCK_ALL delete failed");
                    return;
                }

                string safeRemoteList = remoteIPList.Replace("'", "''");
                int port = Math.Max(1, rdpPort);
                string script =
                    "$rule = Get-NetFirewallRule -Name 'RDP_BLOCK_ALL' -ErrorAction SilentlyContinue;" +
                    "if ($null -eq $rule) {" +
                    $" New-NetFirewallRule -Name 'RDP_BLOCK_ALL' -DisplayName 'RDP_BLOCK_ALL' -Direction Inbound -Action Block -Protocol TCP -LocalPort {port} -RemoteAddress '{safeRemoteList}' -Profile Any -Enabled True | Out-Null;" +
                    "} else {" +
                    $" Set-NetFirewallRule -Name 'RDP_BLOCK_ALL' -Enabled True -Direction Inbound -Action Block -Protocol TCP -LocalPort {port} | Out-Null;" +
                    $" (Get-NetFirewallRule -Name 'RDP_BLOCK_ALL' | Get-NetFirewallAddressFilter) | Set-NetFirewallAddressFilter -RemoteAddress '{safeRemoteList}' | Out-Null;" +
                    "}";

                string output;
                bool ok = RunPowerShell(script, out output);
                if (ok)
                    WriteLog($"RDP_BLOCK_ALL upserted via NetSecurity: {remoteIPList}");
                else
                    WriteLog($"RDP_BLOCK_ALL upsert failed: {output}");
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
                    File.AppendAllText(logPath, logEntry + Environment.NewLine);
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
                }
            };
        }
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

