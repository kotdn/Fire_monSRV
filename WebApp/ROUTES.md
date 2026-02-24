# Маршруты приложения

## Общая информация

Приложение использует **стандартную ASP.NET Core маршрутизацию** вместо PHP сегментов URL.

### Формат маршрутов

```
/ControllerName/ActionName/Parameters
```

Например:
- `/account/login` → AccountController.Login()
- `/home/index` → HomeController.Index()
- `/admin/deletemessage/5` → AdminController.DeleteMessage(5)

---

## Полный список маршрутов

### 🔐 Аутентификация (`/account`)

| Метод | Путь | Описание |
|-------|------|---------|
| GET | `/account/login` | Показать форму логина |
| POST | `/account/login` | Обработка логина |
| POST | `/account/logout` | Выход из системы |

**Примеры:**
```html
<!-- Форма логина -->
<form method="post" action="/account/login">
    <input type="text" name="username" required>
    <input type="password" name="password" required>
    <button type="submit">Войти</button>
</form>

<!-- Выход -->
<form method="post" action="/account/logout">
    <button type="submit">Выйти</button>
</form>
```

---

### 📱 Главная страница (`/home`)

| Метод | Путь | Описание |
|-------|------|---------|
| GET | `/home` или `/home/index` | Показать главную страницу |

**Компонент:**
- Показывает все сообщения из БД
- Требует авторизацию
- Для авторизованных пользователей

---

### ⚙️ Администрирование (`/admin`)

| Метод | Путь | Описание |
|-------|------|---------|
| GET | `/admin` | Админпанель со всеми сообщениями |
| POST | `/admin/addmessage` | Добавить новое сообщение |
| POST | `/admin/updatemessage/5` | Обновить сообщение #5 |
| POST | `/admin/deletemessage/5` | Удалить сообщение #5 |

**Примеры:**
```html
<!-- Форма добавления -->
<form method="post" action="/admin/addmessage">
    <textarea name="message"></textarea>
    <button type="submit">Добавить</button>
</form>

<!-- Обновление сообщения -->
<form method="post" action="/admin/updatemessage/5">
    <textarea name="message">Новый текст</textarea>
    <button type="submit">Обновить</button>
</form>

<!-- Удаление сообщения -->
<form method="post" action="/admin/deletemessage/5">
    <button type="submit" onclick="return confirm('Удалить?')">Удалить</button>
</form>
```

**Требования:**
- Только для админов (IsAdmin = true)
- Требует авторизацию

---

### 🏠 Корневой путь

| Метод | Путь | Описание |
|-------|------|---------|
| GET | `/` | Редирект на `/account/login` |

---

## Отличия от PHP версии

| Функция | PHP | ASP.NET Core |
|---------|-----|-------------|
| **Файл** | `index.php` | `/` → редирект на `/account/login` |
| **Логин** | `index.php` (POST) | `/account/login` (POST) |
| **Главная** | `main.php` | `/home` |
| **Админка** | `admin.php` | `/admin` |
| **Логи** | `current_log.php`, `general_log.php` | *Не реализовано* |
| **Выход** | `logout.php` | `/account/logout` |

---

## Примечания

1. **Нет расширений файлов** — все URL'ы заканчиваются на действие (action), а не на `.php`
2. **Чувствительность к регистру** — контроллеры и действия могут быть заданы в любом регистре, но рекомендуется использовать стандарт ASP.NET Core
3. **ID в URL** — используется для передачи ID (например, `/admin/deletemessage/5`)
4. **Form методы** — используются POST для изменения данных, GET для просмотра

---

## Перенаправления

| От | К | Причина |
|----|---|---------|
| `/` | `/account/login` | Неавторизованный доступ |
| `/account/login` | `/home` | Пользователь уже авторизован |
| Любая страница без сессии | `/account/login` | Сессия истекла |
| `/admin` | Forbid | Не админ |

