RDP Security Suite (win-x64)

Файлы рядом:
- WinService.exe            Windows-служба (RDPSecurityService)
- RDPSecurityViewer.exe     Монитор/просмотр логов (просто запускается как EXE)

Конфиг службы:
- C:\ProgramData\RDPSecurityService\config.json
- Если файла нет, служба создаст его автоматически.
- Пример: config.example.json

Скрипты службы (только от Администратора):
- install_service.bat
- start_service.bat
- stop_service.bat
- uninstall_service.bat
- restart_all.bat (рестарт службы + прибить viewer + запустить viewer)

Логи:
- C:\ProgramData\RDPSecurityService\service.log
- C:\ProgramData\RDPSecurityService\access.log
- C:\ProgramData\RDPSecurityService\block_list.log
- C:\ProgramData\RDPSecurityService\whiteList.log
- C:\ProgramData\RDPSecurityService\current_log.log
