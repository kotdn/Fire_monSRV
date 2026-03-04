# 📋 CHANGELOG - RDP Security Service

## [2026-03-03] - Configurable RDP Port

### ✨ Added
- **Параметр порта при установке**: Теперь setup.bat и deploy.bat принимают порт как параметр
  ```batch
  setup.bat 3390
  deploy.bat 3390
  ```

- **Интерактивный запрос порта**: Если параметр не указан, setup.bat спросит порт во время установки
  ```
  Enter RDP port to monitor [default: 3389]: _
  ```

- **Новая документация**:
  - `CONFIG_EXAMPLES.md` - 5 примеров конфигураций (стандартный порт, нестандартный, агрессивная/мягкая блокировка, ультра-строгий режим)
  - `CHEATSHEET.md` - Шпаргалка всех команд и путей
  - `check_status.bat` - Быстрая проверка статуса службы

### 🔧 Changed
- **setup.bat**: Добавлен параметр командной строки для порта + интерактивный запрос
- **deploy.bat**: Добавлен параметр командной строки для порта
- **README.md**: Обновлена секция "Настройка порта мониторинга" с 4 вариантами
- **Версия**: 2026-03-01 → 2026-03-03

### 📚 Documentation
- Примеры для 5 сценариев использования (публичные серверы, внутренняя сеть, ультра-строгий режим)
- Таблицы рекомендованных значений для времени блокировки и количества попыток
- Команды для проверки и управления конфигурацией

---

## [2026-03-01] - Light Theme + Code Quality

### ✨ Added
- **Light Theme**: Светлая тема для RDPSecurityViewer (230-240 gray, черный текст)
- **System Tray**: Монитор минимизируется в system tray вместо закрытия

### 🐛 Fixed
- **Compilation Warnings**: Исправлены 76 ошибок/предупреждений
  - Nullable reference warnings resolved
  - Windows-only API warnings suppressed
  - Obsolete EventID → InstanceId
- **Build Status**: ✅ Успешная компиляция с 19 nullable warnings (некритичные)

### 🔧 Changed
- **RDPSecurityViewer**: OnFormClosing → minimize to tray
- **WinService.csproj**: Добавлен RuntimeIdentifier (win-x64) и SupportedOSPlatformVersion
- **Program.cs**: Добавлен `#pragma warning disable CA1416`

---

## [2026-02-28] - System Tray + SYSTEM Account

### ✨ Added
- **System Tray Icon**: NotifyIcon с контекстным меню (Show/Hide/Exit)
- **Minimize to Tray**: Закрытие окна минимизирует в tray, а не выходит из приложения
- **Tray Menu**: Правый клик → Show/Hide/Exit

### 🔧 Changed
- **Установка сервиса**: Теперь от SYSTEM account (было LocalService)
- **setup.bat**: Обновлен для установки от SYSTEM
- **Документация**: Добавлен TRAY_GUIDE.md

---

## [2026-02-25] - IP Grouping Fix

### 🐛 Fixed
- **Duplicate IPs**: Blocked IPs tab теперь группирует по IP (без дублей)
- **Sorting**: Сначала ACTIVE, потом EXPIRED
- **Display**: Показывает Latest Time, Max Attempts, Status для каждого IP

### 🔧 Changed
- **LoadBlockedIPs()**: Переписан с Dictionary-based grouping
- **UI Refresh**: Оптимизирован до 3 секунд

---

## [2026-02-20] - Firewall Sync Fix (Critical)

### 🐛 Fixed
- **CRITICAL**: Firewall synchronization was broken
  - PowerShell cmdlets failing with InputObjectNotBound errors
  - `Remove-NetFirewallRule` and `Set-NetFirewallAddressFilter` не работали

### 🔧 Changed
- **UpdateFirewallRuleFromBlockList()**: Полностью переписан
  - Теперь использует `netsh` command-line вместо PowerShell cmdlets
  - 100% надёжная синхронизация firewall rules с block_list.log

```batch
# Старый код (BROKEN):
Remove-NetFirewallRule -Name "RDP_BLOCK_ALL" -ErrorAction SilentlyContinue
New-NetFirewallRule -Name "RDP_BLOCK_ALL" ...

# Новый код (WORKING):
netsh advfirewall firewall delete rule name="RDP_BLOCK_ALL"
netsh advfirewall firewall add rule name="RDP_BLOCK_ALL" ...
```

---

## [2026-02-15] - Initial Release

### ✨ Features
- **Event Log Monitoring**: Windows Event Security 4625 (Failed RDP logon)
- **Multi-level Blocking**: 3/5/7 попыток → разные времена блокировки
- **Firewall Integration**: Автоматическое добавление IP в Windows Firewall
- **WinForms Monitor**: GUI для просмотра попыток и заблокированных IP
- **SQLite Database**: Хранение истории блокировок
- **Configuration**: JSON config для порта и уровней блокировки
- **Logs**: access.log, block_list.log, current.log

### 📦 Components
- **WinService.exe**: Windows Service (SYSTEM account)
- **RDPSecurityViewer.exe**: WinForms Monitor
- **Deployment Scripts**: setup.bat, deploy.bat, deploy.ps1

---

## 📊 Version Summary

| Version    | Key Feature                        | Status      |
|------------|------------------------------------|-------------|
| 2026-03-03 | Configurable Port                  | ✅ Current   |
| 2026-03-01 | Light Theme + Code Quality         | ✅ Stable    |
| 2026-02-28 | System Tray + SYSTEM Account       | ✅ Stable    |
| 2026-02-25 | IP Grouping Fix                    | ✅ Stable    |
| 2026-02-20 | Firewall Sync Fix (Critical)       | ✅ Stable    |
| 2026-02-15 | Initial Release                    | ✅ Archived  |

---

## 🔜 Roadmap

### Planned Features:
- [ ] Web Dashboard для мониторинга
- [ ] Email alerts при блокировке
- [ ] Whitelist management в GUI
- [ ] Автоматический geolocation lookup для IP
- [ ] Export blocked IPs в CSV/JSON
- [ ] Integration с cloud firewall services

### Under Consideration:
- [ ] Multi-server management
- [ ] API для внешних систем
- [ ] Machine Learning для detection patterns
- [ ] Integration с SIEM системами

---

**Документация**: [README.md](README.md) | [CONFIG_EXAMPLES.md](CONFIG_EXAMPLES.md) | [CHEATSHEET.md](CHEATSHEET.md)
