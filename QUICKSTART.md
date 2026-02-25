# 📘 QUICKSTART GUIDE

## 🎯 Де шукати файли?

### 📍 Основна папка проекту:
```
C:\Users\samoilenkod\source\repos\Winservice
```

### 📍 Коли розгортаєш на сервер:
```
C:\Apps\WebApp  (на сервері)
```

---

## 🚀 ШВИДКИЙ СТАРТ

### Для розробників (локально)
```powershell
# 1️⃣ Клонувати / відкрити проект
cd C:\Users\samoilenkod\source\repos\Winservice

# 2️⃣ Запустити локально
cd WebApp
dotnet run

# 3️⃣ Открыть браузер
# http://localhost:5000/import/list
```

### Для admin'ів (сервер)
```powershell
# 1️⃣ На локальній машині опублікувати
.\deploy.ps1

# 2️⃣ На сервері встановити як сервіс
.\server-startup.ps1
# Вибрати: 4. Install as service

# 3️⃣ Перевірити
# http://СЕ РВЕР_IP:5000
```

---

## 📚 ДОКУМЕНТАЦІЯ

| Файл | Для кого | Про що |
|------|----------|-------|
| **LOCAL_SETUP.md** | 👨‍💻 Розробники | Як запустити локально, структура проекту, шляхи файлів |
| **DEPLOYMENT.md** | 🔧 Admin'и | Детальна інструкція розгортання (Windows Service, IIS, Console) |
| **SERVER_DEPLOYMENT.md** | 🖥️ Server'ы | Крок за кроком: копіювання файлів, конфіг, запуск |
| **deploy.ps1** | 👨‍💻 Розробники | PowerShell скрипт для публікації |
| **deploy.bat** | 👨‍💻 Розробники | Batch версія скрипту публікації |
| **server-startup.ps1** | 🖥️ Server'ы | Утиліта управління сервісом на сервері (меню) |
| **server-startup.bat** | 🖥️ Server'ы | Batch версія утиліти управління |

---

## 🗂️ СТРУКТУРА ПРОЕКТУ

```
Winservice/
├── 📂 WebApp/                      ← ГОЛОВНЕ ПРИЛОЖЕНИЕ
│   ├── Controllers/
│   │   └── ImportController.cs    ← Логіка імпорту Excel
│   ├── Models/
│   │   └── ImportData.cs          ← Модель даних
│   ├── Views/
│   │   ├── Import/
│   │   │   ├── Excel.cshtml       ← Форма загрузки
│   │   │   └── List.cshtml        ← Таблиця з 15 колонками 👈
│   │   └── ...
│   ├── Program.cs                 ← Точка входу
│   ├── appsettings.json           ← Конфіг
│   └── WebApp.csproj
│
├── 📄 LOCAL_SETUP.md              ← ЧИТАЙ СЮДИ! (локальний запуск)
├── 📄 DEPLOYMENT.md               ← ЧИТАЙ СЮДИ! (розгортання)
├── 📄 SERVER_DEPLOYMENT.md        ← ЧИТАЙ СЮДИ! (на сервері)
│
├── 📄 deploy.ps1                  ← Публікація (PowerShell)
├── 📄 deploy.bat                  ← Публікація (Batch)
│
├── 📄 server-startup.ps1          ← Управління сервісом
├── 📄 server-startup.bat          ← Управління сервісом
│
├── 📄 Winservice.sln              ← Visual Studio solution
└── 📁 artifacts/                  ← Результати публікації
    └── deploy/
        └── WebApp/                ← КОПІЮЄМО СЮДИ НА СЕРВЕР!
```

---

## ⚡ ТИПОВІ КОМАНДИ

### Локально (розробка)
```powershell
# Запустити приложение
cd WebApp
dotnet run

# Запустити у Release
dotnet run -c Release

# Очистити / перебудувати
dotnet clean
dotnet build

# Тести
dotnet test
```

### Публікація
```powershell
# Запустити скрипт розгортання
.\deploy.ps1

# Результат появивиться в:
# .\artifacts\deploy\WebApp\
```

### На сервері (Windows Service)
```powershell
# Відкрити утиліту
.\server-startup.ps1

# Або вручну:
sc start WebApp      # запустити
sc stop WebApp       # остановить
sc query WebApp      # статус

# Лог'и
sc query WebApp
```

---

## 🔧 ОСНОВНІ ФАЙЛИ

### appsettings.json - КОНФІГУРАЦІЯ
```
📍 Локально:     WebApp\appsettings.json
📍 На сервері:   C:\Apps\WebApp\appsettings.json

Змінювати:
- ConnectionStrings → Database path
- Urls → порт (5000, 80, 443)
- Logging → рівень логування
```

### ImportController.cs - ЛОГІКА ІМПОРТУ
```
📍 WebApp\Controllers\ImportController.cs

Що робить:
- Excel() GET     → Форма завантаження
- Excel() POST    → Аналіз файлу (читає Excel, показує структуру)
- List()          → Виводить усі імпортовані дані
- Add()           → Додати новий запис
- Delete(id)      → Видалити запис
```

### List.cshtml - ТАБЛИЦЯ З 15 КОЛОНОК
```
📍 WebApp\Views\Import\List.cshtml

Показує:
1. ID
2. Рік
3. № вводу
4. Постачальник ← (ЦЕ КЛЮЧОВА КОЛОНКА для перевірки)
5-15. Інші дані

Особливість:
- Горизонтальний скрол
- Ultra-compact (10px шрифт, 2px 3px padd)
- Все на одній сторінці
```

---

## 🐛 НАЙТИПІЧНІШІ ПРОБЛЕМИ

| Проблема | Рішення |
|----------|---------|
| **Port 5000 in use** | `netstat -ano \| findstr ":5000"` та `taskkill /PID <PID>` |
| **Cannot connect DB** | Перевірити SQL Server запущена, рядок підключення |
| **Cannot find exe** | Запустити `deploy.ps1` першим |
| **Service not found** | Запустити `server-startup.ps1` → вибрати "4. Install" |
| **Access Denied** | Запустити PowerShell як адміністратор |

---

## 📞 ФАЙЛИ ДОВІДКИ (очевидно)

```
LOCAL_SETUP.md         👈 ЧИТАЙ СЮДИ! для локального запуску
DEPLOYMENT.md          👈 ЧИТАЙ СЮДИ! для розгортання
SERVER_DEPLOYMENT.md   👈 ЧИТАЙ СЮДИ! для сервера
```

---

## ✅ ГОТОВО?

1. ✅ **Для локального запуску**: Читай `LOCAL_SETUP.md`
2. ✅ **Для розгортання на сервер**: Читай `DEPLOYMENT.md` та `SERVER_DEPLOYMENT.md`
3. ✅ **Управління сервісом**: Використовуй `server-startup.ps1`

**Успіхів! 🚀**
