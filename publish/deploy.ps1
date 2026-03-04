# RDP Security Service - Automated Deployment Script
# Run as Administrator!

param(
    [string]$ServicePath = "C:\ProgramData\RDPSecurityService",
    [int]$RDPPort = 3389
)

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    Write-Host "ERROR: This script requires Administrator privileges!" -ForegroundColor Red
    exit 1
}

Write-Host "RDP Security Service - Deployment" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Step 1: Stop existing service
Write-Host "`n[1/6] Stopping existing service..." -ForegroundColor Yellow
try {
    $service = Get-Service -Name "RDPSecurityService" -ErrorAction SilentlyContinue
    if ($service) {
        Stop-Service -Name "RDPSecurityService" -Force -ErrorAction SilentlyContinue
        Write-Host "✓ Service stopped" -ForegroundColor Green
    } else {
        Write-Host "ℹ Service not found, skipping stop" -ForegroundColor Cyan
    }
} catch {
    Write-Host "✓ Service already stopped or not installed" -ForegroundColor Green
}

# Step 2: Create service directory
Write-Host "`n[2/6] Creating service directory..." -ForegroundColor Yellow
if (-not (Test-Path $ServicePath)) {
    New-Item -ItemType Directory -Path $ServicePath -Force | Out-Null
    Write-Host "✓ Directory created: $ServicePath" -ForegroundColor Green
} else {
    Write-Host "✓ Directory already exists: $ServicePath" -ForegroundColor Green
}

# Step 3: Copy binaries
Write-Host "`n[3/6] Copying binaries..." -ForegroundColor Yellow
try {
    # Copy WinService
    Copy-Item -Path ".\WinService\*" -Destination $ServicePath -Recurse -Force -ErrorAction Stop
    Write-Host "✓ WinService files copied" -ForegroundColor Green
    
    # Copy RDPSecurityViewer
    Copy-Item -Path ".\RDPSecurityViewer\*" -Destination $ServicePath -Recurse -Force -ErrorAction Stop
    Write-Host "✓ RDPSecurityViewer files copied" -ForegroundColor Green
} catch {
    Write-Host "✗ Error copying files: $_" -ForegroundColor Red
    exit 1
}

# Step 4: Create/Update config.json
Write-Host "`n[4/6] Creating configuration..." -ForegroundColor Yellow
$configPath = Join-Path $ServicePath "config.json"
$config = @{
    port = $RDPPort
    levels = @(
        @{ attempts = 3; blockMinutes = 30 },
        @{ attempts = 5; blockMinutes = 180 },
        @{ attempts = 7; blockMinutes = 2880 }
    )
}

$config | ConvertTo-Json | Set-Content -Path $configPath -Force
Write-Host "✓ Config created: $configPath" -ForegroundColor Green

# Step 5: Register and start service
Write-Host "`n[5/6] Registering Windows Service..." -ForegroundColor Yellow
$servicePath = Join-Path $ServicePath "WinService.exe"

# Remove old service if exists
$existingService = Get-Service -Name "RDPSecurityService" -ErrorAction SilentlyContinue
if ($existingService) {
    sc.exe delete RDPSecurityService | Out-Null
    Start-Sleep -Seconds 1
    Write-Host "✓ Old service removed" -ForegroundColor Green
}

# Create new service
try {
    New-Service -Name "RDPSecurityService" `
        -BinaryPathName $servicePath `
        -DisplayName "RDP Security Service" `
        -Description "Monitors and blocks RDP brute force attacks" `
        -StartupType Automatic `
        -ErrorAction Stop | Out-Null
    
    Write-Host "✓ Service registered" -ForegroundColor Green
} catch {
    Write-Host "✗ Error registering service: $_" -ForegroundColor Red
    exit 1
}

# Start service
Write-Host "`n[6/6] Starting service..." -ForegroundColor Yellow
try {
    Start-Service -Name "RDPSecurityService" -ErrorAction Stop
    Start-Sleep -Seconds 2
    
    $serviceStatus = (Get-Service -Name "RDPSecurityService").Status
    if ($serviceStatus -eq "Running") {
        Write-Host "✓ Service started successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠ Service registered but not running. Status: $serviceStatus" -ForegroundColor Yellow
    }
} catch {
    Write-Host "✗ Error starting service: $_" -ForegroundColor Red
    exit 1
}

# Summary
Write-Host "`n=================================" -ForegroundColor Green
Write-Host "✓ DEPLOYMENT COMPLETE" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green
Write-Host "`nService Details:" -ForegroundColor Cyan
Write-Host "  Name: RDPSecurityService"
Write-Host "  Path: $ServicePath"
Write-Host "  RDP Port: $RDPPort"
Write-Host "  Status: $(​(Get-Service -Name 'RDPSecurityService').Status)"
Write-Host "`nLog Files:" -ForegroundColor Cyan
Write-Host "  - $ServicePath\service.log"
Write-Host "  - $ServicePath\access.log"
Write-Host "  - $ServicePath\block_list.log"
Write-Host "`nTo launch monitor:" -ForegroundColor Cyan
Write-Host "  & '$ServicePath\RDPSecurityViewer.exe'"
Write-Host "`nTo view service:" -ForegroundColor Cyan
Write-Host "  Get-Service RDPSecurityService"
Write-Host "`n"
