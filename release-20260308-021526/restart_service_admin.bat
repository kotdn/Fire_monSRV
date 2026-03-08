@echo off
setlocal
REM Administrator restart script for RDP Security Service with firewall update
REM This script restarts the RDP Security Service and updates firewall rules

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Requesting administrator privileges...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

echo RDP Security Service Restart Script
echo ====================================
echo.

REM Stop the service
echo Stopping RDPSecurityService...
net stop RDPSecurityService
if errorlevel 1 (
    echo Warning: Could not stop service via net command
)

echo Waiting for process to terminate...
timeout /t 3 /nobreak >nul

REM Start the service
echo Starting RDPSecurityService...
net start RDPSecurityService
if errorlevel 1 (
    echo ERROR: Could not start RDPSecurityService.
    echo If you see "Client does not possess required privilege", run from elevated shell.
    pause
    exit /b 1
)

echo.
echo Checking service status...
sc query RDPSecurityService

echo.
echo Service restart complete!
echo Waiting 2 seconds then checking firewall rule...
timeout /t 2 /nobreak >nul

echo.
echo Current firewall rule status:
netsh advfirewall firewall show rule name="BlockRDP_IPs"

echo.
echo Done!
pause
endlocal
