namespace WebApp.Models;

public class ImportData
{
    public int Id { get; set; }
    public int? Year { get; set; }
    public string? EntryNo { get; set; }
    public string? VendorDescription { get; set; }
    public string? VendorNo { get; set; }
    public string? LocationDescription { get; set; }
    public string? ImporterName { get; set; }
    public string? Responsibility { get; set; }
    public string? Consignee { get; set; }
    public DateTime? DateOfData { get; set; }
    public decimal? InvoiceAmount { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string? InvoiceReadiness { get; set; }
    public DateTime? DateOfReceipt { get; set; }
    public decimal? GrossWeight { get; set; }
    public decimal? NetWeight { get; set; }
    public int? NumberOfUnits { get; set; }
    public decimal? Volume { get; set; }
    public DateTime? ShipmentFrom { get; set; }
    public string? TermsOfDelivery { get; set; }
    public string? Notes { get; set; }
    public string? DeliveryVia { get; set; }
    public string? TruckNo { get; set; }
    public string? CDNo { get; set; }
    public string? ProductUnit { get; set; }
    public decimal? GoodsAmount { get; set; }
    public DateTime? DateOfCollection { get; set; }
    public string? Warehouse { get; set; }
    public bool? IsNecessary { get; set; }
    public string? Explanation { get; set; }
    public string? Machinery { get; set; }
    public string? Carrier { get; set; }
    public string? CarrierWithTir { get; set; }
    public int? NumberOfLines { get; set; }
    public string? TypeOfOperation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
