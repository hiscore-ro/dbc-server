using DbcServer.Application.DTOs;
using DbcServer.Application.Interfaces;
using DbcServer.Core.Interfaces;
using DbcServer.Core.Models;

namespace DbcServer.Application.Services;

public class StockService : IStockService
{
    private readonly IStockRepository _repository;

    public StockService(IStockRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginatedResponseDto<StockItemDto>> GetStockItemsAsync(int pageNumber = 1, int pageSize = 10, string? barcode = null)
    {
        var result = await _repository.GetStockItemsAsync(pageNumber, pageSize, barcode);
        
        return new PaginatedResponseDto<StockItemDto>
        {
            Items = result.Items.Select(MapToDto),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasPreviousPage = result.HasPreviousPage,
            HasNextPage = result.HasNextPage
        };
    }

    public async Task<StockItemDto?> GetStockItemByCodeAsync(int code)
    {
        var item = await _repository.GetStockItemByCodeAsync(code);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<IEnumerable<StockItemDto>> SearchByBarcodeAsync(string barcode)
    {
        var items = await _repository.SearchByBarcodeAsync(barcode);
        return items.Select(MapToDto);
    }

    private StockItemDto MapToDto(StockItem item)
    {
        return new StockItemDto
        {
            Code = item.Code,
            Name = item.Name,
            Category = item.Category,
            Quantity = item.Quantity,
            Unit = item.Unit,
            Price = item.Price,
            Barcode = item.Barcode,
            Warehouse = item.Warehouse,
            Notes = item.Notes,
            PriceB = item.PriceB,
            PriceC = item.PriceC,
            Lot = item.Lot,
            Warranty = item.Warranty
        };
    }
}