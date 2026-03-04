# RDP Security Service - Monitorの Tray Icon (System Tray)

## 🎯 Монитор теперь в трее!

### Что изменилось:

✅ **Монитор RDPSecurityViewer** - работает в system tray (рядом с часами)  
✅ **Сервис WinService** - установлен и работает от пользователя **SYSTEM**  
✅ **Кликабельная иконка** - открыть/закрыть монитор  

---

## 🖱️ КАК ПОЛЬЗОВАТЬСЯ

### Запуск монитора:

1. **Первый раз** - запусти сразу после установки:
   ```batch
   C:\ProgramData\RDPSecurityService\RDPSecurityViewer.exe
   ```

2. **После закрытия окна** - иконка остаётся в трее ✓

---

## 🔄 РАБОТА С ТРЕЙ-ИКОНКОЙ

### Левый клик на иконку:
- 📌 Развернуть монитор (если открыт → закроется в трей)
- 📌 Закрыть монитор (если закрыт → откроется)

### Правый клик на иконку:
- **Show** - Развернуть монитор
- **Hide** - Свернуть в трей
- **Exit** - Закрыть приложение (сервис продолжит работать!)

---

## ⚙️ СЛУЖБА (WinService)

### Установлена от пользователя: **SYSTEM**

Это значит:
- ✅ Полный доступ к Event Log (для чтения попыток RDP)
- ✅ Полный доступ к Firewall (для блокирования IP)
- ✅ Автоматический запуск при перезагрузке сервера
- ✅ Запускается ДО login пользователя

### Проверить статус:
```powershell
Get-Service RDPSecurityService | Select-Object Status, StartType
```

или

```batch
sc query RDPSecurityService
```

### Перезагрузить сервис:
```batch
C:\ProgramData\RDPSecurityService\restart_service_admin.bat
```

или

```batch
net stop RDPSecurityService
net start RDPSecurityService
```

---

## 📋 РАБОТА МОНИТОРА

### Вкладки в мониторе:

1. **Failed Attempts** - Все попытки RDP входа
2. **Blocked IPs** - Заблокированные IP адреса (grouped, no duplicates)
3. **WhiteList** - Белый список (не блокировать)
4. **Current Log** - Текущие TCP соединения

### Кнопки в мониторе:

- **Refresh** - Пересчитать Failed Attempts
- **Перечитать** - Обновить Blocked IPs вручную
- **Add IP** - Добавить в WhiteList или BlockedIPs
- **Delete** - Удалить из списков

---

## 📝 ФАЙЛЫ И ЛОГИ

Всё в: `C:\ProgramData\RDPSecurityService\`

```
logs/
├── service.log          (события сервиса)
├── access.log           (все попытки RDP)
├── block_list.log       (заблокированные IP)
├── current_log.log      (текущие соединения)
├── config.json          (конфигурация)
├── whiteList.log        (белый список)
└── [binaries + dlls]
```

---

## 🔧 КОНФИГУРАЦИЯ

Отредактировать: `C:\ProgramData\RDPSecurityService\config.json`

```json
{
  "port": 3389,
  "levels": [
    { "attempts": 3, "blockMinutes": 30 },      // 3+ = блокируем на 30 мин
    { "attempts": 5, "blockMinutes": 180 },     // 5+ = блокируем на 3 часа
    { "attempts": 7, "blockMinutes": 2880 }     // 7+ = блокируем на 48 часов
  ]
}
```

**После сохранения** - сервис перезагрузится автоматически ✓

---

## ✨ ИТОГО

- 🎯 Монитор в трее для удобства
- 🛡️ Сервис работает 24/7 от SYSTEM
- 📊 Все логи и настройки в папке
- 🔄 Автоматический запуск при перезагрузке

**ГОТОВО К ПРОДАКШЕНУ!** 🚀
