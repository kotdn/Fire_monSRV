<?php
require __DIR__ . '/config.php';
if (!isset($_SESSION['user'])) {
    header('Location: index.php');
    exit;
}

$currentPage = basename($_SERVER['PHP_SELF']);
$logExpanded = in_array($currentPage, ['current_log.php', 'general_log.php'], true);

$pdo = getPDO();
$sql = (defined('DB_TYPE') && DB_TYPE === 'mssql')
    ? 'SELECT txt_mes FROM dbo.mes_text'
    : 'SELECT txt_mes FROM main';
$rows = $pdo->query($sql)->fetchAll(PDO::FETCH_COLUMN);
?>
<!doctype html>
<html>
<head>
    <meta charset="utf-8">
    <title>TEST_SAIT — Главная</title>
    <link rel="stylesheet" href="style.css">
</head>
<body>
<div class="page split">
    <aside class="side">
        <nav class="side-menu">
            <?php if (!empty($_SESSION['user']['is_admin'])): ?>
                <a href="admin.php">Админ</a>
            <?php endif; ?>
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
        <div class="content-topbar">
            <?php if (!empty($_SESSION['user']['is_admin'])): ?>
                <a class="btn-link" href="admin.php">Админка</a>
            <?php endif; ?>
            <a class="btn-link" href="logout.php">Выйти</a>
        </div>
        <div class="container">
            <h1>Главная</h1>
            <p>Вы вошли как: <?php echo htmlspecialchars($_SESSION['user']['username']); ?></p>
            <?php foreach ($rows as $r): ?>
                <div class="message"><?php echo nl2br(htmlspecialchars($r)); ?></div>
            <?php endforeach; ?>
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
