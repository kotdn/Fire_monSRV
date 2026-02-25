@echo off
REM ==========================================
REM DEPLOYMENT SCRIPT - BATCH VERSION
REM ASP.NET Core 8.0 Web Application
REM ==========================================

setlocal enabledelayedexpansion

echo.
echo ======================================
echo  DEPLOYMENT SCRIPT
echo ======================================
echo.

REM Configuration
set ProjectFile=.\WebApp\WebApp.csproj
set OutputPath=.\artifacts\deploy
set PublishFolder=!OutputPath!\WebApp
set Environment=Release

REM Check if dotnet is available
where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet CLI not found. Please install .NET 8.0 SDK
    pause
    exit /b 1
)

REM Step 1: Clean
echo [STEP 1] Cleaning...
if exist "!OutputPath!" (
    rmdir /s /q "!OutputPath!"
)
mkdir "!OutputPath!"
echo [OK] Cleaned

REM Step 2: Restore
echo [STEP 2] Restoring NuGet packages...
dotnet restore "!ProjectFile!"
if errorlevel 1 (
    echo [ERROR] Restore failed!
    pause
    exit /b 1
)
echo [OK] Restored

REM Step 3: Build
echo [STEP 3] Building...
dotnet build "!ProjectFile!" -c !Environment! --no-restore
if errorlevel 1 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)
echo [OK] Built

REM Step 4: Publish
echo [STEP 4] Publishing...
dotnet publish "!ProjectFile!" -c !Environment! -o "!PublishFolder!" ^
    --self-contained -r win-x64 ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    --no-build
if errorlevel 1 (
    echo [ERROR] Publish failed!
    pause
    exit /b 1
)
echo [OK] Published

REM Step 5: Create archive
echo [STEP 5] Creating deployment archive...
for /f "tokens=2-4 delims=/ " %%a in ('date /t') do (set mydate=%%c%%a%%b)
for /f "tokens=1-2 delims=/:" %%a in ('time /t') do (set mytime=%%a%%b)
set ZipName=WebApp-!mydate!-!mytime!.zip

powershell -Command "Compress-Archive -Path '!PublishFolder!' -DestinationPath '!OutputPath!\!ZipName!' -Force"
echo [OK] Archive created: !OutputPath!\!ZipName!

echo.
echo ======================================
echo  DEPLOYMENT READY!
echo ======================================
echo.
echo Published files: !PublishFolder!
echo Archive: !OutputPath!\!ZipName!
echo.
echo Next steps:
echo 1. Copy contents of: !PublishFolder!
echo 2. Paste to server: C:\Apps\WebApp
echo 3. Run as Windows Service or IIS
echo 4. See DEPLOYMENT.md for details
echo.
pause
