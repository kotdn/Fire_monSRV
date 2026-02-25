# 🔧 ЛОКАЛЬНИЙ ЗАПУСК ДЛЯ РОЗРОБКИ

## 📍 ШЛЯХИ ПРОЕКТУ

### Основне місцезнаходження
```
C:\Users\samoilenkod\source\repos\Winservice
```

### Головні папки
```
📁 Winservice/                          # Корінь проекту
  ├── 📁 WebApp/                        # ASP.NET Core 8.0 приложение
  │   ├── WebApp.csproj                 # Файл проекта
  │   ├── Program.cs                    # Точка входа
  │   ├── appsettings.json              # Конфигурация
  │   ├── Controllers/                  # Контролери
  │   ├── Models/                       # Моделі даних
  │   ├── Views/                        # Razor представлення
  │   └── wwwroot/                      # Статичні файли
  │
  ├── 📁 artifacts/                     # Артефакти збірки
  ├── 📁 TestProject/                   # Юніт-тести (опціонально)
  │
  ├── deploy.ps1                        # PowerShell скрипт розгортання
  ├── deploy.bat                        # Batch скрипт розгортання
  ├── DEPLOYMENT.md                     # Документація розгортання
  ├── LOCAL_SETUP.md                    # Цей файл
  │
  └── Winservice.sln                    # Visual Studio рішення
```

---

## 🚀 ШВИДКИЙ СТАРТ

### Мінімум вимог
- **Windows 10/11** або **Windows Server 2019+**
- **.NET 8.0 SDK** https://dotnet.microsoft.com/download/dotnet/8.0
- **SQL Server 2019+** або **(localdb)\mssqllocaldb** (з Visual Studio)
- **Git** для клонування репо

### Перевірка встановлення
```powershell
# Перевіряємо .NET
dotnet --version
# Повинно бути: 8.0.x або вище

# Перевіряємо SQL Server
sqlcmd -S (localdb)\mssqllocaldb -Q "SELECT @@VERSION"
```

---

## 🗂️ ФАЙЛОВА СТРУКТУРА

### WebApp/Controllers/
```
📁 Controllers/
  ├── ImportController.cs      # Логіка імпорту Excel (основне!)
  ├── HomeController.cs        # Стартова сторінка
  └── ... інші контролери
```

#### ImportController.cs - ЩО ТУТ:
- `Excel()` GET - форма завантаження файлу
- `Excel()` POST - обробка Excel файлу (аналіз структури)
- `List()` - показати імпортовані дані
- `Add()` - додати новий запис
- `Delete()` - видалити запис
- `Clear()` - видалити все

### WebApp/Views/Import/
```
📁 Views/Import/
  ├── Excel.cshtml    # Форма завантаження + аналіз файлу
  ├── List.cshtml     # Таблиця з усіма 15 колонками (ПОПРИ!)
  └── ... інші представлення
```

#### List.cshtml - ТАБЛИЦЯ З 15 КОЛОНОК:
1. ID
2. Рік (Year)
3. № вводу (EntryNo)
4. Постачальник (VendorDescription)
5. Номер інвойсу (InvoiceNumber)
6. Сума (InvoiceAmount)
7. Дата даних (DateOfData)
8. Дата інвойсу (InvoiceDate)
9. Дата отримання (DateOfReceipt)
10. Перевізник (Carrier)
11. Умови доставки (TermsOfDelivery)
12. Вага брутто (GrossWeight)
13. Вага нетто (NetWeight)
14. Імпортер (ImporterName)
15. Кнопка видалення

### WebApp/Models/
```
📁 Models/
  ├── ImportData.cs           # Модель для збереження даних
  └── ... інші моделі
```

#### ImportData.cs - СТРУКТУРА:
- 35 nullable properties для Excel колонок
- Поля з кириличними назвами для українських даних
- Автоматична конвертація типів

---

## 💻 ЛОКАЛЬНИЙ ЗАПУСК

### Способ 1: Через IDE (Visual Studio)
```
1. Відкрити C:\Users\samoilenkod\source\repos\Winservice\Winservice.sln
2. Натиснути F5 (запуск Debug)
3. Браузер відкриється: https://localhost:7000 (або http:localhost:5000)
```

### Способ 2: Через CMD/PowerShell
```powershell
# Перейти в папку WebApp
cd C:\Users\samoilenkod\source\repos\Winservice\WebApp

# Запустити приложение
dotnet run

# Або в Release режимі
dotnet run --configuration Release
```

### Способ 3: Через запуск exe файлу
```powershell
# Спочатку опублікувати
cd C:\Users\samoilenkod\source\repos\Winservice
.\deploy.ps1

# Потім запустити
.\artifacts\deploy\WebApp\WebApp.exe
```

---

## 🌐 ДОСТУП ПО ЛОКАЛЬНІЙ МЕРЕЖІ

### Розробниця машина прослуховує на портах:
- **https://localhost:7000** (HTTPS)
- **http://localhost:5000** (HTTP)

### Отримати доступ з іншого комп'ютера в мережі:
```powershell
# 1. Дізнатися IP адресу розробниці машини
ipconfig

# Шукаємо "IPv4 Address", напр: 192.168.1.100

# 2. З іншого ПК відкрити:
# http://192.168.1.100:5000
```

### Якщо не працює - дозволити firewall:
```powershell
# PowerShell (як адміністратор)
netsh advfirewall firewall add rule name="WebApp Port 5000" dir=in action=allow protocol=tcp localport=5000
```

---

## 🗄️ БАЗА ДАНИХ

### Переважна конфігурація
```
Сървер: (localdb)\mssqllocaldb
База: DB_SAIT
Кодування: SQL_Latin1_General_CP1_CI_AS
```

### appsettings.json - де конфігурувати
```
C:\Users\samoilenkod\source\repos\Winservice\WebApp\appsettings.json
```

### Рядок підключення
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DB_SAIT;Trusted_Connection=true;"
  }
}
```

### Перевірити підключення БД
```powershell
# Запуститися сервер
sqlcmd -S (localdb)\mssqllocaldb -Q "USE DB_SAIT; SELECT COUNT(*) FROM ImportData;"
```

---

## 📊 МИГРАЦІЇ БАЗИ ДАНИХ

### Якщо змінили моделі даних - накатити міграції
```powershell
cd C:\Users\samoilenkod\source\repos\Winservice\WebApp

# Перевірити статус
dotnet ef migrations list

# Додати нову міграцію (якщо змінили модель)
dotnet ef migrations add MigrationName

# Накатити на БД
dotnet ef database update
```

### Видалити всі дані та пересоздати БД
```powershell
# НЕБЕЗПЕЧНО! Видалить усі дані!
dotnet ef database drop --force
dotnet ef database update
```

---

## 🧪 ТЕСТУВАННЯ

### Запустити юніт-тести
```powershell
cd C:\Users\samoilenkod\source\repos\Winservice

# Якщо є TestProject
dotnet test
```

### Тести шляхів/URL-ів
```
GET /import/excel           # Форма завантаження
POST /import/excel          # Обробка файлу
GET /import/list            # Таблиця з даними
POST /import/add            # Додати запис
POST /import/delete/id      # Видалити запис
```

---

## 📁 ВАЖЛИВІ ФАЙЛИ

### Конфігурація приложения
```
📄 WebApp/appsettings.json       # Конфіг для всіх середовищ
📄 WebApp/appsettings.Development.json  # Конфіг для локалу
📄 WebApp/appsettings.Production.json   # Конфіг для серверу
```

### Точка входу
```
📄 WebApp/Program.cs             # Налаштування Dependency Injection, middleware
```

### Шляблони (Views)
```
📁 WebApp/Views/
  ├── Shared/Layout.cshtml       # Головний шаблон
  ├── Home/Index.cshtml          # Стартова сторінка
  ├── Import/Excel.cshtml        # Форма імпорту
  └── Import/List.cshtml         # Таблиця з даними
```

### Стилі
```
📁 WebApp/wwwroot/
  ├── css/                        # CSS файли
  └── js/                         # JavaScript файли
```

---

## 🐛 DEBUGGING

### Debug в Visual Studio
1. Відкрити **Debug** → **Windows** → **Debug Output**
2. Встановити breakpoints (клік на ліву колонку)
3. Натиснути F5 для запуску
4. Натиснути F10 для кроку

### Логи приложения
```
📁 WebApp/bin/Debug/net8.0/logs/  # Папка з логами (але потрібно налаштувати Serilog)
```

### Консоль браузера (F12)
```
1. Натиснути F12 у браузері
2. Перейти на вкладку "Console" та "Network"
3. Перевіряємо помилки та запити
```

---

## 🔍 ПЕРЕВІРКИ ЗДОРОВ'Я

### Приложение запущено?
```powershell
# Перевіряємо, слухає на 5000
netstat -ano | findstr ":5000"

# Повинна бути строка з dotnet.exe
```

### БД доступна?
```powershell
# Тест підключення
sqlcmd -S (localdb)\mssqllocaldb -Q "SELECT @@VERSION"
```

### Браузер видить приложение?
```powershell
# Запит до сервера
Invoke-WebRequest http://localhost:5000 -UseBasicParsing
```

---

## 📝 ЧАСТІ ПОМИЛКИ

### Помилка: "Port 5000 is already in use"
```powershell
# Найти процес, який використовує порт
netstat -ano | findstr ":5000"

# Отримуємо PID (останній стовпець), потім
taskkill /PID <PID> /F
```

### Помилка: "Cannot connect to database"
```
Перевіряємо:
1. SQL Server запущен: services.msc
2. Рядок підключення в appsettings.json
3. Бази DB_SAIT існує
```

### Помилка: "Assembly not found"
```powershell
# Очистити та перебудувати
dotnet clean
dotnet restore
dotnet build
```

---

## 📦 РОЗГОРТАННЯ НА СЕРВЕР

### Коли локальне тестування пройдено:
```powershell
# 1. Перейти в корінь проекту
cd C:\Users\samoilenkod\source\repos\Winservice

# 2. Запустити скрипт розгортання
.\deploy.ps1

# 3. Скопіювати файли на сервер
# Шляхи для копіювання:
# З: .\artifacts\deploy\WebApp\*
# На: C:\Apps\WebApp\ (на серверу)
```

### Детальні інструкції розгортання
```
📄 DEPLOYMENT.md  # Повна документація для серверу
```

---

## 📚 ДОКУМЕНТАЦІЯ

```
📄 LOCAL_SETUP.md (ЦЕЙ ФАЙЛ)     # Локальний запуск
📄 DEPLOYMENT.md                  # Розгортання на сервер
📄 GETTING_STARTED.md             # Початок роботи (якщо є)
📄 PROJECT_OVERVIEW.md            # Огляд проекту (якщо є)
```

---

## ❓ ШВИДКА ДОВІДКА

| Завдання | Команда |
|----------|---------|
| **Запустити локально** | `cd WebApp && dotnet run` |
| **Опублікувати** | `.\deploy.ps1` |
| **Оновити БД** | `dotnet ef database update` |
| **Очистити артефакти** | `dotnet clean` |
| **Тести** | `dotnet test` |
| **Git commit** | `git add . && git commit -m "message"` |
| **Git push** | `git push origin master` |

---

## 👤 КОНТАКТИ / ПОМОЩЬ

- **Розробник**: Samoilenko D
- **Репозиторій**: C:\Users\samoilenkod\source\repos\Winservice
- **GitHub**: https://github.com/ad_sait/ad_sait (public)
- **Версія**: ASP.NET Core 8.0
- **БД**: SQL Server (localdb)\mssqllocaldb

---

**Успіх в розробці! 🚀**
