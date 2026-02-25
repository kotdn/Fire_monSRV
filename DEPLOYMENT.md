# 🚀 DEPLOYMENT GUIDE

## Быстрый старт

### Локально (для тестирования)
```powershell
cd C:\path\to\Winservice
.\deploy.ps1 -Environment Release
```

### На сервер (Production)
1. Запустить `deploy.ps1` локально
2. Скопировать содержимое папки `artifacts\deploy\WebApp` на сервер
3. Следовать инструкциям ниже

---

## Требования сервера

- **ОС**: Windows Server 2019+ или Windows 10/11
- **Framework**: .NET 8.0 Runtime (или .NET 8.0 SDK)
- **SQL Server**: 2019+ или SQL Server Express
- **IIS**: опционально (можно запустить как консольное приложение)
- **Порт**: 5000 (по умолчанию) или настроить в appsettings.json

---

## Способ 1: Windows Service (рекомендуется)

### Создание сервиса
```powershell
# Перейти в папку приложения
cd C:\Apps\WebApp

# Создать сервис
sc create WebApp binPath= "C:\Apps\WebApp\WebApp.exe"

# Установить описание
sc description WebApp "ASP.NET Core Web Application"

# Запустить сервис
sc start WebApp

# Проверить статус
sc query WebApp
```

### Остановка сервиса
```powershell
sc stop WebApp
```

### Удаление сервиса
```powershell
sc delete WebApp
```

---

## Способ 2: IIS (Internet Information Services)

### Установка prerequisite
1. **Установить IIS**:
   - Control Panel → Programs → Programs and Features
   - Turn Windows features on or off
   - ☑ Internet Information Services

2. **Установить Hosting Bundle**:
   - Скачать: https://dotnet.microsoft.com/download/dotnet/8.0
   - `.NET 8.0 Hosting Bundle` (для Windows)

3. **Перезагрузить сервер** (важно!)

### Создание приложения в IIS
1. Открыть **IIS Manager** (inetmgr.exe)
2. Правый клик на **Sites** → **Add Website**
   - **Site name**: WebApp
   - **Physical path**: C:\Apps\WebApp
   - **Binding**: http, localhost, port 80

3. **Application Pool**:
   - Создать новый пул
   - **.NET CLR version**: No Managed Code
   - **Managed pipeline mode**: Integrated

4. Назначить приложение в этот пул

---

## Способ 3: Запуск как консольное приложение

```powershell
cd C:\Apps\WebApp
.\WebApp.exe
```

Приложение будет доступно по адресу: `http://localhost:5000`

---

## Конфигурация приложения

### appsettings.json
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DB_SAIT;Trusted_Connection=true;"
  },
  "AllowedHosts": "*",
  "Urls": "http://localhost:5000"
}
```

### Изменение порта
- **IIS**: настроить Binding (обычно 80/443)
- **Windows Service**: изменить `Urls` в appsettings.json
- **Консоль**: передать аргумент: `dotnet WebApp.dll --urls http://localhost:8080`

### SQL Server подключение
```
Server=SERVER_NAME;Database=DB_SAIT;User Id=sa;Password=YOUR_PASSWORD;
```

---

## Проверка развёртывания

### Локально
```powershell
# Проверить, слушает ли приложение на портах
netstat -ano | findstr "5000"

# Перейти в браузер
http://localhost:5000/import/list
```

### На сервере
```powershell
# Если Windows Service
sc query WebApp

# Если консольное приложение
tasklist | findstr "WebApp"

# Проверить логи
Get-Content "C:\Apps\WebApp\logs\*.log"

# Попытка подключиться
Invoke-WebRequest http://localhost:5000/import/list -UseBasicParsing
```

---

## Проблемы и решения

### Приложение не запускается
1. Проверить .NET SDK установлен: `dotnet --version`
2. Проверить SQL Server доступен
3. Посмотреть логи приложения в папке logs/

### SQL Server недоступен
1. Проверить SQL Server запущен: `SELECT @@VERSION`
2. Проверить строку подключения в appsettings.json
3. Если используется проверка Windows: убедиться в правах пользователя

### IIS 500 ошибки
1. Включить детальные ошибки: `web.config`
```xml
<system.webServer>
  <httpErrors errorMode="Detailed" />
</system.webServer>
```
2. Проверить Application Pool запущен
3. Посмотреть Event Viewer

### Порт уже занят
```powershell
# Найти процесс, использующий порт 5000
netstat -ano | findstr ":5000"

# Остановить процесс
taskkill /PID <PID> /F
```

---

## Обновление приложения

### На локальной машине
```powershell
# 1. Обновить код в git
git pull

# 2. Запустить deploy.ps1
.\deploy.ps1

# 3. Скопировать содержимое artifacts\deploy\WebApp на сервер
```

### На сервере
```powershell
# 1. Если Windows Service
sc stop WebApp

# 2. Скопировать новые файлы (перезаписать)
Copy-Item "C:\path\to\deploy\WebApp\*" "C:\Apps\WebApp\" -Recurse -Force

# 3. Запустить сервис или приложение
sc start WebApp
```

---

## Security рекомендации

- ✅ Использовать HTTPS (SSL сертификат)
- ✅ Ограничить доступ к администраторским функциям
- ✅ Использовать пароли для SQL Server
- ✅ Регулярно делать backup БД
- ✅ Мониторить логи приложения
- ✅ Обновлять .NET и SQL Server

---

## Поддержка

Для помощи посмотреть:
- Логи приложения: `logs/` папка
- Event Viewer: System и Application
- IIS Logs: `%SystemRoot%\System32\LogFiles\W3SVC\`
