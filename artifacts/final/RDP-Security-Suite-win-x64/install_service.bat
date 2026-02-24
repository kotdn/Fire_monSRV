@echo off
setlocal
cd /d "%~dp0"

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo ERROR: Run this as Administrator.
  pause
  exit /b 1
)

set "SVC=RDPSecurityService"
set "EXE=%~dp0WinService.exe"

if not exist "%EXE%" (
  echo ERROR: "%EXE%" not found.
  pause
  exit /b 1
)

sc.exe query "%SVC%" >nul 2>&1
if %errorlevel%==0 (
  echo Service already exists. Reinstalling...
  sc.exe stop "%SVC%" >nul 2>&1
  timeout /t 2 /nobreak >nul
  for /f "tokens=2 delims=:" %%A in ('sc.exe queryex "%SVC%" ^| findstr /i "PID"') do set "PID=%%A"
  set "PID=%PID: =%"
  if not "%PID%"=="" if not "%PID%"=="0" taskkill /PID %PID% /F >nul 2>&1
  sc.exe delete "%SVC%" >nul 2>&1
  timeout /t 1 /nobreak >nul
)

echo Installing service...
sc.exe create "%SVC%" binPath= "\"%EXE%\"" DisplayName= "RDP Security Service - Auth Failures Blocker" start= auto
if %errorlevel% neq 0 (
  echo ERROR: sc create failed.
  pause
  exit /b 1
)

sc.exe failure "%SVC%" reset= 0 actions= restart/5000 >nul 2>&1

echo Starting service...
sc.exe start "%SVC%"
sc.exe query "%SVC%"

echo.
echo Logs folder: C:\ProgramData\RDPSecurityService
pause
