INSERT INTO ImportData 
(Year, EntryNo, VendorDescription, VendorNo, LocationDescription, ImporterName, Responsibility, Consignee, DateOfData, InvoiceAmount, InvoiceNumber, InvoiceDate, DateOfReceipt, GrossWeight, NetWeight, NumberOfUnits, Volume, IsNecessary, CreatedAt, TypeOfOperation) 
VALUES 
(2026, N'ENT-001', N'ТОВ Логістика', N'VEN-123', N'Київ', N'Василь Петренко', N'Відділ закупок', N'ТОВ Торгова компанія', GETDATE(), 45000.50, N'INV-2026-001', '2026-02-20', '2026-02-24', 2500.75, 2100.50, 100, 50.25, 1, GETDATE(), N'Завезення'),
(2026, N'ENT-002', N'OAO Поставщик', N'VEN-224', N'Харків', N'Ольга Сидоренко', N'Логістика', N'Склад №3', GETDATE(), 32150.00, N'INV-2026-002', '2026-02-21', '2026-02-25', 1800.00, 1650.30, 75, 40.50, 1, GETDATE(), N'Вивезення'),
(2026, N'ENT-003', N'Зовнішня торгівля Ltd', N'VEN-335', N'Одеса', N'Павло Шевченко', N'Управління', N'Порт Одеса', GETDATE(), 125000.99, N'INV-2026-003', '2026-02-18', '2026-02-26', 5500.50, 4800.25, 250, 120.75, 1, GETDATE(), N'Митна очистка');
