@echo off
REM RDP Security Service - Quick Deploy Batch Script
REM Run as Administrator!

setlocal enabledelayedexpansion

REM Check Administrator privileges
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: This script requires Administrator privileges!
    echo Please run Command Prompt as Administrator.
    pause
    exit /b 1
)

set SERVICE_PATH=C:\ProgramData\RDPSecurityService

REM Get RDP port from parameter or use default
if "%~1"=="" (
    set RDP_PORT=3389
) else (
    set RDP_PORT=%~1
)

echo.
echo =========================================
echo  RDP Security Service - Deployment
echo  Monitoring Port: %RDP_PORT%
echo =========================================
echo.

REM Step 1: Stop existing service
echo [1/5] Stopping existing service...
net stop RDPSecurityService >nul 2>&1
if errorlevel 1 (
    echo ℹ Service not running or not installed
) else (
    echo ✓ Service stopped
)

timeout /t 1 /nobreak >nul

REM Step 2: Create directory
echo.
echo [2/5] Creating service directory...
if not exist "%SERVICE_PATH%" (
    mkdir "%SERVICE_PATH%"
    echo ✓ Directory created: %SERVICE_PATH%
) else (
    echo ✓ Directory already exists
)

REM Step 3: Copy files
echo.
echo [3/5] Copying files...
xcopy /E /I /Y "WinService" "%SERVICE_PATH%" >nul 2>&1
if errorlevel 1 (
    echo ✗ Error copying WinService files
    pause
    exit /b 1
)
echo ✓ WinService copied

xcopy /E /I /Y "RDPSecurityViewer" "%SERVICE_PATH%" >nul 2>&1
if errorlevel 1 (
    echo ✗ Error copying RDPSecurityViewer files
    pause
    exit /b 1
)
echo ✓ RDPSecurityViewer copied

REM Step 4: Create config.json
echo.
echo [4/5] Creating configuration...
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
echo ✓ Config created: %SERVICE_PATH%\config.json

REM Step 5: Register and start service
echo.
echo [5/5] Installing and starting service...

REM Remove old service
sc stop RDPSecurityService >nul 2>&1
sc delete RDPSecurityService >nul 2>&1
timeout /t 1 /nobreak >nul

REM Create new service
sc create RDPSecurityService binPath= "%SERVICE_PATH%\WinService.exe" DisplayName= "RDP Security Service" >nul 2>&1
if errorlevel 1 (
    echo ✗ Error creating service
    pause
    exit /b 1
)
echo ✓ Service registered

REM Set service to auto start
sc config RDPSecurityService start= auto >nul 2>&1

REM Start service
net start RDPSecurityService >nul 2>&1
if errorlevel 1 (
    echo ⚠ Service registered but failed to start
    echo   Check: %SERVICE_PATH%\service.log
) else (
    echo ✓ Service started
)

echo.
echo =========================================
echo ✓ DEPLOYMENT COMPLETE
echo =========================================
echo.
echo Service Directory: %SERVICE_PATH%
echo Config File: %SERVICE_PATH%\config.json
echo.
echo To launch monitor GUI:
echo   %SERVICE_PATH%\RDPSecurityViewer.exe
echo.
echo To check service status:
echo   sc query RDPSecurityService
echo.
pause
