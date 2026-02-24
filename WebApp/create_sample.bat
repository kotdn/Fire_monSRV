@echo off
REM Create sample Excel file with PowerShell
powershell -Command ^
"$excelPath = 'C:\Users\samoilenkod\source\repos\Winservice\WebApp\sample_import.xlsx'; ^
[Reflection.Assembly]::LoadFrom('C:\Users\samoilenkod\.nuget\packages\epplus\7.0.0\lib\net6.0\EPPlus.dll') | Out-Null; ^
$package = New-Object OfficeOpenXml.ExcelPackage; ^
$ws = $package.Workbook.Worksheets.Add('Data'); ^
$headers = @('Year', 'EntryNo', 'VendorDescription', 'VendorNo', 'LocationDescription', 'ImporterName', 'Responsibility', 'Consignee', 'DateOfData', 'InvoiceAmount', 'InvoiceNumber', 'InvoiceDate', 'InvoiceReadiness', 'DateOfReceipt', 'GrossWeight', 'NetWeight', 'NumberOfUnits', 'Volume', 'ShipmentFrom', 'TermsOfDelivery', 'Notes', 'DeliveryVia', 'TruckNo', 'CDNo', 'ProductUnit', 'GoodsAmount', 'DateOfCollection', 'Warehouse', 'IsNecessary', 'Explanation', 'Machinery', 'Carrier', 'CarrierWithTir', 'NumberOfLines', 'TypeOfOperation'); ^
for (`$i = 0; `$i -lt 35; `$i++) { `$ws.Cells[1, `$i+1].Value = `$headers[`$i] }; ^
`$ws.Cells[2,1].Value = 2026; ^
`$ws.Cells[2,2].Value = 'ENT-TEST-001'; ^
`$ws.Cells[2,3].Value = 'ТОВ Тестова Компанія'; ^
`$ws.Cells[2,4].Value = 'TEST-123'; ^
`$ws.Cells[2,5].Value = 'Київ'; ^
`$ws.Cells[2,6].Value = 'Іван Петренко'; ^
`$ws.Cells[2,7].Value = 'Закупки'; ^
`$ws.Cells[2,8].Value = 'Сховище'; ^
`$ws.Cells[2,9].Value = '05.02.2026'; ^
`$ws.Cells[2,10].Value = 50000.99; ^
`$ws.Cells[2,11].Value = 'INV-TEST-001'; ^
`$ws.Cells[2,12].Value = '01.02.2026'; ^
`$ws.Cells[2,13].Value = 'так'; ^
`$ws.Cells[2,14].Value = '05.02.2026'; ^
`$ws.Cells[2,15].Value = 3000.50; ^
`$ws.Cells[2,16].Value = 2800.75; ^
`$ws.Cells[2,17].Value = 50; ^
`$ws.Cells[2,18].Value = 100.25; ^
`$ws.Cells[2,35].Value = 'Завезення'; ^
`$package.SaveAs(`$excelPath); ^
Write-Host 'Файл создан:' `$excelPath"
