@echo off
setlocal

net session >nul 2>&1
if %errorlevel% neq 0 (
  echo ERROR: Run this as Administrator.
  pause
  exit /b 1
)

set "SVC=RDPSecurityService"
sc.exe stop "%SVC%"
timeout /t 2 /nobreak >nul
sc.exe query "%SVC%"
pause
