<?php
session_start();

// Load local overrides (create config.local.php from example if needed)
$local = __DIR__ . '/config.local.php';
if (file_exists($local)) {
    include $local;
}

// Default DB type: 'sqlite' or 'mssql'. Override in config.local.php
if (!defined('DB_TYPE')) define('DB_TYPE', 'sqlite');

if (DB_TYPE === 'sqlite') {
    if (!defined('DB_PATH')) define('DB_PATH', __DIR__ . '/data/db.sqlite');
}

function getPDO()
{
    if (DB_TYPE === 'mssql') {
        if (!defined('MSSQL_SERVER') || !defined('MSSQL_DATABASE')) {
            throw new Exception('MSSQL settings not defined. Copy config.local.php.example to config.local.php and set MSSQL_SERVER, MSSQL_DATABASE, MSSQL_USER, MSSQL_PASS');
        }
        $server = MSSQL_SERVER;
        $port = defined('MSSQL_PORT') ? MSSQL_PORT : 1433;
        $database = MSSQL_DATABASE;
        $user = defined('MSSQL_USER') ? MSSQL_USER : '';
        $pass = defined('MSSQL_PASS') ? MSSQL_PASS : '';
        $isLocalDb = stripos($server, '(localdb)') === 0;
        $dsn = $isLocalDb
            ? "sqlsrv:Server={$server};Database={$database}"
            : "sqlsrv:Server={$server},{$port};Database={$database}";
        $pdo = new PDO($dsn, $user, $pass);
        $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
        return $pdo;
    }

    // sqlite
    if (!is_dir(__DIR__ . '/data')) mkdir(__DIR__ . '/data', 0755, true);
    $pdo = new PDO('sqlite:' . DB_PATH);
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);
    return $pdo;
}
