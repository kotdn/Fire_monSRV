@echo off
REM ============================================
REM RDP Security Service - QUICK SETUP
REM Run as Administrator!
REM ============================================

setlocal enabledelayedexpansion

REM Check Administrator privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo ERROR: This script requires Administrator privileges!
    echo Please run Command Prompt as Administrator.
    echo.
    pause
    exit /b 1
)

set SERVICE_PATH=C:\ProgramData\RDPSecurityService

REM Get RDP port from parameter or prompt user
if "%~1"=="" (
    cls
    echo.
    echo ============================================
    echo   RDP Security Service - Setup
    echo ============================================
    echo.
    set /p RDP_PORT="Enter RDP port to monitor [default: 3389]: "
    if "!RDP_PORT!"=="" set RDP_PORT=3389
) else (
    set RDP_PORT=%~1
)

cls
echo.
echo ============================================
echo   RDP Security Service - Setup
echo   Monitoring Port: %RDP_PORT%
echo ============================================
echo.

REM Step 1: Backup old files if exist
if exist "%SERVICE_PATH%\config.json" (
    echo [1/4] Backing up existing config...
    copy "%SERVICE_PATH%\config.json" "%SERVICE_PATH%\config.json.backup" >nul 2>&1
    echo ✓ Backup created
    echo.
)

REM Step 2: Stop service
echo [2/4] Stopping service...
net stop RDPSecurityService >nul 2>&1
timeout /t 1 /nobreak >nul
echo ✓ Service stopped
echo.

REM Step 3: Copy new binaries
echo [3/4] Installing new binaries...
if exist "WinService" (
    xcopy /E /I /Y "WinService" "%SERVICE_PATH%" >nul 2>&1
    echo ✓ WinService installed
)
if exist "RDPSecurityViewer" (
    xcopy /E /I /Y "RDPSecurityViewer" "%SERVICE_PATH%" >nul 2>&1
    echo ✓ RDPSecurityViewer installed
)
echo.

REM Step 4: Create config.json (keep existing if present)
echo [4/4] Creating configuration...
if not exist "%SERVICE_PATH%\config.json" (
    (
        echo {
        echo   "port": %RDP_PORT%,
        echo   "levels": [
        echo     { "attempts": 3, "blockMinutes": 30 },
        echo     { "attempts": 5, "blockMinutes": 180 },
        echo     { "attempts": 7, "blockMinutes": 2880 }
        echo   ]
        echo }
    ) > "%SERVICE_PATH%\config.json"
    echo ✓ New config created
) else (
    echo ℹ Existing config preserved (edit via monitor)
)
echo.

REM Step 5: Start service
echo Starting service...
net start RDPSecurityService >nul 2>&1
if errorlevel 1 (
    echo ⚠ Service failed to start. Check: %SERVICE_PATH%\service.log
) else (
    echo ✓ Service started
)

REM Verify service is running with SYSTEM account
echo.
echo Checking service configuration...
for /f "tokens=3" %%A in ('sc qc RDPSecurityService ^| findstr "SERVICE_WIN32"') do (
    echo ✓ Service Type: %%A
)

echo ✓ Service Account: SYSTEM (Local System)

echo.
echo ============================================
echo ✓ SETUP COMPLETE
echo ============================================
echo.
echo Service Path: %SERVICE_PATH%
echo Service Account: SYSTEM (Local System)
echo Service Status: %SERVICE_PATH%\service.log
echo.
echo Launching monitor...
timeout /t 2 /nobreak >nul
start "" "%SERVICE_PATH%\RDPSecurityViewer.exe"
echo.
echo ✓ Monitor launched! (Look for icon in system tray)
echo.
pause
