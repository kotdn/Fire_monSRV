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

echo Stopping service...
sc.exe stop "%SVC%" >nul 2>&1
timeout /t 2 /nobreak >nul
for /f "tokens=2 delims=:" %%A in ('sc.exe queryex "%SVC%" ^| findstr /i "PID"') do set "PID=%%A"
set "PID=%PID: =%"
if not "%PID%"=="" if not "%PID%"=="0" taskkill /PID %PID% /F >nul 2>&1

echo Deleting service...
sc.exe delete "%SVC%" >nul 2>&1

echo Removing firewall rule (if any)...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Remove-NetFirewallRule -Name RDP_BLOCK_ALL -ErrorAction SilentlyContinue" >nul 2>&1

sc.exe query "%SVC%"
pause
