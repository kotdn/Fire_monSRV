# 🚀 RDP Security Service - Deployment Package

**Version**: 2026-03-03  
**Features**: System Tray Monitor + Service from SYSTEM Account + Configurable Port

---

## 📦 СОДЕРЖИМОЕ ПАПКИ

```
publish/
├── 🎯 setup.bat                ← ЗАПУСТИ ЭТО ПЕРВЫМ (от администратора!)
├── deploy.bat                   (альтернатива - полная установка)
├── deploy.ps1                   (PowerShell версия)
├── check_status.bat            (быстрая проверка статуса)
├── restart_service_admin.bat   (перезапуск сервиса)
├── update_firewall.ps1         (обновление firewall)
├── RDPSecurityViewer/
│   ├── RDPSecurityViewer.exe   (Монитор в трее)
│   └── RDPSecurityViewer.dll
├── WinService/
│   ├── WinService.exe          (Сервис - работает от SYSTEM)
│   ├── WinService.dll
│   └── [другие файлы]
└── 📖 Документация:
    ├── README.md               (главная инструкция)
    ├── CONFIG_EXAMPLES.md      (⭐ примеры настройки портов)
    ├── CHEATSHEET.md           (⚡ шпаргалка команд)
    ├── CHANGELOG.md            (история изменений)
    ├── TRAY_GUIDE.md           (работа с монитором в трее)
    ├── DEPLOYMENT.md           (развёрнутая инструкция)
    └── INSTALL.md              (техническое описание)
```

---

## ⚡ БЫСТРЫЙ СТАРТ (3 ШАГА)

### 1️⃣ Скопировать на сервер

Весь `publish/` на сервер (например: `C:\Software\RDP-Security\`)

### 2️⃣ Открыть Command Prompt ОТ АДМИНИСТРАТОРА

**Стандартный порт (3389):**
```batch
cd C:\Software\RDP-Security
setup.bat
```

**Нестандартный порт:**
```batch
cd C:\Software\RDP-Security
setup.bat 3390
```

⚙️ **Во время установки скрипт спросит порт** (если не указан в параметре)

### 3️⃣ ВСЁ! 

- ✅ Сервис установлен и запущен
- ✅ Монитор открыт в трее
- ✅ Готово блокировать RDP атаки

---

## 🔧 НАСТРОЙКА ПОРТА МОНИТОРИНГА

RDP Security Service может мониторить любой порт (не только стандартный 3389).

### Вариант 1: При установке (параметр)
```batch
setup.bat 3390
deploy.bat 3390
```

### Вариант 2: При установке (интерактивно)
```batch
setup.bat
⏩ Enter RDP port to monitor [default: 3389]: 3390
```

### Вариант 3: PowerShell
```powershell
.\deploy.ps1 -RDPPort 3390
```

### Вариант 4: Ручное редактирование
Отредактировать `C:\ProgramData\RDPSecurityService\config.json`:
```json
{
  "port": 3390,
  "levels": [ ... ]
}
```
Затем перезапустить сервис:
```batch
restart_service_admin.bat
```

---

## 🎯 ЧТО ИЗМЕНИЛОСЬ В ЭТОЙ ВЕРСИИ

### Монитор (RDPSecurityViewer):
- ✨ **Система трей** - минимизируется в system tray (рядом с часами)
- 🖱️ **Тray меню** - Show/Hide/Exit
- 📌 **Одиночный клик** - показать/скрыть
- 🚫 **Закрытие окна** - не закрывает приложение, только минимизирует в трей
- 📊 **IP Grouping** - без дублей в Blocked IPs
- 🔄 **Автоматический refresh** - Every 3 сек

### Сервис (WinService):
- 👤 **Запускается от: SYSTEM** - полный доступ к Event Log + Firewall
- 🔒 **Автозапуск** - при перезагрузке сервера
- 📋 **Event Log 4625** - мониторит failed RDP attempts
- 🚨 **Блокирует через firewall** - netsh (надежнее PowerShell)
- ✅ **Multi-level blocking** - 3, 5, 7 попыток = разные времена блокировки

---

## 📋 ФАЙЛЫ & ДОКУМЕНТАЦИЯ

### Для запуска:
- `setup.bat` - **ОСНОВНОЙ** (запуск от администратора)
- `deploy.bat` - Полная установка со всеми параметрами
- `deploy.ps1` - PowerShell версия

### Утилиты:
- `restart_service_admin.bat` - Перезагрузить сервис вручную
- `update_firewall.ps1` - Обновить firewall правило

### Документация:
- `README.md` - **⭐ ТЫ ЗДЕСь** Главная инструкция
- `CONFIG_EXAMPLES.md` - **🔧 ПРИМЕРЫ КОНФИГУРАЦИЙ** (порты, уровни блокировки)
- `CHEATSHEET.md` - **⚡ ШПАРГАЛКА** (все команды в одном месте)
- `CHANGELOG.md` - История изменений версий
- `TRAY_GUIDE.md` - Работа с монитором в трее
- `DEPLOYMENT.md` - Развёрнутые инструкции
- `INSTALL.md` - Техническое описание

---

## 🔑 ВАЖНОЕ

⚠️ **ВСЕГДА запускай setup.bat от администратора!**

```batch
REM Правильно:
"C:\Software\RDP-Security\setup.bat"   ← От Admin CMD

REM Неправильно:
Просто двойной клик без прав Admin ❌
```

---

## ✅ ПРОВЕРИТЬ РАБОТУ

### 1. Сервис запущен?
```powershell
Get-Service RDPSecurityService
# Status должен быть: Running
```

### 2. Монитор работает?
- 🎯 Иконка в system tray (рядом с часами Windows)
- Клик на иконку = открыть/закрыть

### 3. Логи генерируются?
```batch
dir C:\ProgramData\RDPSecurityService\
```

Должны быть:
- service.log
- access.log  
- block_list.log

---

## 🎮 УПРАВЛЕНИЕ

### Во время работы:

**Монитор:**
- Левый клик на тray = Show/Hide
- Правый клик на tray = меню (Show/Hide/Exit)
- Вкладки: Failed Attempts, Blocked IPs, WhiteList, Current Log

**Сервис:**
```batch
net stop RDPSecurityService     # Остановить
net start RDPSecurityService    # Запустить
```

### Настройки:

Редактировать: `C:\ProgramData\RDPSecurityService\config.json`

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

---

## ⚙️ ТРЕБОВАНИЯ

- **Windows Server 2016+** или Windows 10+
- **.NET 8.0 Runtime** ([Install](https://dotnet.microsoft.com/en-us/download/dotnet/8.0))
- **Admin права** на установку
- **Event Log доступ** (SYSTEM = автоматически есть)

---

## 🐛 ЕСЛИ ЧТО-ТО НЕ РАБОТАЕТ

### Ошибка: "Permission Denied"
→ Запусти setup.bat откак от **Administrator**

### Ошибка: "Service failed to start"
→ Проверь логи:
```batch
type C:\ProgramData\RDPSecurityService\service.log
```

### Монитор не открывается
→ Проверь Event Viewer (recherche для ошибок)

### .NET Runtime missing
→ Установи: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

---

## 📞 ТЕХПОДДЕРЖКА

Все логи в: `C:\ProgramData\RDPSecurityService\`

Проверить:
1. `service.log` - ошибки сервиса
2. `access.log` - попытки RDP
3. `block_list.log` - блокировки

---

## ✨ ГОТОВО!

Теперь сервер защищен от RDP атак 24/7!

🛡️ **ENJOY PROTECTED RDP!** 🛡️
