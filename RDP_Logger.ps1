# Скрипт логирования RDP Security Service
# Читает Event Log и пишет в файлы

$logDir = "$env:ProgramData\RDPSecurityService"
if (-not (Test-Path $logDir)) { mkdir $logDir -Force | Out-Null }

$accessLog = "$logDir\access.log"
$blockLog = "$logDir\block_list.log"

while ($true) {
    try {
        # Читаем последние события за последнюю минуту
        $minutes = 1
        $events = Get-EventLog -LogName Security -InstanceId 4625 -After (Get-Date).AddMinutes(-$minutes) -ErrorAction SilentlyContinue
        
        foreach ($evt in $events) {
            if ($evt.Message -match "Source Network Address:\s+(\S+)") {
                $ip = $matches[1]
                if ($ip -notin @("::1", "127.0.0.1", "-")) {
                    $logEntry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] IP: $ip | Event from $($evt.TimeGenerated)"
                    Add-Content -Path $accessLog -Value $logEntry -ErrorAction SilentlyContinue
                }
            }
        }
        
        # Проверяем заблокированные IPs из Firewall rules
        $output = & netsh.exe advfirewall firewall show rule name="RDP_BLOCK_*" dir=in 2>$null
        if ($output) {
            $lines = $output -split "`n"
            $currentRule = $null
            $currentIP = $null
            
            foreach ($line in $lines) {
                if ($line -match "Rule Name:\s+(.+)") {
                    $currentRule = $matches[1].Trim()
                }
                if ($line -match "Remote IP:\s+(.+)") {
                    $currentIP = $matches[1].Trim()
                    if ($currentIP -and $currentRule) {
                        # Проверяем уже ли записано в лог
                        $existing = Select-String -Path $blockLog -Pattern $currentIP -ErrorAction SilentlyContinue
                        if (-not $existing) {
                            $logEntry = "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] BLOCKED IP: $currentIP | Rule: $currentRule"
                            Add-Content -Path $blockLog -Value $logEntry -ErrorAction SilentlyContinue
                        }
                    }
                }
            }
        }
    }
    catch {
        Write-Error $_
    }
    
    Start-Sleep -Seconds 60
}
