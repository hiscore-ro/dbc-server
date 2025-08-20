using System.Text.Json.Serialization;

namespace DbcServer.Application.DTOs;

public class StockItemDto
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("barcode")]
    public string? Barcode { get; set; }

    [JsonPropertyName("warehouse")]
    public int Warehouse { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("priceB")]
    public decimal PriceB { get; set; }

    [JsonPropertyName("priceC")]
    public decimal PriceC { get; set; }

    [JsonPropertyName("lot")]
    public string? Lot { get; set; }

    [JsonPropertyName("warranty")]
    public DateTime? Warranty { get; set; }
}