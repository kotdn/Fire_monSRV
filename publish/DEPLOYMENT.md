# 🚀 БЫСТРОЕ РАЗВЁРТЫВАНИЕ - RDP Security Service

## ⚡ САМЫЙ БЫСТРЫЙ СПОСОБ (2 клика)

### Вариант 1: PowerShell (рекомендуется)

1. **Скопировать всю папку `publish\` на сервер**
2. **Открыть PowerShell ОТ АДМИНИСТРАТОРА**
3. **Перейти в папку:**
   ```powershell
   cd C:\path\to\publish
   ```
4. **Запустить:**
   ```powershell
   .\deploy.ps1
   ```

✅ ВСЁ АВТОМАТИЧЕСКИ!

---

### Вариант 2: Batch (для очень консервативных серверов)

1. **Скопировать всю папку `publish\` на сервер**
2. **Открыть Command Prompt ОТ АДМИНИСТРАТОРА**
3. **Перейти в папку:**
   ```batch
   cd C:\path\to\publish
   ```
4. **Запустить:**
   ```batch
   deploy.bat
   ```

✅ ВСЁ АВТОМАТИЧЕСКИ!

---

## 📋 ЧТО ДЕЛАЮТ СКРИПТЫ

Скрипты автоматически:

✅ Останавливают старый сервис (если был)  
✅ Создают директорию `C:\ProgramData\RDPSecurityService\`  
✅ Копируют `WinService.exe` и `RDPSecurityViewer.exe`  
✅ Создают `config.json` с правильными настройками  
✅ Регистрируют Windows Service  
✅ Запускают сервис  

---

## ✅ ПРОВЕРКА

После развёртывания:

### 1. Проверить сервис запущен:
```powershell
Get-Service RDPSecurityService
# Status должен быть: Running
```

или

```batch
sc query RDPSecurityService
```

### 2. Запустить мониТор GUI:
```powershell
C:\ProgramData\RDPSecurityService\RDPSecurityViewer.exe
```

### 3. Проверить логи:
```powershell
Get-Content 'C:\ProgramData\RDPSecurityService\service.log'
Get-Content 'C:\ProgramData\RDPSecurityService\access.log'
Get-Content 'C:\ProgramData\RDPSecurityService\block_list.log'
```

---

## ⚙️ НАСТРОЙКИ

Редактировать: `C:\ProgramData\RDPSecurityService\config.json`

```json
{
  "port": 3389,
  "levels": [
    { "attempts": 3, "blockMinutes": 30 },      // На 30 мин
    { "attempts": 5, "blockMinutes": 180 },     // На 3 часа
    { "attempts": 7, "blockMinutes": 2880 }     // На 48 часов
  ]
}
```

**Сохраняй**, сервис перезагрузится сам.

---

## 🔧 УТИЛИТЫ

В папке `publish\`:

- **`deploy.ps1`** - PowerShell развертывание (основной)
- **`deploy.bat`** - Batch развертывание
- **`restart_service_admin.bat`** - Перезагрузить сервис
- **`update_firewall.ps1`** - Обновить firewall правило вручную

---

## 🐛 ЕСЛИ ВОЗНИКЛА ОШИБКА

### "Permission Denied"
→ Запусти Command Prompt / PowerShell **ОТ АДМИНИСТРАТОРА**

### "Service failed to start"
→ Проверь логи: `service.log`

### "port already in use"
→ Измени port в `config.json` (строка `"port": 3390`)

### ".NET Runtime not found"
→ Установи [.NET 8.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

---

## 📝 СТРУКТУРА ФАЙЛОВ

```
C:\ProgramData\RDPSecurityService\
├── WinService.exe              (Основной сервис)
├── RDPSecurityViewer.exe       (GUI Монитор)
├── config.json                 (Конфигурация)
├── service.log                 (Логи сервиса)
├── access.log                  (Все попытки)
├── block_list.log              (Заблокированные IPs)
└── current_log.log             (Текущие соединения)
```

---

## ✨ ГОТОВО!

Сервис установлен и работает. Блокирует атаки на RDP в реальном времени. 🛡️
