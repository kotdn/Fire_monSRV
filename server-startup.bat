@echo off
REM ============================================
REM SERVER STARTUP SCRIPT - WINDOWS SERVICE
REM ASP.NET Core 8.0 Web Application
REM ============================================

setlocal enabledelayedexpansion

echo.
echo ============================================
echo  SERVER STARTUP UTILITY
echo ============================================
echo.

REM Configuration
set APP_NAME=WebApp
set APP_PATH=C:\Apps\WebApp
set SERVICE_NAME=WebApp

REM Menu
echo Choose action:
echo.
echo 1. Start as Windows Service
echo 2. Stop Windows Service
echo 3. Restart Windows Service
echo 4. Install as Windows Service
echo 5. Uninstall Windows Service
echo 6. Run as Console (Debug)
echo 7. Check service status
echo 0. Exit
echo.

set /p choice="Enter choice (0-7): "

if "%choice%"=="1" goto start_service
if "%choice%"=="2" goto stop_service
if "%choice%"=="3" goto restart_service
if "%choice%"=="4" goto install_service
if "%choice%"=="5" goto uninstall_service
if "%choice%"=="6" goto run_console
if "%choice%"=="7" goto check_status
if "%choice%"=="0" exit /b 0

echo Invalid choice!
timeout /t 2
goto menu

:start_service
echo [INFO] Starting %SERVICE_NAME% service...
sc start %SERVICE_NAME%
if errorlevel 1 (
    echo [ERROR] Failed to start service
) else (
    echo [OK] Service started
)
timeout /t 2
goto end

:stop_service
echo [INFO] Stopping %SERVICE_NAME% service...
sc stop %SERVICE_NAME%
if errorlevel 1 (
    echo [ERROR] Failed to stop service
) else (
    echo [OK] Service stopped
)
timeout /t 2
goto end

:restart_service
echo [INFO] Restarting %SERVICE_NAME% service...
sc stop %SERVICE_NAME%
timeout /t 2
sc start %SERVICE_NAME%
echo [OK] Service restarted
timeout /t 2
goto end

:install_service
echo [INFO] Installing %SERVICE_NAME% as Windows Service...
if exist "%APP_PATH%\%APP_NAME%.exe" (
    sc create %SERVICE_NAME% binPath= "%APP_PATH%\%APP_NAME%.exe"
    sc description %SERVICE_NAME% "ASP.NET Core Web Application"
    echo [OK] Service installed
    echo [INFO] Starting service...
    sc start %SERVICE_NAME%
) else (
    echo [ERROR] Application exe not found at %APP_PATH%\%APP_NAME%.exe
)
timeout /t 2
goto end

:uninstall_service
echo [WARNING] Uninstalling %SERVICE_NAME% service...
echo [INFO] Stopping service first...
sc stop %SERVICE_NAME%
timeout /t 2
sc delete %SERVICE_NAME%
if errorlevel 1 (
    echo [ERROR] Failed to uninstall service
) else (
    echo [OK] Service uninstalled
)
timeout /t 2
goto end

:run_console
echo [INFO] Running as console application...
echo [INFO] Press Ctrl+C to stop
echo.
cd /d "%APP_PATH%"
"%APP_PATH%\%APP_NAME%.exe"
goto end

:check_status
echo [INFO] Checking %SERVICE_NAME% status...
sc query %SERVICE_NAME%
echo.
echo [INFO] Checking if listening on port 5000...
netstat -ano | findstr ":5000"
timeout /t 2
goto end

:end
echo.
echo ============================================
echo  Done
echo ============================================
echo.
pause
