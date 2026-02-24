# TEST_SAIT

Простой PHP-сайт с авторизацией и админкой, использующий SQLite по умолчанию. Подходит для запуска под Apache + PHP.

Как запустить

- Установите Apache и PHP (PHP >= 7.4). Для SQLite нужен `pdo_sqlite` (обычно включён). Для MS SQL понадобится `pdo_sqlsrv`.
- Скопируйте папку `TEST_SAIT` в ваш `DocumentRoot` или настройте VirtualHost на `.../TEST_SAIT`.
- Инициализируйте базу данных (один раз):

```bash
php create_db.php
```

По умолчанию это создаст `data/db.sqlite` и добавит двух пользователей: `admin`/`adminpass` (админ) и `user`/`userpass`.

- Затем откройте в браузере `http://localhost/TEST_SAIT/` (или настроенный хост).

Файлы

- `index.php` — стартовая страница (вход)
- `main.php` — главная страница после входа (запрос `SELECT txt_mes FROM main`)
- `admin.php` — админка (тоже авторизация через таблицу `USERS`; добавление пользователей, редактирование текста)
- `create_db.php` — скрипт создания/инициализации базы
- `config.php` — конфиг (поддерживает `sqlite` и `mssql`)

Безопасность

- После инициализации удалите или защитите `create_db.php`.
- Для работы в продакшне используйте HTTPS и установите надёжные пароли.

MS SQL

- Проект поддерживает MS SQL (Microsoft SQL Server) через драйвер `pdo_sqlsrv`.
- Скопируйте `config.local.php.example` в `config.local.php` и укажите `DB_TYPE` = `mssql` и параметры `MSSQL_SERVER`, `MSSQL_DATABASE`, `MSSQL_USER`, `MSSQL_PASS`.
- Установите расширения PHP для SQL Server (Windows):

	1. Скачайте и установите Microsoft Drivers for PHP for SQL Server для вашей версии PHP: https://learn.microsoft.com/sql/connect/php/download-drivers-php-sql-server
	2. В `php.ini` включите `extension=sqlsrv` и `extension=pdo_sqlsrv`, затем перезапустите Apache.

- После настройки, в каталоге `TEST_SAIT` выполните:

```bash
php create_db.php
```

Это создаст таблицы в указанной базе и добавит тестовых пользователей `admin`/`adminpass` и `user`/`userpass`.

Примечание: убедитесь, что база данных `MSSQL_DATABASE` создана заранее, либо у пользователя есть права на создание БД.
