using System.Collections.Generic;

namespace RDPMonitor
{
    public static class Lang
    {
        private static Dictionary<string, Dictionary<string, string>> translations = new Dictionary<string, Dictionary<string, string>>
        {
            ["MAIN_TITLE"] = new Dictionary<string, string> { ["UA"] = "Монітор безпеки RDP", ["EN"] = "RDP Security Monitor" },
            ["SERVICE_STATUS_HEADER"] = new Dictionary<string, string> { ["UA"] = "Статус служби:", ["EN"] = "Service Status:" },
            ["SERVICE_CHECKING"] = new Dictionary<string, string> { ["UA"] = "Перевірка статусу...", ["EN"] = "Checking status..." },
            ["SERVICE_STATUS_RUNNING"] = new Dictionary<string, string> { ["UA"] = "Працює", ["EN"] = "Running" },
            ["SERVICE_STATUS_STOPPED"] = new Dictionary<string, string> { ["UA"] = "Зупинено", ["EN"] = "Stopped" },
            ["SERVICE_STATUS_UNKNOWN"] = new Dictionary<string, string> { ["UA"] = "Невідомо", ["EN"] = "Unknown" },
            
            ["BTN_START"] = new Dictionary<string, string> { ["UA"] = "Запустити службу", ["EN"] = "Start Service" },
            ["BTN_STOP"] = new Dictionary<string, string> { ["UA"] = "Зупинити службу", ["EN"] = "Stop Service" },
            ["BTN_REFRESH"] = new Dictionary<string, string> { ["UA"] = "Оновити", ["EN"] = "Refresh" },
            ["BTN_UNLOCK"] = new Dictionary<string, string> { ["UA"] = "Розблокувати IP", ["EN"] = "Unlock IP" },
            ["BTN_ADD"] = new Dictionary<string, string> { ["UA"] = "Додати", ["EN"] = "Add" },
            ["BTN_REMOVE"] = new Dictionary<string, string> { ["UA"] = "Видалити", ["EN"] = "Remove" },
            ["BTN_BLOCK"] = new Dictionary<string, string> { ["UA"] = "Заблокувати IP", ["EN"] = "Block IP" },
            ["BTN_SAVE"] = new Dictionary<string, string> { ["UA"] = "Зберегти налаштування", ["EN"] = "Save Config" },
            ["BTN_ADD_LEVEL"] = new Dictionary<string, string> { ["UA"] = "Додати рівень", ["EN"] = "Add Level" },
            ["BTN_REMOVE_LEVEL"] = new Dictionary<string, string> { ["UA"] = "Видалити рівень", ["EN"] = "Remove Level" },
            
            ["CONFIG_HEADER"] = new Dictionary<string, string> { ["UA"] = "Конфігурація:", ["EN"] = "Configuration:" },
            ["CONFIG_PORT"] = new Dictionary<string, string> { ["UA"] = "Порт:", ["EN"] = "Port:" },
            ["LOADING"] = new Dictionary<string, string> { ["UA"] = "Завантаження...", ["EN"] = "Loading..." },
            
            ["TAB_CURRENT_LOGS"] = new Dictionary<string, string> { ["UA"] = "Поточні логи", ["EN"] = "Current Logs" },
            ["TAB_BANNED_IPS"] = new Dictionary<string, string> { ["UA"] = "Заблоковані IP", ["EN"] = "Banned IPs" },
            ["TAB_WHITE_LIST"] = new Dictionary<string, string> { ["UA"] = "Білий список", ["EN"] = "White List" },
            ["TAB_MANUAL_BLOCK"] = new Dictionary<string, string> { ["UA"] = "Ручне блокування", ["EN"] = "Manual Block" },
            ["TAB_SETTINGS"] = new Dictionary<string, string> { ["UA"] = "Налаштування", ["EN"] = "Settings" },
            
            ["SECTION_BANNED_IPS"] = new Dictionary<string, string> { ["UA"] = "Заблоковані IP-адреси:", ["EN"] = "Blocked IPs:" },
            ["SECTION_WHITE_LIST"] = new Dictionary<string, string> { ["UA"] = "Білий список IP (авторозблокування):", ["EN"] = "IP Whitelist (Auto-unlock):" },
            ["SECTION_MANUAL_BLOCK"] = new Dictionary<string, string> { ["UA"] = "Вручну заблокувати IP:", ["EN"] = "Manually Block IP:" },
            ["SECTION_BLOCK_LEVELS"] = new Dictionary<string, string> { ["UA"] = "Налаштування рівнів блокування:", ["EN"] = "Block Levels Configuration:" },
            ["SECTION_BLOCKED_IPS_FIREWALL"] = new Dictionary<string, string> { ["UA"] = "Поточні заблоковані IP (з правила брандмауера):", ["EN"] = "Currently blocked IPs (from firewall rule):" },
            ["SECTION_IP_TO_UNBLOCK"] = new Dictionary<string, string> { ["UA"] = "IP для розблокування:", ["EN"] = "IP to unlock:" },
            ["SECTION_ADD_WHITELIST_IP"] = new Dictionary<string, string> { ["UA"] = "Додати IP до білого списку:", ["EN"] = "Add IP to whitelist:" },
            ["SECTION_SERVICE_CONFIG"] = new Dictionary<string, string> { ["UA"] = "Конфігурація служби", ["EN"] = "Service Configuration" },
            ["SECTION_BLOCK_LEVELS_ATTEMPTS"] = new Dictionary<string, string> { ["UA"] = "Рівні блокування (Спроби → Хвилини):", ["EN"] = "Block Levels (Attempts → Minutes):" },
            
            ["LABEL_IP_ADDRESS"] = new Dictionary<string, string> { ["UA"] = "IP-адреса:", ["EN"] = "IP Address:" },
            ["LABEL_DURATION"] = new Dictionary<string, string> { ["UA"] = "Тривалість (хвилини):", ["EN"] = "Duration (minutes):" },
            ["LABEL_BLOCK_STATUS_PLACEHOLDER"] = new Dictionary<string, string> { ["UA"] = "Статус з'явиться тут...", ["EN"] = "Status will appear here..." },

            ["BTN_UNBLOCK_IP"] = new Dictionary<string, string> { ["UA"] = "Розблокувати IP", ["EN"] = "Unlock IP" },
            ["BTN_ADD_WITH_PLUS"] = new Dictionary<string, string> { ["UA"] = "+ Додати", ["EN"] = "+ Add" },
            ["BTN_REMOVE_WITH_X"] = new Dictionary<string, string> { ["UA"] = "✕ Видалити", ["EN"] = "✕ Remove" },
            ["BTN_BLOCK_THIS_IP"] = new Dictionary<string, string> { ["UA"] = "ЗАБЛОКУВАТИ ЦЕЙ IP", ["EN"] = "BLOCK THIS IP" },
            ["BTN_ADD_LEVEL_WITH_PLUS"] = new Dictionary<string, string> { ["UA"] = "+ Додати рівень", ["EN"] = "+ Add Level" },
            ["BTN_SAVE_CONFIGURATION"] = new Dictionary<string, string> { ["UA"] = "ЗБЕРЕГТИ КОНФІГУРАЦІЮ", ["EN"] = "SAVE CONFIGURATION" },
            
            ["GRID_FAILED_ATTEMPTS"] = new Dictionary<string, string> { ["UA"] = "Невдалих спроб", ["EN"] = "Failed Attempts" },
            ["GRID_BLOCK_DURATION"] = new Dictionary<string, string> { ["UA"] = "Тривалість блокування (хв)", ["EN"] = "Block Duration (min)" },
            ["GRID_ATTEMPTS"] = new Dictionary<string, string> { ["UA"] = "Спроби", ["EN"] = "Attempts" },
            ["GRID_BLOCK_MINUTES"] = new Dictionary<string, string> { ["UA"] = "Хвилини блокування", ["EN"] = "Block Minutes" },
        };

        public static string Get(string key)
        {
            string lang = Program.CurrentLanguage;
            if (translations.ContainsKey(key) && translations[key].ContainsKey(lang))
                return translations[key][lang];
            
            // Fallback to EN
            if (translations.ContainsKey(key) && translations[key].ContainsKey("EN"))
                return translations[key]["EN"];
                
            return key; // Fallback to key itself
        }
    }
}
