using DbcServer.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DbcServer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStockItems(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? barcode = null)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var result = await _stockService.GetStockItemsAsync(pageNumber, pageSize, barcode);
        return Ok(result);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetStockItemByCode(int code)
    {
        var item = await _stockService.GetStockItemByCodeAsync(code);

        if (item == null)
            return NotFound(new { message = $"Stock item with code {code} not found" });

        return Ok(item);
    }

    [HttpGet("search")]
    public async Task<IActionResult> SearchByBarcode([FromQuery] string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return BadRequest(new { message = "Barcode parameter is required" });

        var items = await _stockService.SearchByBarcodeAsync(barcode);
        return Ok(items);
    }
}