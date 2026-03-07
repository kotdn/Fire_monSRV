# Installation Guide - RDP Security Service

## 🎯 Quick Start

### Minimum Requirements
- Windows 10/11 or Windows Server 2016+
- Administrator privileges
- 100 MB free disk space
- RDP service enabled

### 1-Minute Installation

1. **Extract** the release package to any folder
2. **Right-click** `install.ps1` → **Run with PowerShell**
3. **Wait** for installation to complete
4. **Done!** Service is running and protecting your server

## 📋 Detailed Installation

### Step 1: Prepare System

Ensure you have:
- Administrator account access
- RDP enabled (check with `Get-Service TermService`)
- Windows Firewall enabled
- No conflicting security software

### Step 2: Run Installer

```powershell
# Open PowerShell as Administrator
cd path\to\release-20260306-214803
.\install.ps1
```

The installer will:
1. ✅ Check administrator privileges
2. ✅ Stop existing service (if any)
3. ✅ Create installation directories
4. ✅ Copy service files to `C:\Program Files\RDPSecurityService`
5. ✅ Copy monitor to `C:\Program Files\RDPSecurityService\Monitor`
6. ✅ Create data directory at `C:\ProgramData\RDPSecurityService`
7. ✅ Generate default config.json
8. ✅ Install Windows Service
9. ✅ Start the service
10. ✅ Create desktop shortcut

### Step 3: Verify Installation

Check service status:
```powershell
Get-Service RDPSecurityService
```

Expected output:
```
Status   Name                   DisplayName
------   ----                   -----------
Running  RDPSecurityService     RDP Security Service - Auth Failu...
```

### Step 4: Launch Monitor

- **Option 1:** Double-click "RDP Monitor" shortcut on desktop
- **Option 2:** Run from Start menu
- **Option 3:** Navigate to `C:\Program Files\RDPSecurityService\Monitor\RDPMonitor.exe`

## 🔧 Manual Installation

If automatic installation fails, install manually:

### 1. Copy Files

```powershell
# Create directories
New-Item -ItemType Directory -Path "C:\Program Files\RDPSecurityService" -Force
New-Item -ItemType Directory -Path "C:\ProgramData\RDPSecurityService" -Force

# Copy service
Copy-Item ".\WinService\*" -Destination "C:\Program Files\RDPSecurityService" -Recurse -Force

# Copy monitor
New-Item -ItemType Directory -Path "C:\Program Files\RDPSecurityService\Monitor" -Force
Copy-Item ".\Monitor\*" -Destination "C:\Program Files\RDPSecurityService\Monitor" -Recurse -Force
```

### 2. Create Configuration

Create `C:\ProgramData\RDPSecurityService\config.json`:

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

### 3. Install Service

```powershell
sc.exe create RDPSecurityService binPath= "C:\Program Files\RDPSecurityService\WinService.exe" DisplayName= "RDP Security Service - Auth Failures Blocker" start= auto
```

### 4. Start Service

```powershell
Start-Service RDPSecurityService
```

## ⚙️ Post-Installation Configuration

### Configure Block Levels

Edit `C:\ProgramData\RDPSecurityService\config.json`:

```json
{
  "port": 3389,
  "levels": [
    { "attempts": 3, "blockMinutes": 30 },
    { "attempts": 5, "blockMinutes": 180 },
    { "attempts": 10, "blockMinutes": 1440 }
  ]
}
```

Restart service after changes:
```powershell
Restart-Service RDPSecurityService
```

### Add White-listed IPs

Create `C:\ProgramData\RDPSecurityService\whiteList.log`:

```
192.168.1.100
10.0.0.5
```

Or use the Monitor GUI:
1. Open RDP Monitor
2. Go to "White List" tab
3. Enter IP and click "Add to White List"

### Change RDP Port

If using non-standard RDP port, update config:

```json
{
  "port": 3390,
  "levels": [ ... ]
}
```

## 🔍 Verify Installation

### Check Service
```powershell
Get-Service RDPSecurityService | Format-List *
```

### Check Logs
```powershell
Get-Content "C:\ProgramData\RDPSecurityService\access.log" -Tail 20
```

### Check Firewall Rule
```powershell
Get-NetFirewallRule -DisplayName "*RDP*Block*"
```

### Test Monitor
1. Launch RDPMonitor.exe
2. Select language (UA or EN)
3. Verify service status shows "Running"
4. Check configuration tab displays correct settings

## 🚨 Troubleshooting

### "Access Denied" Error
- Ensure running PowerShell as Administrator
- Check user has local admin rights

### Service Fails to Start
- Check Event Viewer: `Get-EventLog -LogName Application -Newest 10`
- Verify WinService.exe exists in install path
- Check Windows Firewall is enabled

### Monitor Can't Connect
- Verify service is running
- Check data path permissions: `C:\ProgramData\RDPSecurityService`
- Ensure current user has read access to data folder

### Configuration Not Applied
- Restart service after config changes
- Verify JSON syntax is valid
- Check file encoding is UTF-8

## 🔄 Upgrading from Previous Version

1. **Backup** current configuration:
   ```powershell
   Copy-Item "C:\ProgramData\RDPSecurityService\*" -Destination "C:\Backup\RDPSec" -Recurse
   ```

2. **Stop** the service:
   ```powershell
   Stop-Service RDPSecurityService
   ```

3. **Run** new installer (install.ps1)

4. **Restore** custom configuration if needed:
   ```powershell
   Copy-Item "C:\Backup\RDPSec\config.json" -Destination "C:\ProgramData\RDPSecurityService\" -Force
   ```

5. **Restart** service:
   ```powershell
   Start-Service RDPSecurityService
   ```

## 🗑️ Uninstallation

### Automated Removal

```powershell
# Run as Administrator
Stop-Service RDPSecurityService
sc.exe delete RDPSecurityService

# Remove program files
Remove-Item "C:\Program Files\RDPSecurityService" -Recurse -Force

# Remove data (includes logs and config)
Remove-Item "C:\ProgramData\RDPSecurityService" -Recurse -Force

# Remove desktop shortcut
Remove-Item "$env:USERPROFILE\Desktop\RDP Monitor.lnk" -ErrorAction SilentlyContinue
```

### Keep Configuration

To preserve config and logs during uninstall:

```powershell
# Backup data
Copy-Item "C:\ProgramData\RDPSecurityService" -Destination "C:\Backup\RDPSec-Data" -Recurse

# Uninstall service
Stop-Service RDPSecurityService
sc.exe delete RDPSecurityService
Remove-Item "C:\Program Files\RDPSecurityService" -Recurse -Force
```

## 📞 Support

If installation issues persist:
1. Check Event Viewer for errors
2. Review service logs in `C:\ProgramData\RDPSecurityService\`
3. Contact system administrator
4. Create issue in project repository

---

**Installation Time:** ~2 minutes  
**Disk Space Required:** ~50-100 MB  
**Administrator Required:** Yes
