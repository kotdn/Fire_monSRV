@echo off
setlocal
cd /d "%~dp0"

REM Kill viewer so ?????????? ????????? ?? ????????? ? lock.
taskkill /IM RDPSecurityViewer.exe /F >nul 2>&1

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo NOTE: restart_all.bat needs Administrator to restart the service.
  echo Starting viewer only...
  start "RDPSecurityViewer" "%~dp0RDPSecurityViewer.exe"
  exit /b 0
)

set "SVC=RDPSecurityService"
sc.exe stop "%SVC%" >nul 2>&1
timeout /t 2 /nobreak >nul
for /f "tokens=2 delims=:" %%A in ('sc.exe queryex "%SVC%" ^| findstr /i "PID"') do set "PID=%%A"
set "PID=%PID: =%"
if not "%PID%"=="" if not "%PID%"=="0" taskkill /PID %PID% /F >nul 2>&1
sc.exe start "%SVC%" >nul 2>&1
timeout /t 1 /nobreak >nul
sc.exe query "%SVC%"

start "RDPSecurityViewer" "%~dp0RDPSecurityViewer.exe"
pause
