<?php
require __DIR__ . '/config.php';

$pdo = getPDO();

if (defined('DB_TYPE') && DB_TYPE === 'mssql') {
    // Check and create USERS table
    $cnt = $pdo->query("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'USERS'")->fetchColumn();
    if ($cnt == 0) {
        $pdo->exec("CREATE TABLE USERS (
            id INT IDENTITY(1,1) PRIMARY KEY,
            username NVARCHAR(255) NOT NULL UNIQUE,
            password NVARCHAR(255) NOT NULL,
            is_admin BIT NOT NULL DEFAULT 0
        )");
    }

    $cnt = $pdo->query("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'main'")->fetchColumn();
    if ($cnt == 0) {
        $pdo->exec("CREATE TABLE main (
            id INT IDENTITY(1,1) PRIMARY KEY,
            txt_mes NVARCHAR(MAX)
        )");
    }

    $stmt = $pdo->query('SELECT COUNT(*) FROM USERS');
    if ($stmt->fetchColumn() == 0) {
        $insert = $pdo->prepare('INSERT INTO USERS (username, password, is_admin) VALUES (:u, :p, :a)');
        $insert->execute([':u' => 'admin', ':p' => password_hash('adminpass', PASSWORD_DEFAULT), ':a' => 1]);
        $insert->execute([':u' => 'user', ':p' => password_hash('userpass', PASSWORD_DEFAULT), ':a' => 0]);
    }

    $stmt = $pdo->query('SELECT COUNT(*) FROM main');
    if ($stmt->fetchColumn() == 0) {
        $pdo->prepare('INSERT INTO main (txt_mes) VALUES (:m)')->execute([':m' => 'Welcome — исходное сообщение из таблицы main.']);
    }

    echo "MSSQL database tables ensured in " . (defined('MSSQL_DATABASE') ? MSSQL_DATABASE : '') . "\n";
    echo "Users: admin/adminpass, user/userpass\n";
    echo "Run create_db.php once, then secure it.\n";

} else {
    // default: sqlite
    if (!is_dir(__DIR__ . '/data')) {
        mkdir(__DIR__ . '/data', 0755, true);
    }

    $pdo->exec("CREATE TABLE IF NOT EXISTS USERS (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        username TEXT NOT NULL UNIQUE,
        password TEXT NOT NULL,
        is_admin INTEGER NOT NULL DEFAULT 0
    )");

    $pdo->exec("CREATE TABLE IF NOT EXISTS main (
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        txt_mes TEXT
    )");

    $stmt = $pdo->prepare('SELECT COUNT(*) FROM USERS');
    $stmt->execute();
    if ($stmt->fetchColumn() == 0) {
        $insert = $pdo->prepare('INSERT INTO USERS (username, password, is_admin) VALUES (:u, :p, :a)');
        $insert->execute([':u' => 'admin', ':p' => password_hash('adminpass', PASSWORD_DEFAULT), ':a' => 1]);
        $insert->execute([':u' => 'user', ':p' => password_hash('userpass', PASSWORD_DEFAULT), ':a' => 0]);
    }

    $stmt = $pdo->prepare('SELECT COUNT(*) FROM main');
    $stmt->execute();
    if ($stmt->fetchColumn() == 0) {
        $pdo->prepare('INSERT INTO main (txt_mes) VALUES (:m)')->execute([':m' => 'Welcome — исходное сообщение из таблицы main.']);
    }

    echo "SQLite database initialized at " . DB_PATH . "\n";
    echo "Users: admin/adminpass, user/userpass\n";
    echo "Run create_db.php once, then remove or secure it.\n";
}
