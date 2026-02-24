# WebApp — ASP.NET Core версия

Новый проект на ASP.NET Core с полной переработкой из PHP на C#.

## 🎯 Общая информация

Это веб-приложение для управления сообщениями с двумя уровнями доступа: **пользователь** (просмотр) и **администратор** (управление).

**Основано на:** PHP сайт `TEST_SAIT/` из того же решения.

---

## 📂 Структура проекта

```
WebApp/
├── Controllers/          # Контроллеры с логикой
│   ├── AccountController.cs    # Логин/Выход
│   ├── HomeController.cs       # Главная страница
│   └── AdminController.cs      # Админпанель
├── Models/              # Модели данных
│   ├── User.cs          # Пользователь
│   ├── Message.cs       # Сообщение
│   └── UserSession.cs   # Сессия
├── Data/                # Доступ к данным
│   ├── AppDbContext.cs  # Entity Framework контекст
│   └── DbInitializer.cs # Инициализация БД
├── Views/               # Razor Views
│   ├── Account/Login.cshtml
│   ├── Home/Index.cshtml
│   ├── Admin/Index.cshtml
│   └── Shared/_Layout.cshtml
├── wwwroot/
│   └── style.css        # Стили
├── Program.cs           # Конфигурация и маршруты
├── appsettings.json     # Конфиг БД
├── WebApp.csproj        # Конфиг проекта
└── run.bat              # Батник для запуска
```

---

## 🚀 Быстрый старт

### Требования
- .NET 8 SDK или выше

### Запуск

**На Windows (самый простой способ):**
```bash
cd WebApp
run.bat
```

**Вручную (все ОС):**
```bash
cd WebApp
dotnet restore
dotnet run
```

Приложение будет доступно на **http://localhost:5000**

---

## 📘 Маршруты (Routes)

Приложение использует ASP.NET Core маршрутизацию вместо файлов PHP.

### Аутентификация (`/account`)
- `GET /account/login` — Форма логина
- `POST /account/login` — Обработка логина
- `POST /account/logout` — Выход

### Главная страница (`/home`)
- `GET /home` — Показать все сообщения

### Администрирование (`/admin`)
- `GET /admin` — Админпанель
- `POST /admin/addmessage` — Добавить сообщение
- `POST /admin/updatemessage/{id}` — Обновить сообщение
- `POST /admin/deletemessage/{id}` — Удалить сообщение

**Полная документация:** см. [ROUTES.md](ROUTES.md)

---

## 👥 Учетные данные по умолчанию

| Username | Password | Роль |
|----------|----------|------|
| admin | adminpass | Администратор |
| user | userpass | Пользователь |

---

## 🗄️ Базы данных

Поддерживаются две БД:

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

Измените конфиг в `appsettings.json` и пересоздайте БД.

---

## 📚 Документация

- **[ROUTES.md](ROUTES.md)** — Полный список маршрутов с примерами
- **[DEVELOPMENT.md](DEVELOPMENT.md)** — Инструкции по разработке
- **[MIGRATION.md](MIGRATION.md)** — Как мы мигрировали с PHP

---

## 🔧 Разработка

### Создать миграцию БД
```bash
dotnet ef migrations add {MigrationName}
dotnet ef database update
```

### Запустить с пересоздание БД
```bash
# Удалить старую БД (SQLite)
rm data/app.db

# Запустить (БД будет пересоздана)
dotnet run
```

### Скомпилировать для продакшена
```bash
dotnet publish -c Release -o ./publish
```

---

## 📝 Технологический стек

- **ASP.NET Core** 8.0
- **Entity Framework Core** — ORM
- **Razor Views** — HTML представления
- **BCrypt.Net-Next** — Хеширование пароля
- **SQLite / MSSQL** — БД

---

## 🚚 Отличия от PHP версии

✅ **Плюсы:**
- Нет расширений `.php` в URL'ах
- Строгая типизация моделей
- Entity Framework вместо PDO
- Встроенная аутентификация в Sessions
- Автоматические миграции БД
- RESTful маршруты
- Значительно лучше производительность

❌ **Минусы (пока не реализовано):**
- Логирование (`current_log.php`, `general_log.php`)
- Интерфейс для добавления пользователей

---

## 🐛 Поиск ошибок

### Ошибка подключения к БД
```
Access to the path is denied
```
**Решение:** Убедитесь, что папка `data/` существует и имеет права на запись.

### Сессия не сохраняется
```
Session is null
```
**Решение:** Убедитесь, что `app.UseSession()` вызывается перед `app.MapControllerRoute()` в Program.cs

### Стили не загружаются
```
style.css не найден
```
**Решение:** Убедитесь, что `app.UseStaticFiles()` вызывается в Program.cs

---

## 📄 Лицензия

Проект на основе решения Winservice.

---

## 📞 Поддержка

Для рекомендаций и улучшений обратитесь к документации:
- [DEVELOPMENT.md](DEVELOPMENT.md) — как добавить новую функцию
- [ROUTES.md](ROUTES.md) — как устроена маршрутизация
- [MIGRATION.md](MIGRATION.md) — как мы пришли к этой архитектуре
