# RDP Security Service - Installation Script
# UTF-8 Support enabled for all operations
# Version: 2026-03-06

Write-Host "=== RDP Security Service Installer ===" -ForegroundColor Cyan
Write-Host "Version: 2026-03-06" -ForegroundColor Gray
Write-Host ""

# Check admin rights
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    pause
    exit 1
}

$serviceName = "RDPSecurityService"
$serviceDisplayName = "RDP Security Service - Auth Failures Blocker"
$installPath = "C:\Program Files\RDPSecurityService"
$dataPath = "C:\ProgramData\RDPSecurityService"

Write-Host "Installation paths:" -ForegroundColor Green
Write-Host "  Service: $installPath" -ForegroundColor Gray
Write-Host "  Data:    $dataPath" -ForegroundColor Gray
Write-Host ""

# Stop existing service if running
try {
    $existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "Stopping existing service..." -ForegroundColor Yellow
        Stop-Service -Name $serviceName -Force -ErrorAction Stop
        Start-Sleep -Seconds 2
    }
} catch {
    Write-Host "Warning: Could not stop service: $_" -ForegroundColor Yellow
}

# Create directories
Write-Host "Creating directories..." -ForegroundColor Green
New-Item -ItemType Directory -Path $installPath -Force | Out-Null
New-Item -ItemType Directory -Path $dataPath -Force | Out-Null

# Copy service files
Write-Host "Copying service files..." -ForegroundColor Green
Copy-Item ".\WinService\*" -Destination $installPath -Recurse -Force

# Copy monitor files to a subdirectory
Write-Host "Copying monitor files..." -ForegroundColor Green
$monitorPath = Join-Path $installPath "Monitor"
New-Item -ItemType Directory -Path $monitorPath -Force | Out-Null
Copy-Item ".\Monitor\*" -Destination $monitorPath -Recurse -Force

# Create default config if not exists
$configPath = Join-Path $dataPath "config.json"
if (-not (Test-Path $configPath)) {
    Write-Host "Creating default configuration..." -ForegroundColor Green
    $defaultConfig = @{
        port = 3389
        levels = @(
            @{ attempts = 1; blockMinutes = 20 }
            @{ attempts = 2; blockMinutes = 128 }
            @{ attempts = 7; blockMinutes = 240 }
            @{ attempts = 10; blockMinutes = 1440 }
        )
    }
    $defaultConfig | ConvertTo-Json -Depth 10 | Out-File -FilePath $configPath -Encoding UTF8
}

# Install/Update service
Write-Host "Installing service..." -ForegroundColor Green
$exePath = Join-Path $installPath "WinService.exe"

try {
    $existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "Service already installed, updating..." -ForegroundColor Yellow
    } else {
        sc.exe create $serviceName binPath= "`"$exePath`"" DisplayName= "$serviceDisplayName" start= auto | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Service created successfully" -ForegroundColor Green
        } else {
            throw "Failed to create service (exit code: $LASTEXITCODE)"
        }
    }
} catch {
    Write-Host "ERROR: Failed to install service: $_" -ForegroundColor Red
    pause
    exit 1
}

# Start service
Write-Host "Starting service..." -ForegroundColor Green
try {
    Start-Service -Name $serviceName -ErrorAction Stop
    Write-Host "Service started successfully!" -ForegroundColor Green
} catch {
    Write-Host "ERROR: Failed to start service: $_" -ForegroundColor Red
    Write-Host "You may need to start it manually using 'net start $serviceName'" -ForegroundColor Yellow
}

# Create desktop shortcut for monitor
Write-Host "Creating desktop shortcut..." -ForegroundColor Green
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "RDP Monitor.lnk"
$WScriptShell = New-Object -ComObject WScript.Shell
$shortcut = $WScriptShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $monitorPath "RDPMonitor.exe"
$shortcut.WorkingDirectory = $monitorPath
$shortcut.Description = "RDP Security Monitor - View blocked IPs and logs"
$shortcut.Save()

Write-Host ""
Write-Host "=== Installation Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Service installed at: $installPath" -ForegroundColor Cyan
Write-Host "Monitor located at:   $monitorPath\RDPMonitor.exe" -ForegroundColor Cyan
Write-Host "Data directory:       $dataPath" -ForegroundColor Cyan
Write-Host "Desktop shortcut:     $shortcutPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Features:" -ForegroundColor Yellow
Write-Host "  ✓ Real-time RDP brute-force protection (1-second interval)" -ForegroundColor White
Write-Host "  ✓ Multi-level blocking system" -ForegroundColor White
Write-Host "  ✓ GUI monitor with UA/EN localization" -ForegroundColor White
Write-Host "  ✓ Automatic firewall rule management" -ForegroundColor White
Write-Host "  ✓ White-list support" -ForegroundColor White
Write-Host ""
Write-Host "To launch the monitor, run:" -ForegroundColor Yellow
Write-Host "  $monitorPath\RDPMonitor.exe" -ForegroundColor White
Write-Host "  OR use the desktop shortcut" -ForegroundColor White
Write-Host ""
pause
