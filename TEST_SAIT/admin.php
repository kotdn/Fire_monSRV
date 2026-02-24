<?php
require __DIR__ . '/config.php';
if (!isset($_SESSION['user']) || empty($_SESSION['user']['is_admin'])) {
    header('Location: index.php');
    exit;
}

$currentPage = basename($_SERVER['PHP_SELF']);
$logExpanded = in_array($currentPage, ['current_log.php', 'general_log.php'], true);

$pdo = getPDO();
$msg = '';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    if (isset($_POST['txt_mes'])) {
        $txt = $_POST['txt_mes'];
        if (defined('DB_TYPE') && DB_TYPE === 'mssql') {
            $stmt = $pdo->prepare('SELECT TOP 1 id FROM dbo.mes_text ORDER BY id');
            $stmt->execute();
            $row = $stmt->fetch(PDO::FETCH_ASSOC);
            if ($row) {
                $pdo->prepare('UPDATE dbo.mes_text SET txt_mes = :m WHERE id = :id')->execute([':m' => $txt, ':id' => $row['id']]);
            } else {
                $pdo->prepare('INSERT INTO dbo.mes_text (txt_mes) VALUES (:m)')->execute([':m' => $txt]);
            }
        } else {
            $stmt = $pdo->prepare('SELECT id FROM main LIMIT 1');
            $stmt->execute();
            $row = $stmt->fetch(PDO::FETCH_ASSOC);
            if ($row) {
                $pdo->prepare('UPDATE main SET txt_mes = :m WHERE id = :id')->execute([':m' => $txt, ':id' => $row['id']]);
            } else {
                $pdo->prepare('INSERT INTO main (txt_mes) VALUES (:m)')->execute([':m' => $txt]);
            }
        }
        $msg = 'Сообщение сохранено.';
    }
    if (isset($_POST['new_user']) && $_POST['new_user'] !== '') {
        $nu = $_POST['new_user'];
        $pw = $_POST['new_pass'] ?? '';
        $is_admin = isset($_POST['is_admin']) ? 1 : 0;
        if ($pw === '') {
            $msg = 'Пароль обязателен для нового пользователя.';
        } else {
            $pwh = password_hash($pw, PASSWORD_DEFAULT);
            try {
                $pdo->prepare('INSERT INTO USERS (username, password, is_admin) VALUES (:u, :p, :a)')
                    ->execute([':u' => $nu, ':p' => $pwh, ':a' => $is_admin]);
                $msg = 'Пользователь добавлен.';
            } catch (Exception $e) {
                $msg = 'Ошибка добавления: ' . $e->getMessage();
            }
        }
    }
}

$users = $pdo->query('SELECT id, username, is_admin FROM USERS')->fetchAll(PDO::FETCH_ASSOC);
$current = (defined('DB_TYPE') && DB_TYPE === 'mssql')
    ? $pdo->query('SELECT TOP 1 txt_mes FROM dbo.mes_text ORDER BY id')->fetchColumn()
    : $pdo->query('SELECT txt_mes FROM main LIMIT 1')->fetchColumn();
?>
<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <title>TEST_SAIT — Админка</title>
    <link rel="stylesheet" href="style.css">
</head>
<body>
<div class="page split">
    <aside class="side">
        <nav class="side-menu">
            <a href="admin.php">Админ</a>
            <div class="menu-group" data-collapsible>
                <button class="menu-toggle" type="button" aria-expanded="<?php echo $logExpanded ? 'true' : 'false'; ?>">
                    <span class="toggle-icon"><?php echo $logExpanded ? '-' : '+'; ?></span>
                    <span class="menu-title">Логи</span>
                </button>
                <div class="submenu-list"<?php echo $logExpanded ? '' : ' hidden'; ?>>
                    <a class="submenu" href="current_log.php">Текущий лог</a>
                    <a class="submenu" href="general_log.php">Общий лог</a>
                </div>
            </div>
            <a href="logout.php">Выход</a>
        </nav>
    </aside>
    <main class="content">
        <div class="container">
            <h1>Админка</h1>
            <p><?php echo htmlspecialchars($_SESSION['user']['username']); ?> — администратор</p>
            <?php if ($msg): ?><div class="info"><?php echo htmlspecialchars($msg); ?></div><?php endif; ?>

            <h2>Текст на главной</h2>
            <form method="post">
                <textarea name="txt_mes" rows="6"><?php echo htmlspecialchars($current); ?></textarea>
                <button type="submit">Сохранить</button>
            </form>

            <h2>Пользователи</h2>
            <table>
                <tr><th>ID</th><th>Имя</th><th>Роль</th></tr>
                <?php foreach ($users as $u): ?>
                    <tr><td><?php echo $u['id']; ?></td><td><?php echo htmlspecialchars($u['username']); ?></td><td><?php echo $u['is_admin'] ? 'admin' : 'user'; ?></td></tr>
                <?php endforeach; ?>
            </table>

            <h3>Добавить пользователя</h3>
            <form method="post">
                <label>Имя: <input name="new_user" required></label>
                <label>Пароль: <input name="new_pass" required></label>
                <label><input type="checkbox" name="is_admin"> admin</label>
                <button type="submit">Добавить</button>
            </form>

            <p><a href="main.php">Вернуться</a> | <a href="logout.php">Выйти</a></p>
        </div>
    </main>
</div>
<script>
document.querySelectorAll('[data-collapsible]').forEach((group) => {
    const btn = group.querySelector('.menu-toggle');
    const panel = group.querySelector('.submenu-list');
    const icon = btn ? btn.querySelector('.toggle-icon') : null;
    if (!btn || !panel) return;
    btn.addEventListener('click', () => {
        const expanded = btn.getAttribute('aria-expanded') === 'true';
        btn.setAttribute('aria-expanded', expanded ? 'false' : 'true');
        panel.hidden = expanded;
        if (icon) icon.textContent = expanded ? '+' : '-';
    });
});
</script>
</body>
</html>
