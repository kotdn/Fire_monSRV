@echo off
setlocal

set "SERVICE_NAME=RDPSecurityService"

net session >nul 2>&1
if not %errorlevel%==0 (
  echo [ERROR] Run as Administrator.
  exit /b 1
)

sc.exe stop "%SERVICE_NAME%" >nul 2>&1
sc.exe delete "%SERVICE_NAME%"

echo Done.
exit /b 0
