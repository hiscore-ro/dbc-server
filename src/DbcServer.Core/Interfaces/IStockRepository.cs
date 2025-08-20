using DbcServer.Core.Models;

namespace DbcServer.Core.Interfaces;

public interface IStockRepository
{
    Task<PaginatedResult<StockItem>> GetStockItemsAsync(int pageNumber, int pageSize, string? barcode = null);
    Task<StockItem?> GetStockItemByCodeAsync(int code);
    Task<IEnumerable<StockItem>> SearchByBarcodeAsync(string barcode);
    Task<int> GetTotalCountAsync(string? barcode = null);
}