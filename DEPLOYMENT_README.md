# RDP Security Service - Firewall Update Deployment

## Quick Status

✅ **Fixed**: Firewall synchronization now uses `netsh` instead of PowerShell cmdlets for reliable IP blocking

📦 **Deployment Package**: `artifacts/final/package/RDP-Security-Suite-win-x64/`

## What's New

- **Updated WinService.exe** (v2) with netsh-based firewall sync
- **Admin scripts** for restart and manual firewall updates
- **Documentation** of changes and testing procedures

## Installation Steps

### 1. Backup Current Service

```powershell
# Backup existing service
Copy-Item "C:\ProgramData\RDPSecurityService\WinService.exe" `
          "C:\ProgramData\RDPSecurityService\WinService.exe.backup"
```

### 2. Deploy New Binary

```powershell
# Copy new binary from deployment package
Copy-Item "artifacts\final\package\RDP-Security-Suite-win-x64\WinService.exe" `
          "C:\ProgramData\RDPSecurityService\WinService.exe" -Force
```

### 3. Restart Service (Administrator Required)

**Option A - Using Batch Script** (Recommended):
```bash
# Run as Administrator
restart_service_admin.bat
```

**Option B - Manual Restart**:
```batch
net stop RDPSecurityService
net start RDPSecurityService
```

### 4. Verify Firewall Rule

After service restart, check that firewall rule contains correct IPs:

```powershell
# Should show blocked IPs (not 1.1.1.1)
netsh advfirewall firewall show rule name="RDP_BLOCK_ALL"
```

Expected output:
```
Имя правила:                          RDP_BLOCK_ALL
...
Удаленный IP-адрес:                   192.168.1.100/32,192.168.1.174/32,...
```

## Troubleshooting

### Service Won't Start
- Check permissions: Service must run as SYSTEM or Administrator
- Review `C:\ProgramData\RDPSecurityService\service.log` for errors
- Verify WinService.exe is not corrupted: `Get-FileHash` to compare

### Firewall Rule Still Shows Wrong IPs
- Service may not have restarted yet (it caches old IP list)
- Manually run PowerShell script: `. .\update_firewall.ps1` (as Administrator)
- Clear old rule manually: `netsh advfirewall firewall delete rule name="RDP_BLOCK_ALL"`
- Wait for service to recreate rule from block_list.log

### Logs Show Netsh Errors
- May indicate permission issues
- Verify service account has firewall management rights
- Review Windows Firewall error messages in `C:\ProgramData\RDPSecurityService\service.log`

## Testing

After deployment, test the firewall blocking works:

1. Clear logs:
   ```powershell
   Clear-Content "C:\ProgramData\RDPSecurityService\access.log"
   Clear-Content "C:\ProgramData\RDPSecurityService\block_list.log"
   ```

2. Trigger failed RDP attempts from a test IP

3. Verify in Monitor:
   - Connections tab shows attempts
   - Blocked IPs tab shows correct IP address

4. Verify firewall:
   ```powershell
   netsh advfirewall firewall show rule name="RDP_BLOCK_ALL"
   # Should see test IP in RemoteAddress
   ```

## Rollback

If issues occur, revert to previous version:

```powershell
# Restore backup
Copy-Item "C:\ProgramData\RDPSecurityService\WinService.exe.backup" `
          "C:\ProgramData\RDPSecurityService\WinService.exe" -Force

# Restart service
net stop RDPSecurityService
net start RDPSecurityService
```

## Files Included

- `WinService.exe` - Updated service binary with netsh firewall sync
- `restart_service_admin.bat` - Batch script to restart service (Administrator required)
- `update_firewall.ps1` - PowerShell script for manual firewall update (Administrator required)
- `FIREWALL_FIX.md` - Technical documentation of changes

## Support

For issues or questions:
1. Check service logs: `C:\ProgramData\RDPSecurityService\service.log`
2. Review access log: `C:\ProgramData\RDPSecurityService\access.log`
3. Verify config: `C:\ProgramData\RDPSecurityService\config.json`

