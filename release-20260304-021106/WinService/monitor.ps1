# Real-time monitor for RDPSecurityService
# Shows live service logs and current blocks

$ErrorActionPreference = "Continue"
$logDir = "C:\ProgramData\RDPSecurityService"
$serviceLog = Join-Path $logDir "service.log"
$accessLog = Join-Path $logDir "access.log"
$blockLog = Join-Path $logDir "block_list.log"

Clear-Host

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  RDP Security Service - Real-Time Monitor" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

# Check service status
$service = Get-Service -Name "RDPSecurityService" -ErrorAction SilentlyContinue
if ($null -eq $service) {
    Write-Host "[ERROR] Service not found!" -ForegroundColor Red
    exit 1
}

$statusColor = if ($service.Status -eq "Running") { "Green" } else { "Red" }
Write-Host "Service Status: " -NoNewline
Write-Host $service.Status -ForegroundColor $statusColor
Write-Host ""

# Show current configuration
$configPath = Join-Path $logDir "config.json"
if (Test-Path $configPath) {
    Write-Host "Configuration:" -ForegroundColor Yellow
    $config = Get-Content $configPath | ConvertFrom-Json
    Write-Host "  RDP Port: $($config.port)" -ForegroundColor Gray
    Write-Host "  Block Levels:" -ForegroundColor Gray
    foreach ($level in $config.levels) {
        Write-Host "    - $($level.attempts) attempt(s) -> block $($level.blockMinutes) min" -ForegroundColor Gray
    }
    Write-Host ""
}

# Show current firewall blocks
Write-Host "Current Firewall Blocks:" -ForegroundColor Yellow
$fwRule = Get-NetFirewallRule -Name "RDP_BLOCK_ALL" -ErrorAction SilentlyContinue
if ($null -ne $fwRule) {
    $addressFilter = $fwRule | Get-NetFirewallAddressFilter
    $ips = $addressFilter.RemoteAddress -split ","
    if ($ips.Count -gt 0 -and $ips[0] -ne "Any") {
        foreach ($ip in $ips) {
            Write-Host "  [BLOCKED] $ip" -ForegroundColor Red
        }
    } else {
        Write-Host "  (no IPs blocked yet)" -ForegroundColor Gray
    }
} else {
    Write-Host "  (firewall rule not created yet)" -ForegroundColor Gray
}
Write-Host ""

# Show recent blocks
if (Test-Path $blockLog) {
    Write-Host "Recent Blocks (last 5):" -ForegroundColor Yellow
    $blocks = Get-Content $blockLog -Tail 5 -ErrorAction SilentlyContinue
    if ($blocks) {
        foreach ($line in $blocks) {
            Write-Host "  $line" -ForegroundColor Magenta
        }
    } else {
        Write-Host "  (no blocks yet)" -ForegroundColor Gray
    }
    Write-Host ""
}

Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host "  LIVE LOG MONITORING (Ctrl+C to stop)" -ForegroundColor Cyan
Write-Host "===============================================================" -ForegroundColor Cyan
Write-Host ""

# Tail all logs in real-time
$jobs = @()

# Service log
if (Test-Path $serviceLog) {
    $jobs += Start-Job -ScriptBlock {
        param($path)
        Get-Content -Path $path -Tail 0 -Wait | ForEach-Object {
            "[SERVICE] $_"
        }
    } -ArgumentList $serviceLog
}

# Access log
if (Test-Path $accessLog) {
    $jobs += Start-Job -ScriptBlock {
        param($path)
        Get-Content -Path $path -Tail 0 -Wait | ForEach-Object {
            "[ACCESS] $_"
        }
    } -ArgumentList $accessLog
}

# Block log
if (Test-Path $blockLog) {
    $jobs += Start-Job -ScriptBlock {
        param($path)
        Get-Content -Path $path -Tail 0 -Wait | ForEach-Object {
            "[BLOCK] $_"
        }
    } -ArgumentList $blockLog
}

try {
    while ($true) {
        foreach ($job in $jobs) {
            $output = Receive-Job -Job $job
            if ($output) {
                foreach ($line in $output) {
                    $timestamp = Get-Date -Format "HH:mm:ss"
                    
                    if ($line -like "*[BLOCK]*") {
                        Write-Host "[$timestamp] " -NoNewline -ForegroundColor DarkGray
                        Write-Host $line -ForegroundColor Red
                    }
                    elseif ($line -like "*[ACCESS]*") {
                        Write-Host "[$timestamp] " -NoNewline -ForegroundColor DarkGray
                        Write-Host $line -ForegroundColor Yellow
                    }
                    else {
                        Write-Host "[$timestamp] " -NoNewline -ForegroundColor DarkGray
                        Write-Host $line -ForegroundColor Cyan
                    }
                }
            }
        }
        Start-Sleep -Milliseconds 100
    }
}
finally {
    # Cleanup
    foreach ($job in $jobs) {
        Stop-Job -Job $job -ErrorAction SilentlyContinue
        Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
    }
    Write-Host ""
    Write-Host "Monitor stopped." -ForegroundColor Yellow
}
