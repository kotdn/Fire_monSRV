# Запуск WebApp проекта

## 🚀 Первый запуск (облегченно)

### На Windows (самый простой способ)

1. Откройте терминал в папке `WebApp`
2. Запустите:
   ```bash
   run.bat
   ```
3. Откройте браузер: **http://localhost:5000**
4. Войдите с учетными данными:
   - **Login:** `admin`
   - **Password:** `adminpass`

### На Mac / Linux

1. Откройте терминал в папке `WebApp`
2. Запустите:
   ```bash
   dotnet restore
   dotnet run
   ```
3. Откройте браузер: **http://localhost:5000**

---

## 📋 Требования

- **[.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** или выше
- **Visual Studio 2022** (опционально, для удобства)

Проверить наличие .NET:
```bash
dotnet --version
```

---

## 🎯 Первые шаги в приложении

1. **Вход как администратор:**
   - Username: `admin`
   - Password: `adminpass`

2. **Перейти в админку:**
   - Нажмите кнопку "Админ" в левом меню

3. **Добавить новое сообщение:**
   - Заполните поле "Новое сообщение"
   - Нажмите "Добавить"

4. **Отредактировать сообщение:**
   - Нажмите "Edit" рядом с сообщением
   - В открывшейся форме измените текст

5. **Удалить сообщение:**
   - Нажмите "Delete" и подтвердите

6. **Выход:**
   - Нажмите "Выйти" или кнопку "Logout"

---

## 🗂️ Файлы проекта

| Файл | Описание |
|------|---------|
| `WebApp.csproj` | Конфиг проекта и зависимости |
| `Program.cs` | Конфигурация приложения |
| `appsettings.json` | Конфиг БД и логирования |
| `Controllers/` | Логика приложения |
| `Models/` | Структуры данных |
| `Views/` | HTML представления |
| `Data/AppDbContext.cs` | Доступ к БД |
| `wwwroot/style.css` | Стили |
| `run.bat` | Батник для запуска (Windows) |

---

## 🔌 Настройка БД

### SQLite (по умолчанию)
Данные хранятся в `data/app.db`. Ничего консфигурировать не нужно.

### MSSQL (опционально)
Отредактируйте `appsettings.json`:
```json
{
  "DatabaseType": "mssql",
  "ConnectionStrings": {
    "MssqlConnection": "Server=(localdb)\\mssqllocaldb;Database=WebAppDb;Trusted_Connection=true;"
  }
}
```

Затем удалите старую БД и пересоздайте:
```bash
rm data/app.db
dotnet run
```

---

## 📚 Документация

- **[README.md](README.md)** — Основная информация
- **[ROUTES.md](ROUTES.md)** — Все маршруты (URLs)
- **[DEVELOPMENT.md](DEVELOPMENT.md)** — Инструкции по разработке
- **[MIGRATION.md](MIGRATION.md)** — Как мы мигрировали с PHP

---

## 🐛 Возможные проблемы

### "dotnet: command not found"
- **Решение:** [Установите .NET SDK](https://dotnet.microsoft.com/download)

### "Access to the path 'data' is denied"
- **Решение:** Создайте папку `data/` вручную или запустите от администратора

### Порт 5000 занят
- **Решение:** В `Properties/launchSettings.json` измените `"applicationUrl"` на другой порт (например, `5001`)

### БД не создалась
- **Решение:** Удалите папку `bin/` и `obj/` и запустите заново

---

## 💾 Открыть в Visual Studio

1. Откройте `webapp.sln` (если находитесь в папке Winservice)
2. В Solution Explorer найдите проект `WebApp`
3. Нажмите правой кнопкой → "Set as Startup Project"
4. Нажмите F5 для запуска с отладкой

---

## 🚀 Готово!

Теперь вы можете:
- ✅ Запустить приложение
- ✅ Добавить свои маршруты (см. DEVELOPMENT.md)
- ✅ Изменить дизайн (см. wwwroot/style.css)
- ✅ Добавить новые таблицы в БД

---

## 📞 Помощь

- Документация: см. папку WebApp
- Примеры код: см. Controllers/
- Вопросы о миграции: см. MIGRATION.md
