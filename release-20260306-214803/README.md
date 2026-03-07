# RDP Security Service - Release Package

**Version:** 2026-03-06  
**Platform:** Windows x64  
**Build Type:** Self-contained (no .NET runtime required)

## 🛡️ Overview

RDP Security Service is a real-time protection system against RDP brute-force attacks. It monitors Windows Security Event Log and automatically blocks IPs based on failed authentication attempts.

## ✨ Features

- ✅ **Real-time monitoring** - 1-second check interval (anti-DDoS optimized)
- ✅ **Multi-level blocking** - Progressive ban durations based on attempt count
- ✅ **Windows Service** - Runs in background, starts automatically
- ✅ **GUI Monitor** - User-friendly interface with real-time data
- ✅ **Localization** - Ukrainian and English interface (Russian removed)
- ✅ **White-list support** - Protect trusted IPs from blocking
- ✅ **Firewall integration** - Automatic Windows Firewall rule management
- ✅ **SQLite database** - Persistent ban tracking
- ✅ **Self-contained** - No .NET runtime installation needed

## 📦 Package Contents

```
release-20260306-214803/
├── WinService/          # Windows Service files
│   ├── WinService.exe   # Main service executable
│   └── ...             # Runtime dependencies
├── Monitor/            # GUI Monitor application
│   ├── RDPMonitor.exe  # Monitor executable
│   └── ...            # Runtime dependencies
├── install.ps1         # Automated installation script
└── README.md          # This file
```

## 🚀 Quick Installation

### Prerequisites
- Windows 10/11 or Windows Server 2016+
- Administrator privileges
- RDP enabled (port 3389 by default)

### Installation Steps

1. **Extract the package** to a temporary folder
2. **Right-click** on `install.ps1`
3. **Select** "Run with PowerShell"
4. Follow the on-screen instructions

The installer will:
- Copy service files to `C:\Program Files\RDPSecurityService`
- Copy monitor to `C:\Program Files\RDPSecurityService\Monitor`
- Create data directory at `C:\ProgramData\RDPSecurityService`
- Install and start the Windows Service
- Create desktop shortcut for the monitor

## ⚙️ Default Configuration

The service creates `C:\ProgramData\RDPSecurityService\config.json` with:

```json
{
  "port": 3389,
  "levels": [
    { "attempts": 1, "blockMinutes": 20 },
    { "attempts": 2, "blockMinutes": 128 },
    { "attempts": 7, "blockMinutes": 240 },
    { "attempts": 10, "blockMinutes": 1440 }
  ]
}
```

**Blocking Logic:**
- 1 failed attempt → 20 minutes ban
- 2 failed attempts → 128 minutes ban (2.1 hours)
- 7 failed attempts → 240 minutes ban (4 hours)
- 10+ failed attempts → 1440 minutes ban (24 hours)

## 🖥️ Using the Monitor

Launch `RDPMonitor.exe` from:
- Desktop shortcut: "RDP Monitor"
- Start menu
- `C:\Program Files\RDPSecurityService\Monitor\RDPMonitor.exe`

### Monitor Features:
- **Current Logs** - Real-time event feed
- **Banned IPs** - View and manage blocked IPs
- **White List** - Add trusted IPs
- **Manual Block** - Block specific IP manually
- **Configuration** - View current settings
- **Language** - Switch between UA/EN

## 📁 File Locations

| Item | Path |
|------|------|
| Service executable | `C:\Program Files\RDPSecurityService\WinService.exe` |
| Monitor executable | `C:\Program Files\RDPSecurityService\Monitor\RDPMonitor.exe` |
| Configuration | `C:\ProgramData\RDPSecurityService\config.json` |
| Access logs | `C:\ProgramData\RDPSecurityService\access.log` |
| Block list | `C:\ProgramData\RDPSecurityService\block_list.log` |
| White list | `C:\ProgramData\RDPSecurityService\whiteList.log` |
| Ban database | `C:\ProgramData\RDPSecurityService\s.db` |

## 🔧 Manual Service Management

### Check service status:
```powershell
Get-Service RDPSecurityService
```

### Start service:
```powershell
Start-Service RDPSecurityService
```

### Stop service:
```powershell
Stop-Service RDPSecurityService
```

### Restart service:
```powershell
Restart-Service RDPSecurityService
```

### View service logs:
```powershell
Get-EventLog -LogName Application -Source RDPSecurityService -Newest 20
```

## 🛠️ Troubleshooting

### Service won't start
1. Check Event Viewer: Application logs
2. Verify port 3389 is in use by RDP
3. Ensure no other security software conflicts
4. Run as Administrator

### IPs not being blocked
1. Verify service is running: `Get-Service RDPSecurityService`
2. Check `access.log` for detected failures
3. Verify Windows Firewall is enabled
4. Check if IP is in white-list

### Monitor shows "Service not running"
1. Start the service manually
2. Check if service path is correct in monitor
3. Verify you have read permissions to `C:\ProgramData\RDPSecurityService`

## 🔒 Security Notes

- Service runs with SYSTEM privileges
- Firewall rules are managed automatically
- White-listed IPs are never blocked
- Ban database persists across reboots
- Logs are rotated automatically (kept in memory)

## 📊 Performance

- **CPU Usage:** < 1% (idle), < 5% (under attack)
- **Memory Usage:** ~30-50 MB
- **Disk I/O:** Minimal (log writes only)
- **Network:** None (local monitoring only)

## 🆕 Changelog (2026-03-06)

### New Features
- Added desktop shortcut creation
- Improved UI layout (adaptive TabControl with 50px bottom margin)
- Added refresh interval display in monitor
- Enhanced localization (UA/EN only, RU removed)

### Bug Fixes
- Fixed bottom elements being cut off in monitor
- Fixed hardcoded English strings
- Corrected typo: "СПРОМИ" → "СПРОБИ"

### Performance
- Optimized 1-second check interval for real-time protection

## 📝 Uninstallation

To remove the service:

```powershell
# Run as Administrator
Stop-Service RDPSecurityService
sc.exe delete RDPSecurityService

# Remove files
Remove-Item "C:\Program Files\RDPSecurityService" -Recurse -Force
Remove-Item "C:\ProgramData\RDPSecurityService" -Recurse -Force

# Remove desktop shortcut
Remove-Item "$env:USERPROFILE\Desktop\RDP Monitor.lnk"
```

## 📧 Support

For issues, questions, or feature requests, please contact the development team or create an issue in the project repository.

## 📜 License

© 2026 RDP Security Service. All rights reserved.

---

**Built with:** .NET 8.0, C#, WinForms  
**Target:** Windows x64  
**Build Date:** March 6, 2026
