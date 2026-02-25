# ============================================
# SERVER STARTUP SCRIPT - POWERSHELL VERSION
# ASP.NET Core 8.0 Web Application
# ============================================

param(
    [ValidateSet("start", "stop", "restart", "install", "uninstall", "console", "status")]
    [string]$Action = "menu",
    [string]$AppPath = "C:\Apps\WebApp",
    [string]$ServiceName = "WebApp"
)

function Show-Menu {
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "  SERVER STARTUP UTILITY" -ForegroundColor Cyan
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Choose action:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Start service" -ForegroundColor White
    Write-Host "  2. Stop service" -ForegroundColor White
    Write-Host "  3. Restart service" -ForegroundColor White
    Write-Host "  4. Install as service" -ForegroundColor White
    Write-Host "  5. Uninstall service" -ForegroundColor White
    Write-Host "  6. Run as console (Debug)" -ForegroundColor White
    Write-Host "  7. Check status" -ForegroundColor White
    Write-Host "  8. View logs" -ForegroundColor White
    Write-Host "  0. Exit" -ForegroundColor White
    Write-Host ""
    $choice = Read-Host "Enter choice (0-8)"
    return $choice
}

function Start-AppService {
    Write-Host "[INFO] Starting $ServiceName service..." -ForegroundColor Yellow
    Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($?) {
        Write-Host "[OK] Service started" -ForegroundColor Green
        Start-Sleep -Seconds 2
        
        Write-Host "[INFO] Checking status..." -ForegroundColor Yellow
        Get-Service -Name $ServiceName | Format-Table -AutoSize
    } else {
        Write-Host "[ERROR] Failed to start service" -ForegroundColor Red
        Write-Host "Service might not exist. Run: 5. Uninstall / 4. Install" -ForegroundColor Yellow
    }
}

function Stop-AppService {
    Write-Host "[INFO] Stopping $ServiceName service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    
    if ($?) {
        Write-Host "[OK] Service stopped" -ForegroundColor Green
    } else {
        Write-Host "[ERROR] Failed to stop service" -ForegroundColor Red
    }
    Start-Sleep -Seconds 2
}

function Restart-AppService {
    Write-Host "[INFO] Restarting $ServiceName service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Start-Service -Name $ServiceName -ErrorAction SilentlyContinue
    Write-Host "[OK] Service restarted" -ForegroundColor Green
    Start-Sleep -Seconds 2
}

function Install-AppService {
    $exePath = Join-Path $AppPath "WebApp.exe"
    
    if (!(Test-Path $exePath)) {
        Write-Host "[ERROR] Application exe not found at $exePath" -ForegroundColor Red
        Write-Host "[INFO] Copy published files to $AppPath first" -ForegroundColor Yellow
        Start-Sleep -Seconds 3
        return
    }
    
    Write-Host "[INFO] Installing $ServiceName as Windows Service..." -ForegroundColor Yellow
    Write-Host "[INFO] Application path: $exePath" -ForegroundColor Cyan
    
    # Check if service already exists
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($service) {
        Write-Host "[WARNING] Service already exists" -ForegroundColor Yellow
        $confirm = Read-Host "Reinstall? (Y/N)"
        if ($confirm -eq "Y" -or $confirm -eq "y") {
            Write-Host "[INFO] Stopping existing service..." -ForegroundColor Yellow
            Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
            Write-Host "[INFO] Removing existing service..." -ForegroundColor Yellow
            Remove-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 2
        } else {
            return
        }
    }
    
    # Create new service
    try {
        New-Service -Name $ServiceName `
                    -BinaryPathName $exePath `
                    -DisplayName "WebApp ASP.NET Core" `
                    -Description "ASP.NET Core 8.0 Web Application" `
                    -StartupType Automatic `
                    -ErrorAction Stop
        
        Write-Host "[OK] Service installed" -ForegroundColor Green
        
        # Start service
        Write-Host "[INFO] Starting service..." -ForegroundColor Yellow
        Start-Service -Name $ServiceName
        Start-Sleep -Seconds 3
        
        Write-Host "[OK] Service started" -ForegroundColor Green
        Write-Host ""
        Write-Host "Service Details:" -ForegroundColor Cyan
        Get-Service -Name $ServiceName | Format-Table -AutoSize
        
    } catch {
        Write-Host "[ERROR] Failed to install service: $_" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds 3
}

function Uninstall-AppService {
    Write-Host "[WARNING] Uninstalling $ServiceName service..." -ForegroundColor Yellow
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    if (!$service) {
        Write-Host "[ERROR] Service not found" -ForegroundColor Red
        Start-Sleep -Seconds 2
        return
    }
    
    $confirm = Read-Host "Are you sure? (Y/N)"
    if ($confirm -ne "Y" -and $confirm -ne "y") {
        Write-Host "[INFO] Cancelled" -ForegroundColor Yellow
        return
    }
    
    Write-Host "[INFO] Stopping service..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    Write-Host "[INFO] Removing service..." -ForegroundColor Yellow
    try {
        Remove-Service -Name $ServiceName -Force -ErrorAction Stop
        Write-Host "[OK] Service uninstalled" -ForegroundColor Green
    } catch {
        Write-Host "[ERROR] Failed to uninstall: $_" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds 2
}

function Run-Console {
    Write-Host "[INFO] Running as console application..." -ForegroundColor Yellow
    Write-Host "[INFO] Press Ctrl+C to stop" -ForegroundColor Cyan
    Write-Host ""
    
    $exePath = Join-Path $AppPath "WebApp.exe"
    
    if (!(Test-Path $exePath)) {
        Write-Host "[ERROR] Application exe not found at $exePath" -ForegroundColor Red
        Start-Sleep -Seconds 2
        return
    }
    
    Set-Location $AppPath
    & $exePath
}

function Check-Status {
    Write-Host "[INFO] Checking $ServiceName status..." -ForegroundColor Yellow
    Write-Host ""
    
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if ($service) {
        Write-Host "Service Status:" -ForegroundColor Cyan
        $service | Format-Table -AutoSize
        Write-Host ""
    } else {
        Write-Host "[WARNING] Service not found" -ForegroundColor Yellow
    }
    
    Write-Host "Port Check (5000):" -ForegroundColor Cyan
    $port = netstat -ano | Select-String ":5000"
    
    if ($port) {
        Write-Host $port -ForegroundColor Green
    } else {
        Write-Host "[INFO] Port 5000 is not in use" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Application Check:" -ForegroundColor Cyan
    
    try {
        $response = Invoke-WebRequest http://localhost:5000 -UseBasicParsing -TimeoutSec 2
        Write-Host "[OK] Application is responding (HTTP $($response.StatusCode))" -ForegroundColor Green
    } catch {
        Write-Host "[WARNING] Application not responding" -ForegroundColor Yellow
    }
    
    Start-Sleep -Seconds 2
}

function View-Logs {
    $logPath = Join-Path $AppPath "logs"
    
    if (!(Test-Path $logPath)) {
        Write-Host "[INFO] Logs folder not found at $logPath" -ForegroundColor Yellow
        Write-Host "[INFO] Logs may be in: C:\Windows\System32\winevt\logs\" -ForegroundColor Cyan
        Start-Sleep -Seconds 2
        return
    }
    
    Write-Host "[INFO] Latest logs:" -ForegroundColor Yellow
    Get-ChildItem $logPath | Sort-Object LastWriteTime -Descending | Select-Object -First 10 | Format-Table Name, LastWriteTime, Length
    
    $lastLog = Get-ChildItem $logPath | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($lastLog) {
        Write-Host ""
        Write-Host "[INFO] Viewing: $($lastLog.Name)" -ForegroundColor Cyan
        Get-Content $lastLog.FullName -Tail 50
    }
    
    Start-Sleep -Seconds 3
}

# Main execution
if ($Action -eq "menu") {
    while ($true) {
        $choice = Show-Menu
        
        switch ($choice) {
            "1" { Start-AppService }
            "2" { Stop-AppService }
            "3" { Restart-AppService }
            "4" { Install-AppService }
            "5" { Uninstall-AppService }
            "6" { Run-Console; break }
            "7" { Check-Status }
            "8" { View-Logs }
            "0" { Write-Host "[INFO] Exiting..."; break }
            default { Write-Host "[ERROR] Invalid choice" -ForegroundColor Red; Start-Sleep -Seconds 1 }
        }
    }
} else {
    # Run with command line parameter
    switch ($Action.ToLower()) {
        "start" { Start-AppService }
        "stop" { Stop-AppService }
        "restart" { Restart-AppService }
        "install" { Install-AppService }
        "uninstall" { Uninstall-AppService }
        "console" { Run-Console }
        "status" { Check-Status }
        default { Write-Host "[ERROR] Invalid action: $Action" -ForegroundColor Red }
    }
}
