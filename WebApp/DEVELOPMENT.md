# Инструкция по разработке

## Быстрый старт

### Требования
- .NET 8 SDK или выше
- Visual Studio 2022 (рекомендуется) или VS Code

### Установка и запуск

**Вариант 1: Используя батник (Windows)**
```bash
cd WebApp
run.bat
```

**Вариант 2: Вручную**
```bash
cd WebApp

# Восстановление зависимостей
dotnet restore

# Запуск приложения
dotnet run
```

Приложение будет доступно на **http://localhost:5000**

---

## Структура проекта

```
WebApp/
├── Models/                          # Модели данных
│   ├── User.cs                      # Пользователь (ID, Username, Password, IsAdmin)
│   ├── Message.cs                   # Сообщение (ID, TxtMes)
│   └── UserSession.cs               # Сессия пользователя
│
├── Controllers/                     # Контроллеры (бизнес-логика)
│   ├── AccountController.cs         # Аутентификация (логин/выход)
│   ├── HomeController.cs            # Главная страница
│   └── AdminController.cs           # Администрирование
│
├── Data/                           # Доступ к данным
│   ├── AppDbContext.cs             # Entity Framework контекст
│   └── DbInitializer.cs            # Инициализация и seeding БД
│
├── Views/                          # Razor Views (HTML представления)
│   ├── Account/
│   │   └── Login.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   ├── Admin/
│   │   └── Index.cshtml
│   └── Shared/
│       └── _Layout.cshtml
│
├── wwwroot/                        # Статические файлы
│   └── style.css
│
├── Program.cs                      # Конфигурация приложения
├── WebApp.csproj                   # Конфигурация проекта
├── appsettings.json                # Конфиг БД и логирования
└── README.md                       # Основная документация
```

---

## Как добавить новый маршрут

### 1. Добавить метод в контроллер

```csharp
// Controllers/HomeController.cs

public class HomeController : Controller
{
    // ... existing code ...

    [HttpGet]
    public IActionResult MyNewPage()
    {
        return View();
    }
}
```

### 2. Создать View

```bash
# Views/Home/MyNewPage.cshtml
@{
    Layout = "_Layout";
    ViewBag.Title = "Моя новая страница";
}

<div class="page split">
    <aside class="side">
        <!-- Меню слева (опционально) -->
    </aside>
    <main class="content">
        <h1>Привет, мир!</h1>
    </main>
</div>
```

### 3. Использовать маршрут

```html
<a href="/home/mynewpage">Перейти на новую страницу</a>
```

---

## Как добавить нового пользователя

### Вариант 1: Через базу данных

```csharp
// В DbInitializer.cs добавить:

new User
{
    Username = "testuser",
    Password = BCrypt.Net.BCrypt.HashPassword("testpass123", 10),
    IsAdmin = false
}
```

### Вариант 2: Через администраторский интерфейс (требует реализации)

Текущее приложение не имеет интерфейса для добавления пользователей. Это может быть добавлено в админку.

---

## Как добавить новую таблицу БД

### 1. Создать модель

```csharp
// Models/MyEntity.cs

namespace WebApp.Models;

public class MyEntity
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
```

### 2. Добавить DbSet в контекст

```csharp
// Data/AppDbContext.cs

public DbSet<MyEntity> MyEntities { get; set; } = null!;
```

### 3. Создать миграцию

```bash
dotnet ef migrations add AddMyEntity
dotnet ef database update
```

### 4. Использовать в контроллере

```csharp
// Controllers/SomeController.cs

var entities = await _context.MyEntities.ToListAsync();
```

---

## Изменение типа БД

### SQLite (по умолчанию)

```json
{
  "DatabaseType": "sqlite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=data/app.db"
  }
}
```

### MSSQL

```json
{
  "DatabaseType": "mssql",
  "ConnectionStrings": {
    "MssqlConnection": "Server=(localdb)\\mssqllocaldb;Database=WebAppDb;Trusted_Connection=true;"
  }
}
```

Затем пересоздать БД:
```bash
rm data/app.db
dotnet run
```

---

## Отладка

### Включить подробное логирование

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "Microsoft.EntityFrameworkCore": "Debug"
    }
  }
}
```

### Просмотр SQL запросов

Добавить в Program.cs:
```csharp
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.AddDebug();
});
```

---

## Справочник NuGet пакетов

- **Microsoft.EntityFrameworkCore** — ORM для работы с БД
- **Microsoft.EntityFrameworkCore.Sqlite** — драйвер SQLite
- **Microsoft.EntityFrameworkCore.SqlServer** — драйвер MSSQL
- **BCrypt.Net-Next** — хеширование паролей
- **Microsoft.AspNetCore.Session** — работа с сессиями

---

## Полезные команды

```bash
# Восстановить пакеты
dotnet restore

# Скомпилировать проект
dotnet build

# Запустить проект
dotnet run

# Создать миграцию
dotnet ef migrations add MigrationName

# Применить миграции
dotnet ef database update

# Удалить последнюю миграцию
dotnet ef migrations remove

# Очистить БД
rm data/app.db

# Опубликовать для продакшена
dotnet publish -c Release -o ./publish
```

---

## Возможные проблемы

### Ошибка подключения к БД
- **Решение:** Убедитесь, что папка `data/` существует и имеет права на запись

### Сессия не сохраняется
- **Решение:** Убедитесь, что `app.UseSession()` вызывается перед `app.MapControllerRoute()`

### Стили не загружаются
- **Решение:** Убедитесь, что `app.UseStaticFiles()` вызывается в Program.cs

### Ошибка "не найдено правило маршрутизации"
- **Решение:** Проверьте имя контроллера и действия (без "Controller" суффикса и "Async" для методов)
