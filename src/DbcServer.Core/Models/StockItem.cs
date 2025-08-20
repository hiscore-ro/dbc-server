namespace DbcServer.Core.Models;

public class StockItem
{
    public bool IsDouble { get; set; }
    public DateTime? Date { get; set; }
    public string? Category { get; set; }
    public string? Name { get; set; }
    public decimal Quantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal Samples { get; set; }
    public string? Unit { get; set; }
    public decimal Price { get; set; }
    public decimal PurchasePrice { get; set; }
    public DateTime? Warranty { get; set; }
    public int Code { get; set; }
    public string? IsWholesale { get; set; }
    public string? Notes { get; set; }
    public int Warehouse { get; set; }
    public string? Pieces { get; set; }
    public decimal VatPurchase { get; set; }
    public decimal VatSale { get; set; }
    public int CorrespondingCode { get; set; }
    public int Supplier { get; set; }
    public string? Barcode { get; set; }
    public string? Address { get; set; }
    public decimal CustomsDuty { get; set; }
    public decimal PriceB { get; set; }
    public decimal PriceC { get; set; }
    public string? ShortCode { get; set; }
    public string? BoxCode { get; set; }
    public decimal ReceptionPrice { get; set; }
    public decimal Margin { get; set; }
    public decimal PurchaseMargin { get; set; }
    public decimal Kilograms { get; set; }
    public string? Dimensions { get; set; }
    public string? OrderKey { get; set; }
    public string? Lot { get; set; }
    public string? TaxInvoiceCode { get; set; }
    public string? CustomsCode { get; set; }
    public string? CpvCode { get; set; }
    public decimal NetWeight { get; set; }
}