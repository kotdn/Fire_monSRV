# Требует запуска от администратора!
# Run as Administrator

$serviceName = "RDPSecurityService"
$servicePath = "$PSScriptRoot\RDP-Security-Suite-win-x64\WinService.exe"

# Проверка существования файла
if (-not (Test-Path $servicePath)) {
    Write-Host "Ошибка: WinService.exe не найден по пути $servicePath" -ForegroundColor Red
    exit 1
}

# Удаление старой версии если существует
Write-Host "Удаляю старую версию сервиса..."
sc.exe stop $serviceName 2>$null
sc.exe delete $serviceName 2>$null
Start-Sleep -Seconds 1

# Установка
Write-Host "Устанавливаю $serviceName..."
sc.exe create $serviceName binPath= "`"$servicePath`"" start= auto DisplayName= "RDP Security Service"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✓ Сервис успешно установлен!" -ForegroundColor Green
    Write-Host "Запускаю сервис..."
    sc.exe start $serviceName
} else {
    Write-Host "✗ Ошибка установки (код $LASTEXITCODE)" -ForegroundColor Red
}
