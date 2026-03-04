# RDP Security Service - Release Package

##  Описание | Description

**RDP Security Service**  служба Windows для защиты от брутфорс-атак на RDP (Remote Desktop Protocol). Автоматически блокирует IP-адреса после определенного количества неудачных попыток входа.

**RDP Security Service** is a Windows service that protects against brute-force attacks on RDP (Remote Desktop Protocol). Automatically blocks IP addresses after a specified number of failed login attempts.

##  Основные возможности | Key Features

-  **Многоуровневая защита** | Multi-level protection with escalating ban durations
-  **Белый список** | IP whitelist for trusted addresses
-  **GUI монитор** | User-friendly monitoring application
-  **UTF-8 поддержка** | Full UTF-8 support for all languages
-  **Локализация UA/EN** | Ukrainian and English interface
-  **Реал-тайм логи** | Real-time log monitoring
-  **Firewall интеграция** | Automatic Windows Firewall rule management

##  Содержимое пакета | Package Contents

```
release-YYYYMMDD-HHMMSS/
 WinService/          # Windows Service files
    WinService.exe   # Main service executable
 Monitor/             # GUI Monitor application
    RDPMonitor.exe   # Monitor executable
 install.ps1          # Installation script
 README.md            # This file
```

##  Установка | Installation

### Требования | Requirements
- Windows Server 2016+ / Windows 10+
- .NET 8.0 Runtime
- Права администратора | Administrator privileges

### Быстрая установка | Quick Install

1. **Запустите PowerShell от имени администратора** | Run PowerShell as Administrator
2. Перейдите в папку релиза | Navigate to release folder
3. Выполните | Execute:
   ```powershell
   .\install.ps1
   ```

##  Версия | Version

**Release Date:** 2026-03-04  
**Build:** UTF-8 Full Support  

**Слава Україні! **
