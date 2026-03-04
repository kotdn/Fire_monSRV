@echo off
REM Quick Status Check for RDP Security Service
REM No admin rights required

setlocal enabledelayedexpansion

cls
echo.
echo ========================================
echo  RDP Security Service - Quick Status
echo ========================================
echo.

REM Service Status
echo [1/5] Service Status:
sc query RDPSecurityService >nul 2>&1
if errorlevel 1 (
    echo   ✗ Service NOT INSTALLED
    goto :end
)

for /f "tokens=3" %%a in ('sc query RDPSecurityService ^| find "STATE"') do (
    if "%%a"=="RUNNING" (
        echo   ✓ RUNNING
    ) else (
        echo   ✗ STOPPED
    )
)
echo.

REM Config Port
echo [2/5] Monitoring Port:
if exist "C:\ProgramData\RDPSecurityService\config.json" (
    for /f "tokens=2 delims=:," %%p in ('type "C:\ProgramData\RDPSecurityService\config.json" ^| find "port"') do (
        echo   Port: %%p
    )
) else (
    echo   ✗ Config file not found
)
echo.

REM Active Blocks
echo [3/5] Blocked IPs:
if exist "C:\ProgramData\RDPSecurityService\Logs\block_list.log" (
    for /f %%c in ('find /c "ACTIVE" "C:\ProgramData\RDPSecurityService\Logs\block_list.log"') do (
        echo   Active blocks: %%c
    )
) else (
    echo   ✗ Block list not found
)
echo.

REM Firewall Rule
echo [4/5] Firewall Rule:
netsh advfirewall firewall show rule name="RDP_BLOCK_ALL" >nul 2>&1
if errorlevel 1 (
    echo   ✗ Not configured
) else (
    echo   ✓ Configured
)
echo.

REM Last Activity
echo [5/5] Last Activity:
if exist "C:\ProgramData\RDPSecurityService\Logs\access.log" (
    for /f "tokens=*" %%l in ('powershell -NoProfile -Command "Get-Content C:\ProgramData\RDPSecurityService\Logs\access.log -Tail 1"') do (
        echo   %%l
    )
) else (
    echo   ✗ No activity log found
)
echo.

:end
echo ========================================
echo.
pause
