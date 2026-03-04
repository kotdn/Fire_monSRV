# ⚡ RDP Security Service - Шпаргалка

## 🚀 Установка

### Стандартный порт (3389):
```batch
setup.bat
```

### Нестандартный порт:
```batch
setup.bat 3390
```
или
```powershell
.\deploy.ps1 -RDPPort 3390
```

---

## 🔧 Настройка порта

**Файл:** `C:\ProgramData\RDPSecurityService\config.json`

```json
{
  "port": 3389,
  "levels": [
    { "attempts": 3, "blockMinutes": 30 },
    { "attempts": 5, "blockMinutes": 180 },
    { "attempts": 7, "blockMinutes": 2880 }
  ]
}
```

**После изменений:**
```batch
restart_service_admin.bat
```

---

## 📂 Важные пути

| Что | Где |
|-----|-----|
| Конфиг | `C:\ProgramData\RDPSecurityService\config.json` |
| Логи | `C:\ProgramData\RDPSecurityService\Logs\` |
| Access лог | `C:\ProgramData\RDPSecurityService\Logs\access.log` |
| Block list | `C:\ProgramData\RDPSecurityService\Logs\block_list.log` |
| База данных | `C:\ProgramData\RDPSecurityService\ban.db` |
| Монитор | `C:\ProgramData\RDPSecurityService\RDPSecurityViewer.exe` |
| Сервис | `C:\ProgramData\RDPSecurityService\WinService.exe` |

---

## 🛠️ Управление сервисом

### Статус:
```batch
sc query RDPSecurityService
```

### Старт:
```batch
net start RDPSecurityService
```

### Стоп:
```batch
net stop RDPSecurityService
```

### Перезапуск:
```batch
net stop RDPSecurityService
net start RDPSecurityService
```
или
```batch
restart_service_admin.bat
```

### Удаление:
```batch
sc delete RDPSecurityService
```

---

## 🔍 Просмотр логов

### Access лог (все попытки):
```batch
type C:\ProgramData\RDPSecurityService\Logs\access.log
```

### Block list (заблокированные):
```batch
type C:\ProgramData\RDPSecurityService\Logs\block_list.log
```

### Firewall правила:
```batch
netsh advfirewall firewall show rule name="RDP_BLOCK_ALL"
```

---

## 🖥️ Монитор (Tray)

### Запуск:
```batch
C:\ProgramData\RDPSecurityService\RDPSecurityViewer.exe
```

### Управление:
- **Minimize to tray** - закрытие окна НЕ выключает приложение
- **Show/Hide** - двойной клик на иконке в трее
- **Exit** - правый клик → Exit

---

## 🚨 Firewall

### Список заблокированных IP:
```batch
netsh advfirewall firewall show rule name="RDP_BLOCK_ALL"
```

### Удалить правило (разблокировать все):
```batch
netsh advfirewall firewall delete rule name="RDP_BLOCK_ALL"
```

### Пересоздать правило (синхронизация):
```batch
update_firewall.ps1
```

---

## 📊 Типовые команды

### Узнать текущий порт:
```powershell
(Get-Content C:\ProgramData\RDPSecurityService\config.json | ConvertFrom-Json).port
```

### Сколько заблокировано IP:
```batch
find /c "ACTIVE" C:\ProgramData\RDPSecurityService\Logs\block_list.log
```

### Последние 10 попыток подключения:
```powershell
Get-Content C:\ProgramData\RDPSecurityService\Logs\access.log -Tail 10
```

### Проверить, работает ли служба:
```batch
sc query RDPSecurityService | find "RUNNING"
```

---

## 🎯 Экспресс-проверка работы

```batch
@echo off
echo === RDP Security Service Status ===
echo.
echo [Service Status]:
sc query RDPSecurityService | find "STATE"
echo.
echo [Config Port]:
powershell -NoProfile -Command "(Get-Content C:\ProgramData\RDPSecurityService\config.json | ConvertFrom-Json).port"
echo.
echo [Active Blocks]:
find /c "ACTIVE" C:\ProgramData\RDPSecurityService\Logs\block_list.log
echo.
echo [Firewall Rule]:
netsh advfirewall firewall show rule name="RDP_BLOCK_ALL" | find "RemoteIP"
echo.
pause
```

Сохранить как `check_status.bat` и запустить.

---

## 📚 Дополнительно

- **Полная документация**: [README.md](README.md)
- **Примеры конфигов**: [CONFIG_EXAMPLES.md](CONFIG_EXAMPLES.md)
- **System Tray гайд**: [TRAY_GUIDE.md](TRAY_GUIDE.md)
- **Развёртывание**: [DEPLOYMENT.md](DEPLOYMENT.md)

---

**Версия**: 2026-03-03
