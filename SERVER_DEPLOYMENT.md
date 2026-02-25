# 📂 SERVER DEPLOYMENT CHECKLIST

## 🎯 ШЛЯХИ НА СЕРВЕРІ

```
C:\                                    # Системний диск
├── Apps\                              # Приложения
│   └── WebApp\                        # СЮДИ КОПІЮЄМО ФАЙЛИ!
│       ├── WebApp.exe                 # Головна програма
│       ├── WebApp.dll                 # Основна бібліотека
│       ├── appsettings.json          # Конфіг сервера
│       ├── appsettings.Production.json
│       ├── Views\                     # HTML шаблони
│       ├── wwwroot\                   # CSS, JS, картинки
│       ├── logs\                      # Логи (створюється автоматично)
│       └── ... решта файлів
│
├── Windows\
│   └── System32\                      # Утиліти (для сценаріїв)
│
└── Users\
    └── Administrator\                 # Користувач для сервісу
```

---

## 📥 КРОК 1: ПІДГОТОВКА ФАЙЛІВ НА ЛОКАЛЬНІЙ МАШИНІ

### На машині розробника:
```powershell
# Перейти до проекту
cd C:\Users\samoilenkod\source\repos\Winservice

# Запустити скрипт публікації
.\deploy.ps1

# Скісти публіковані файли знаходяться в:
# .\artifacts\deploy\WebApp\
```

### Файли для копіювання:
```
📁 .\artifacts\deploy\WebApp\         # ВСЕ його змісту!
  ├── WebApp.exe                       # ⭐ Основна програма
  ├── WebApp.dll                       # ⭐ Основна бібліотека
  ├── appsettings.json                # ⭐ Конфіг
  ├── appsettings.Production.json      # Конфіг для Production
  ├── Views\                           # HTML представлення
  ├── wwwroot\                         # Статичні файли (CSS, JS)
  ├── runtimes\                        # .NET runtime файли
  └── [40+ інших файлів]               # Бібліотеки, ресурси
```

---

## 🖥️ КРОК 2: РОЗМІЩЕННЯ НА СЕРВЕРІ

### 2.1 На серверу Windows - створити папку для приложения
```powershell
# Запустити PowerShell як адміністратор!

# Створити папку
New-Item -ItemType Directory -Path "C:\Apps\WebApp" -Force

# Переконатися, що папка існує
Test-Path "C:\Apps\WebApp"
```

### 2.2 Копіювання файлів
**Варіант A: Без мережи (USB флешка)**
```
1. На локальній машині:
   - Скопіювати весь зміст .\artifacts\deploy\WebApp\
2. На сервері:
   - Вставити USB флешку
   - Копіювати файли на: C:\Apps\WebApp\
```

**Варіант B: Мережева копія (RDP / мережа)**
```powershell
# На сервері, в PowerShell (як адміністратор)

# Якщо розділ доступний по мережі
Copy-Item "\\DEVELOPER_PC\C$\path\to\artifacts\deploy\WebApp\*" `
          "C:\Apps\WebApp\" -Recurse -Force

# Або скопіювати Z: диск (якщо підключений)
Copy-Item "Z:\path\to\artifacts\deploy\WebApp\*" `
          "C:\Apps\WebApp\" -Recurse -Force
```

**Варіант C: WinSCP (якщо мається FTP/SSH)**
```
1. Завантажити WinSCP: https://winscp.net
2. Підключитися до сервера
3. Перетягти файли з локальної папки в C:\Apps\WebApp\
```

### 2.3 Перевірити файли на сервері
```powershell
# Перевірити, що файли скопійовані
Get-ChildItem "C:\Apps\WebApp\" | Select-Object Name

# Повинна бути відповідь вроде:
# WebApp.exe
# WebApp.dll
# appsettings.json
# Views
# wwwroot
# ...
```

---

## ⚙️ КРОК 3: КОНФІГУРАЦІЯ НА СЕРВЕРІ

### 3.1 Редагування appsettings.json для Production
```powershell
# На сервері, відкрити файл
C:\Apps\WebApp\appsettings.json
```

### 3.2 Типова конфіг для SQL Server на сервері
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SQL_SERVER;Database=DB_SAIT;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Urls": "http://0.0.0.0:5000"
}
```

### 3.3 Важні Параметри
```json
{
  "ConnectionStrings": {
    // ЗМІНИТИ НА ВАШЕ:
    // - YOUR_SQL_SERVER: IP сервера SQL, напр. "192.168.1.50"
    //                    або "SERVER_NAME"
    // - YOUR_PASSWORD:   Пароль користувача sa
    "DefaultConnection": "Server=YOUR_SQL_SERVER;Database=DB_SAIT;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=true;"
  },
  "Urls": "http://0.0.0.0:5000"  // Слухає на УСІХ IP, порт 5000
}
```

---

## 🚀 КРОК 4: ЗАПУСК НА СЕРВЕРІ

### Варіант A: Windows Service (рекомендується)

#### 4A.1 Копіювання скриптів запуску на сервер
```powershell
# На локальній машині скопіювати:
# C:\Users\samoilenkod\source\repos\Winservice\server-startup.ps1
# або
# C:\Users\samoilenkod\source\repos\Winservice\server-startup.bat

# На сервер у:
# C:\Apps\
```

#### 4A.2 Запуск на сервері
```powershell
# На сервері, PowerShell як адміністратор

# Перейти в папку
cd C:\Apps

# Запустити скрипт (інтерактивний режим меню)
.\server-startup.ps1

# У меню вибрати: 4. Install as service
```

#### 4A.3 Або вручну встановити сервіс
```powershell
# На сервері, PowerShell як адміністратор

sc create WebApp binPath= "C:\Apps\WebApp\WebApp.exe"
sc description WebApp "ASP.NET Core Web Application"
sc start WebApp

# Перевірити статус
sc query WebApp
```

### Варіант B: IIS

#### 4B.1 Встановити IIS та Hosting Bundle
```
1. Control Panel → Programs → Programs and Features
2. Turn Windows features on or off
3. ☑ Internet Information Services
4. Завантажити Hosting Bundle: 
   https://dotnet.microsoft.com/download/dotnet/8.0
5. Перезагрузити сервер
```

#### 4B.2 Створити сайт в IIS
```
1. Відкрити IIS Manager (inetmgr.exe)
2. Sites → Add Website
   - Site name: WebApp
   - Physical path: C:\Apps\WebApp
   - Binding: http, 0.0.0.0, port 80 (або 443 для HTTPS)
3. Вибрати Application Pool:
   - Новій пул: .NET CLR version "No Managed Code"
```

### Варіант C: Запуск як консоль (для тестування)
```powershell
# На сервері
cd C:\Apps\WebApp
.\WebApp.exe

# Приложение буде слухати на http://localhost:5000
```

---

## 🔍 КРОК 5: ПЕРЕВІРКА

### 5.1 Чи запущено приложение?
```powershell
# На сервері перевірити процес
Get-Process | Where-Object {$_.ProcessName -eq "WebApp"}

# Повинна бути строка з WebApp.exe
```

### 5.2 Чи слухає на портах?
```powershell
# На сервері перевірити портів
netstat -ano | findstr ":5000"

# Повинна бути строка вроде:
# TCP  0.0.0.0:5000  0.0.0.0:0  LISTENING  12345
```

### 5.3 Доступ по мережі
```powershell
# На серверу (локально)
Invoke-WebRequest http://localhost:5000 -UseBasicParsing

# З іншого ПК у мережі (замінити СЕРВЕР_IP)
Invoke-WebRequest http://СЕРВЕР_IP:5000 -UseBasicParsing
```

### 5.4 Перевірити БД
```powershell
# На сервері перевірити підключення до SQL Server
sqlcmd -S ВАैШ_SQL_СЕРВЕР -U sa -P ПАРОЛЬ -Q "SELECT @@VERSION"

# Перевірити базу даних
sqlcmd -S ВААЙШ_SQL_СЕРВЕР -U sa -P ПАРОЛЬ -Q "USE DB_SAIT; SELECT COUNT(*) FROM ImportData;"
```

---

## 📋 КОНТРОЛЬНИЙ СПИСОК

```
ПЕРЕД ЗАПУСКОМ:
☐ Файли скопійовані на C:\Apps\WebApp\
☐ WebApp.exe присутній
☐ appsettings.json змінений для Production
☐ SQL Server доступна
☐ DB_SAIT база існує
☐ Приложение встановлене як Windows Service (або IIS)

ПІСЛЯ ЗАПУСКУ:
☐ Приложение запущено (Get-Process)
☐ Слухає на портах (netstat)
☐ HTTP запити срабатывают (Invoke-WebRequest)
☐ Немає помилок у логах
☐ Навіться браузер відкривається на http://localhost:5000
```

---

## 🆘 ВИПРАВЛЕННЯ ПОМИЛОК

### Помилка: "Cannot find application exe"
```
Перевіряємо:
1. Файли скопійовані до C:\Apps\WebApp\
2. Файл WebApp.exe присутній
3. Права доступу на папку (усім користувачам)
```

### Помилка: "Cannot connect to database"
```
Перевіряємо:
1. SQL Server запущен на сервері
2. Рядок підключення у appsettings.json правильний
3. Користувач sa має пароль
4. Мережева доступність (ping SQL_SERVER)
```

### Помилка: "Port 5000 is already in use"
```powershell
# Знайти процес, який захопив порт
netstat -ano | findstr ":5000"

# Зупинити процес (замінити PID)
taskkill /PID 12345 /F
```

### Помилка: "Access Denied" на папку
```powershell
# На сервері, надати права на папку
icacls "C:\Apps\WebApp" /grant "Everyone:(OI)(CI)F" /T

# Або для специфічного користувача
icacls "C:\Apps\WebApp" /grant "DOMAIN\User:(OI)(CI)F" /T
```

---

## 📞 ФАЙЛИ ДЛЯ ДОВІДКИ

```
LOCAL MACHINE:
📄 C:\Users\samoilenkod\source\repos\Winservice\LOCAL_SETUP.md
   └─ Локальний запуск для розробки

📄 C:\Users\samoilenkod\source\repos\Winservice\DEPLOYMENT.md
   └─ Детальна документація розгортання

📄 C:\Users\samoilenkod\source\repos\Winservice\deploy.ps1
   └─ PowerShell скрипт публікації

📄 C:\Users\samoilenkod\source\repos\Winservice\deploy.bat
   └─ Batch скрипт публікації

SERVER MACHINE:
📄 C:\Apps\WebApp\appsettings.json
   └─ Конфігурація приложения

📄 C:\Apps\server-startup.ps1
   └─ PowerShell утиліта запуску

📄 C:\Apps\server-startup.bat
   └─ Batch утиліта запуску
```

---

## ✅ УСПІХ!

Якщо всі кроки виконані - приложение буде доступно на:
- **Локально на сервері**: http://localhost:5000
- **По мережі**: http://СЕРВЕР_IP:5000
- **Через IIS**: http://СЕРВЕР_NAME (якщо налаштовано)

**Готово до Production! 🚀**
