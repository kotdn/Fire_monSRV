@echo off
setlocal

set "SERVICE_NAME=RDPSecurityService"
set "DISPLAY_NAME=RDP Security Service - Auth Failures Blocker"
set "TARGET_DIR=%ProgramFiles%\RDPSecurityService"
set "TARGET_EXE=%TARGET_DIR%\WinService.exe"

net session >nul 2>&1
if not %errorlevel%==0 (
  echo [ERROR] Run as Administrator.
  exit /b 1
)

if not exist "%TARGET_DIR%" mkdir "%TARGET_DIR%"
copy /Y "%~dp0WinService.exe" "%TARGET_EXE%" >nul
if errorlevel 1 (
  echo [ERROR] Cannot copy WinService.exe
  exit /b 1
)

sc.exe query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel%==0 (
  sc.exe stop "%SERVICE_NAME%" >nul 2>&1
  sc.exe config "%SERVICE_NAME%" binPath= "\"%TARGET_EXE%\"" start= auto DisplayName= "\"%DISPLAY_NAME%\""
) else (
  sc.exe create "%SERVICE_NAME%" binPath= "\"%TARGET_EXE%\"" start= auto DisplayName= "\"%DISPLAY_NAME%\""
)

sc.exe start "%SERVICE_NAME%"
sc.exe query "%SERVICE_NAME%"

echo Done.
exit /b 0
