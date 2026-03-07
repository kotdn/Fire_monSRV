===============================================
   RDP Security Suite - Release Package
   Version: 2026-03-07
===============================================

COMPONENTS:
-----------
1. WinService.exe         - Windows Service (RDPSecurityService)
2. RDPMonitor.exe         - Monitoring UI Application
3. config.example.json    - Configuration template

INSTALLATION:
-------------
1. Run install_service.bat as Administrator
2. Edit config at: C:\ProgramData\RDPSecurityService\config.json
3. Run RDPMonitor.exe to manage service and view logs

MANAGEMENT SCRIPTS (Run as Administrator):
------------------------------------------
- install_service.bat     - Install RDPSecurityService
- start_service.bat       - Start service
- stop_service.bat        - Stop service  
- restart_all.bat         - Restart service and monitor
- uninstall_service.bat   - Remove service

CONFIGURATION:
--------------
Config location: C:\ProgramData\RDPSecurityService\config.json

Example configuration:
{
  "rdpPort": 3389,
  "blockLevels": [
    { "attempts": 1, "blockMinutes": 20 },
    { "attempts": 2, "blockMinutes": 120 },
    { "attempts": 7, "blockMinutes": 240 }
  ],
  "telegram": {
    "enabled": false,
    "botToken": "",
    "chatId": ""
  }
}

TELEGRAM NOTIFICATIONS:
-----------------------
To enable Telegram alerts:
1. Open RDPMonitor.exe
2. Go to "Налаштування" (Settings) tab
3. Enable Telegram and enter Bot Token + Chat ID
4. Test connection and save

The service will send notifications for:
- Service start/stop
- IP blocking events
- Failed authentication attempts
- Manual actions from Monitor UI

LOGS:
-----
All logs are stored in: C:\ProgramData\RDPSecurityService\

- service.log       - Service activity log
- access.log        - Authentication attempts
- block_list.log    - Currently blocked IPs
- whiteList.log     - Whitelisted IPs (never blocked)

MONITOR FEATURES:
-----------------
- Real-time log viewing
- Service management (start/stop/restart)
- IP blocking/unblocking
- Whitelist management  
- Manual IP blocking with custom duration
- Configuration editor
- Telegram integration
- Language support (UA/EN)

FIREWALL RULES:
---------------
The service creates a Windows Firewall rule named "RDP_BLOCK_ALL"
to block IPs on port 3389 (or configured RDP port).

REQUIREMENTS:
-------------
- Windows Server 2016+ or Windows 10/11
- .NET 8.0 Runtime (or later)
- Administrator privileges for service installation
- PowerShell for advanced management

SUPPORT:
--------
Check service status: Run check_status.bat (if available)
View logs: Use RDPMonitor.exe or check files directly

===============================================
