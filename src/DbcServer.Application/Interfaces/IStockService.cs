using DbcServer.Application.DTOs;

namespace DbcServer.Application.Interfaces;

public interface IStockService
{
    Task<PaginatedResponseDto<StockItemDto>> GetStockItemsAsync(int pageNumber = 1, int pageSize = 10, string? barcode = null);
    Task<StockItemDto?> GetStockItemByCodeAsync(int code);
    Task<IEnumerable<StockItemDto>> SearchByBarcodeAsync(string barcode);
}