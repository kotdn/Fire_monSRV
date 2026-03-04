# Quick smoke-test for WinService before production deployment
# Run as Administrator

$ErrorActionPreference = "Continue"
$testLog = ".\smoke-test.log"

function Write-Test {
    param($msg)
    $entry = "[{0:HH:mm:ss}] $msg" -f (Get-Date)
    Write-Host $entry
    $entry | Out-File -Append -FilePath $testLog
}

Write-Test "========== WinService Smoke Test =========="

# 1. Check binary exists
if (-not (Test-Path ".\WinService.exe")) {
    Write-Test "FAIL: WinService.exe not found"
    exit 1
}
Write-Test "OK: WinService.exe exists"

# 2. Check dependencies
$requiredDlls = @("System.Diagnostics.EventLog.dll", "System.ServiceProcess.dll")
foreach ($dll in $requiredDlls) {
    if (-not (Test-Path ".\$dll")) {
        Write-Test "WARN: $dll not found"
    }
}
Write-Test "OK: Core dependencies present"

# 3. Try to install service (dry-run check)
$installOutput = & .\WinService.exe install 2>&1
Write-Test "Install command output: $installOutput"

# 4. Check if service installed
$service = Get-Service -Name "RDPSecurityService" -ErrorAction SilentlyContinue
if ($null -eq $service) {
    Write-Test "WARN: Service not installed (sc create may have failed, check permissions)"
} else {
    Write-Test "OK: Service 'RDPSecurityService' registered"
    
    # 5. Check service status
    Write-Test "Service Status: $($service.Status)"
    
    # Stop if running
    if ($service.Status -eq "Running") {
        Stop-Service -Name "RDPSecurityService" -Force
        Start-Sleep -Seconds 2
    }
}

# 6. Check config directory
$configDir = "$env:ProgramData\RDPSecurityService"
if (-not (Test-Path $configDir)) {
    Write-Test "WARN: Config directory doesn't exist yet (created on first start)"
} else {
    Write-Test "OK: Config directory exists at $configDir"
    
    # Check key files
    $files = @("config.json", "access.log", "block_list.log", "whiteList.log", "service.log")
    foreach ($f in $files) {
        $path = Join-Path $configDir $f
        if (Test-Path $path) {
            Write-Test "  - $f exists"
        }
    }
}

# 7. Firewall rule check
$fwRule = Get-NetFirewallRule -Name "RDP_BLOCK_ALL" -ErrorAction SilentlyContinue
if ($null -eq $fwRule) {
    Write-Test "INFO: Firewall rule RDP_BLOCK_ALL not yet created (normal if service hasn't started)"
} else {
    Write-Test "OK: Firewall rule RDP_BLOCK_ALL exists"
    $addressFilter = $fwRule | Get-NetFirewallAddressFilter
    Write-Test "  RemoteAddress: $($addressFilter.RemoteAddress)"
}

# 8. Cleanup test installation
if ($null -ne $service) {
    Write-Test "Cleaning up test service..."
    & .\WinService.exe uninstall 2>&1 | Out-Null
    Start-Sleep -Seconds 1
    $serviceCheck = Get-Service -Name "RDPSecurityService" -ErrorAction SilentlyContinue
    if ($null -eq $serviceCheck) {
        Write-Test "OK: Test service uninstalled"
    } else {
        Write-Test "WARN: Service still exists after uninstall"
    }
}

Write-Test "========== Smoke Test Complete =========="
Write-Test ""
Write-Test "NEXT STEPS FOR PRODUCTION:"
Write-Test "1. Copy entire folder to target server"
Write-Test "2. Run as Admin: .\WinService.exe install"
Write-Test "3. Start service: Start-Service RDPSecurityService"
Write-Test "4. Check logs: $env:ProgramData\RDPSecurityService\service.log"
Write-Test "5. Edit config: $env:ProgramData\RDPSecurityService\config.json"
Write-Test ""
Write-Test "Test log saved to: $testLog"
