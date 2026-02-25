using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers;

[Route("[controller]")]
public class ImportController : Controller
{
    private readonly AppDbContext _context;

    public ImportController(AppDbContext context)
    {
        _context = context;
    }

    private bool IsAdmin()
    {
        var isAdmin = HttpContext.Session.GetString("IsAdmin");
        return isAdmin == "True";
    }

    [HttpGet]
    [Route("")]
    [Route("Excel")]
    public IActionResult Excel()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return Forbid();

        return View();
    }

    [HttpPost]
    [Route("Excel")]
    public async Task<IActionResult> Excel(IFormFile file, string analyze = "true")
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return Forbid();

        if (file == null || file.Length == 0)
        {
            ViewBag.Error = "Выберите файл для импорта";
            return View();
        }

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
        {
            ViewBag.Error = "Поддерживаются только файлы Excel (.xlsx, .xls)";
            return View();
        }

        try
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    if (package.Workbook.Worksheets.Count == 0)
                    {
                        ViewBag.Error = "Excel файл не содержит листов";
                        return View();
                    }

                    var worksheet = package.Workbook.Worksheets[0];
                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    var colCount = worksheet.Dimension?.Columns ?? 0;

                    if (rowCount < 2)
                    {
                        ViewBag.Error = "Excel файл должен содержать заголовок и минимум одну строку данных";
                        return View();
                    }

                    // Read header
                    List<string> headers = new List<string>();
                    for (int col = 1; col <= colCount; col++)
                    {
                        headers.Add(worksheet.Cells[1, col].Text?.Trim() ?? $"Колонка {col}");
                    }

                    // Read first 5 data rows for preview
                    List<List<string>> preview = new List<List<string>>();
                    for (int row = 2; row <= Math.Min(6, rowCount); row++)
                    {
                        var rowData = new List<string>();
                        for (int col = 1; col <= colCount; col++)
                        {
                            rowData.Add(worksheet.Cells[row, col].Text?.Trim() ?? "-");
                        }
                        preview.Add(rowData);
                    }

                    // Import data to database
                    List<string> importedRows = new List<string>();
                    int importedCount = 0;
                    List<string> errors = new List<string>();

                    for (int row = 2; row <= rowCount; row++)
                    {
                        try
                        {
                            var importData = new ImportData();
                            
                            // Check if VendorDescription (column 3) is empty - if so, we've reached the end of data
                            string vendorCheck = worksheet.Cells[row, 3].Text?.Trim() ?? "";
                            if (string.IsNullOrEmpty(vendorCheck))
                            {
                                break; // End of data reached
                            }
                            
                            // Map columns flexibly - try to find by header names
                            for (int col = 1; col <= colCount; col++)
                            {
                                string headerName = headers[col - 1]?.ToLower() ?? "";
                                string cellValue = worksheet.Cells[row, col].Text?.Trim() ?? "";

                                // Skip empty cells
                                if (string.IsNullOrEmpty(cellValue) || cellValue == "-")
                                    continue;

                                // Direct column index mapping (assuming standard order)
                                switch (col)
                                {
                                    case 1: 
                                        if (int.TryParse(cellValue, out int year))
                                            importData.Year = year;
                                        break;
                                    case 2: importData.EntryNo = cellValue; break;
                                    case 3: importData.VendorDescription = cellValue; break;
                                    case 4: importData.VendorNo = cellValue; break;
                                    case 5: importData.LocationDescription = cellValue; break;
                                    case 6: importData.ImporterName = cellValue; break;
                                    case 7: importData.Responsibility = cellValue; break;
                                    case 8: importData.Consignee = cellValue; break;
                                    case 9: importData.DateOfData = TryParseDate(cellValue); break;
                                    case 10: importData.DateOfCollection = TryParseDate(cellValue); break;
                                    case 11: importData.DateOfReceipt = TryParseDate(cellValue); break;
                                    case 12: importData.InvoiceNumber = cellValue; break;
                                    case 13: importData.InvoiceDate = TryParseDate(cellValue); break;
                                    case 14: 
                                        if (decimal.TryParse(cellValue.Replace(",", "."), out decimal amount))
                                            importData.InvoiceAmount = amount;
                                        break;
                                    case 15: importData.CDNo = cellValue; break;
                                    case 16: importData.GrossWeight = TryParseDecimal(cellValue); break;
                                    case 17: importData.NetWeight = TryParseDecimal(cellValue); break;
                                    case 18: importData.TypeOfOperation = cellValue; break;
                                    case 19: importData.DeliveryVia = cellValue; break;
                                    case 20: importData.TruckNo = cellValue; break;
                                    case 21: importData.Carrier = cellValue; break;
                                    case 22: importData.CarrierWithTir = cellValue; break;
                                    case 23: importData.TermsOfDelivery = cellValue; break;
                                    case 24: importData.ShipmentFrom = TryParseDate(cellValue); break;
                                    case 25: importData.Warehouse = cellValue; break;
                                    case 26: importData.NumberOfUnits = TryParseInt(cellValue); break;
                                    case 27: importData.ProductUnit = cellValue; break;
                                    case 28: importData.NumberOfLines = TryParseInt(cellValue); break;
                                    case 29: importData.GoodsAmount = TryParseDecimal(cellValue); break;
                                    case 30: importData.Volume = TryParseDecimal(cellValue); break;
                                    case 31: importData.Explanation = cellValue; break;
                                    case 32: importData.Notes = cellValue; break;
                                    case 33: importData.Machinery = cellValue; break;
                                    case 34: importData.InvoiceReadiness = cellValue; break;
                                    case 35: importData.IsNecessary = cellValue.ToLower() == "yes" || cellValue.ToLower() == "да"; break;
                                }
                            }

                            // Only add if has at least one key field
                            if (!string.IsNullOrEmpty(importData.VendorDescription) || 
                                !string.IsNullOrEmpty(importData.InvoiceNumber) || 
                                importData.Year > 0)
                            {
                                importData.CreatedAt = DateTime.Now;
                                _context.ImportData.Add(importData);
                                importedCount++;

                                if (importedCount % 50 == 0)
                                {
                                    await _context.SaveChangesAsync();
                                }
                            }
                        }
                        catch (Exception rowEx)
                        {
                            errors.Add($"Строка {row}: {rowEx.Message}");
                        }
                    }

                    // Final save
                    if (importedCount > 0)
                    {
                        await _context.SaveChangesAsync();
                    }

                    // Store analysis results in ViewBag
                    ViewBag.AnalysisInfo = new
                    {
                        FileName = file.FileName,
                        SheetName = worksheet.Name,
                        FileSize = file.Length,
                        Rows = rowCount,
                        Columns = colCount,
                        Headers = headers,
                        Preview = preview,
                        ImportedCount = importedCount
                    };

                    if (importedCount > 0)
                    {
                        ViewBag.Success = $"✅ <strong>Успешно импортировано: {importedCount} записей</strong>";
                    }

                    if (errors.Count > 0)
                    {
                        ViewBag.Errors = errors;
                    }

                    ViewBag.Info = $"📊 <strong>Анализ файла завершён</strong><br/>" +
                        $"📄 Файл: <strong>{file.FileName}</strong><br/>" +
                        $"📈 Размер: {file.Length / 1024} КБ<br/>" +
                        $"📋 Строк: <strong>{rowCount}</strong> (1 заголовок + {rowCount - 1} данных)<br/>" +
                        $"📌 Колонок: <strong>{colCount}</strong><br/>" +
                        $"✅ <strong>Импортировано: {importedCount} записей</strong><br/>" +
                        $"<br/><strong>Заголовки:</strong><br/>" +
                        $"<code>{string.Join(" | ", headers.Take(10))}</code>" +
                        (colCount > 10 ? $"<br/>... и ещё {colCount - 10} колонок" : "");

                    return View();
                }
            }
        }
        catch (Exception ex)
        {
            ViewBag.Error = $"❌ Ошибка анализа: {ex.Message}";
        }

        return View();
    }

    [HttpGet]
    [Route("List")]
    public async Task<IActionResult> List()
    {
        if (HttpContext.Session.GetString("UserId") == null)
            return RedirectToAction("Login", "Account");

        if (!IsAdmin())
            return Forbid();

        var importData = await _context.ImportData
            .Where(d => !string.IsNullOrWhiteSpace(d.VendorDescription) || 
                        !string.IsNullOrWhiteSpace(d.InvoiceNumber) || 
                        d.Year.HasValue)
            .OrderByDescending(d => d.Id)
            .ToListAsync();
        return View(importData);
    }

    [HttpPost]
    [Route("Delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin())
            return Forbid();

        var importData = await _context.ImportData.FindAsync(id);
        if (importData != null)
        {
            _context.ImportData.Remove(importData);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("List");
    }

    [HttpPost]
    [Route("Add")]
    public async Task<IActionResult> Add(int? year, string entryNo, string vendorDescription, string vendorNo, 
        string locationDescription, string importerName, string responsibility, string consignee, string dateOfData,
        string invoiceAmount, string invoiceNumber, string invoiceDate, string invoiceReadiness, string dateOfReceipt,
        string grossWeight, string netWeight, int? numberOfUnits, string volume, string shipmentFrom, string termsOfDelivery,
        string notes, string deliveryVia, string truckNo, string cdNo, string productUnit, string goodsAmount,
        string dateOfCollection, string warehouse, bool isNecessary, string explanation, string machinery,
        string carrier, string carrierWithTir, int? numberOfLines, string typeOfOperation)
    {
        if (!IsAdmin())
            return Forbid();

        if (string.IsNullOrWhiteSpace(vendorDescription))
        {
            return RedirectToAction("List");
        }

        var importData = new ImportData
        {
            Year = year,
            EntryNo = entryNo?.Trim(),
            VendorDescription = vendorDescription?.Trim(),
            VendorNo = vendorNo?.Trim(),
            LocationDescription = locationDescription?.Trim(),
            ImporterName = importerName?.Trim(),
            Responsibility = responsibility?.Trim(),
            Consignee = consignee?.Trim(),
            DateOfData = TryParseDate(dateOfData),
            InvoiceAmount = TryParseDecimal(invoiceAmount),
            InvoiceNumber = invoiceNumber?.Trim(),
            InvoiceDate = TryParseDate(invoiceDate),
            InvoiceReadiness = invoiceReadiness?.Trim(),
            DateOfReceipt = TryParseDate(dateOfReceipt),
            GrossWeight = TryParseDecimal(grossWeight),
            NetWeight = TryParseDecimal(netWeight),
            NumberOfUnits = numberOfUnits,
            Volume = TryParseDecimal(volume),
            ShipmentFrom = TryParseDate(shipmentFrom),
            TermsOfDelivery = termsOfDelivery?.Trim(),
            Notes = notes?.Trim(),
            DeliveryVia = deliveryVia?.Trim(),
            TruckNo = truckNo?.Trim(),
            CDNo = cdNo?.Trim(),
            ProductUnit = productUnit?.Trim(),
            GoodsAmount = TryParseDecimal(goodsAmount),
            DateOfCollection = TryParseDate(dateOfCollection),
            Warehouse = warehouse?.Trim(),
            IsNecessary = isNecessary,
            Explanation = explanation?.Trim(),
            Machinery = machinery?.Trim(),
            Carrier = carrier?.Trim(),
            CarrierWithTir = carrierWithTir?.Trim(),
            NumberOfLines = numberOfLines,
            TypeOfOperation = typeOfOperation?.Trim()
        };

        _context.ImportData.Add(importData);
        await _context.SaveChangesAsync();

        return RedirectToAction("List");
    }

    [HttpPost]
    [Route("Clear")]
    public async Task<IActionResult> Clear()
    {
        if (!IsAdmin())
            return Forbid();

        _context.ImportData.RemoveRange(_context.ImportData);
        await _context.SaveChangesAsync();

        return RedirectToAction("List");
    }

    private int? TryParseInt(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (int.TryParse(value.Trim(), out var result))
            return result;
        return null;
    }

    private decimal? TryParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (decimal.TryParse(value.Trim(), out var result))
            return result;
        return null;
    }

    private DateTime? TryParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        value = value.Trim();

        // Try standard formats
        var formats = new[]
        {
            "dd.MM.yyyy",
            "d.M.yyyy",
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd.MM.yy",
            "d.M.yy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var result))
                return result;
        }

        // Try Parse as fallback
        if (DateTime.TryParse(value, out var parseResult))
            return parseResult;

        return null;
    }

    private bool TryParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim().ToLower();
        return value == "yes" || value == "так" || value == "true" || value == "1" || value == "+";
    }
}
