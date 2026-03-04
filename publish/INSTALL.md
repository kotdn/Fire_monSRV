# RDP Security Service - Release Build

**Version**: 2026-03-01  
**Status**: ✅ READY FOR DEPLOYMENT

## Package Contents

- `WinService.exe` - Windows Service (background monitoring)
- `RDPSecurityViewer.exe` - GUI Monitor (real-time display)
- `restart_service_admin.bat` - Service restart script (requires Administrator)
- `update_firewall.ps1` - Manual firewall update (requires Administrator)

## Installation

### Quick Install (Automated)

```batch
REM Run from Administrator prompt:
cd C:\path\to\release
restart_service_admin.bat
```

### Manual Install

1. **Stop existing service (if running)**:
   ```batch
   net stop RDPSecurityService
   ```

2. **Copy files**:
   ```batch
   copy WinService.exe C:\ProgramData\RDPSecurityService\
   copy RDPSecurityViewer.exe C:\ProgramData\RDPSecurityService\
   ```

3. **Start service**:
   ```batch
   net start RDPSecurityService
   ```

4. **Verify status**:
   ```bash
   sc query RDPSecurityService
   ```

## Usage

### Launch Monitor

```bash
C:\ProgramData\RDPSecurityService\RDPSecurityViewer.exe
```

Or from release package:
```bash
.\RDPSecurityViewer.exe
```

### Monitor Tabs

1. **Failed Attempts** - All detected failed RDP logins
2. **Blocked IPs** - Currently/previously blocked IPs (grouped by IP address)
3. **WhiteList** - IP addresses exempt from blocking
4. **Current Log** - Real-time TCP connections

## Configuration

**File**: `C:\ProgramData\RDPSecurityService\config.json`

Default blocking levels:
- 3 attempts → 30 minutes
- 5 attempts → 180 minutes (3 hours)
- 7 attempts → 2880 minutes (48 hours)

Edit to customize.

## Logs

- `access.log` - All RDP connection attempts
- `block_list.log` - Blocked IP entries with timestamps
- `service.log` - Internal service events
- `current_log.log` - Real-time connection snapshot

## Firewall Rule

**Name**: `RDP_BLOCK_ALL`  
**Port**: 3389 (configurable)  
**Action**: Block inbound TCP from specified IPs

View current rule:
```bash
netsh advfirewall firewall show rule name="RDP_BLOCK_ALL"
```

## Features

✅ **Real-time Monitoring**
- Detects failed RDP login attempts from Event Log (Event ID 4625)
- Tracks attempts per source IP
- Updates in real-time via TCP enumeration

✅ **Automatic Blocking**
- Configurable attempt thresholds per blocking level
- Automatic firewall rule creation/update
- Blocks inbound TCP on RDP port

✅ **Smart Display**
- Groups blocked IPs (no duplicates)
- Shows IP status: ACTIVE (still banned) or EXPIRED
- Manual IP whitelisting support
- Sorted by ban status and expiration time

✅ **Administration**
- Manual IP removal from block list
- IP whitelisting UI
- Service restart scripts
- Firewall rule manual update option

## Troubleshooting

### Service Won't Start

1. Check permissions - must run as Administrator
2. Review logs: `C:\ProgramData\RDPSecurityService\service.log`
3. Verify WinService.exe not corrupted: `certutil -hashfile WinService.exe SHA256`

### Firewall Rule Not Updating

1. Run manual update: `.\update_firewall.ps1` (as Administrator)
2. Restart service: `restart_service_admin.bat`
3. Check service logs for errors

### Monitor Can't Connect

1. Verify service is running: `sc query RDPSecurityService`
2. Check file permissions on `C:\ProgramData\RDPSecurityService\`
3. Ensure monitor has read access to log files

## Support

For issues:
1. Check service logs
2. Review Windows Event Viewer (Security log)
3. Verify config.json syntax
4. Test firewall rule manually with netsh

---

**Installation Date**: 2026-03-01 04:59:41  
**Deployed By**: Release Build Script  
**Service Account**: SYSTEM

