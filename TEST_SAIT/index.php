<?php
require __DIR__ . '/config.php';

if (isset($_SESSION['user'])) {
    // already logged in
    header('Location: main.php');
    exit;
}

$error = '';
if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $u = $_POST['username'] ?? '';
    $p = $_POST['password'] ?? '';
    $pdo = getPDO();
    $stmt = $pdo->prepare('SELECT id, username, password, is_admin FROM USERS WHERE username = :u');
    $stmt->execute([':u' => $u]);
    $row = $stmt->fetch(PDO::FETCH_ASSOC);
    if ($row && password_verify($p, $row['password'])) {
        $_SESSION['user'] = ['id' => $row['id'], 'username' => $row['username'], 'is_admin' => (int)$row['is_admin']];
        header('Location: main.php');
        exit;
    } else {
        $error = 'Неверные учетные данные';
    }
}

$currentPage = basename($_SERVER['PHP_SELF']);
$logExpanded = in_array($currentPage, ['current_log.php', 'general_log.php'], true);
?>
<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <title>TEST_SAIT — Вход</title>
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
            <h1>Вход</h1>
            <?php if ($error): ?><div class="error"><?php echo htmlspecialchars($error); ?></div><?php endif; ?>
            <form method="post">
                <label>Пользователь
                    <input name="username" required>
                </label>
                <label>Пароль
                    <input name="password" type="password" required>
                </label>
                <button type="submit">Войти</button>
            </form>
            <p>Тестовые: admin/adminpass, user/userpass</p>
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
