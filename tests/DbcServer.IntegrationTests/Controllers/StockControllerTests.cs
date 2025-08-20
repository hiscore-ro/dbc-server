using System.Net;
using System.Net.Http.Json;
using DbcServer.Application.DTOs;
using DbcServer.IntegrationTests.TestFixtures;
using FluentAssertions;

namespace DbcServer.IntegrationTests.Controllers;

public class StockControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StockControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetStockItems_ShouldReturnOkWithPaginatedData()
    {
        // Act
        var response = await _client.GetAsync("/api/stock?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<StockItemDto>>();
        content.Should().NotBeNull();
        content!.PageNumber.Should().Be(1);
        content.PageSize.Should().Be(10);
        content.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStockItems_WithBarcodeFilter_ShouldReturnFilteredResults()
    {
        // Act
        var response = await _client.GetAsync("/api/stock?pageNumber=1&pageSize=10&barcode=123");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<StockItemDto>>();
        content.Should().NotBeNull();
        content!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetStockItems_WithInvalidPageNumber_ShouldDefaultToPage1()
    {
        // Act
        var response = await _client.GetAsync("/api/stock?pageNumber=0&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<StockItemDto>>();
        content.Should().NotBeNull();
        content!.PageNumber.Should().Be(1);
    }

    [Fact]
    public async Task GetStockItems_WithLargePageSize_ShouldLimitTo100()
    {
        // Act
        var response = await _client.GetAsync("/api/stock?pageNumber=1&pageSize=500");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<PaginatedResponseDto<StockItemDto>>();
        content.Should().NotBeNull();
        content!.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetStockItemByCode_WhenItemDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/stock/999999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchByBarcode_WithoutBarcodeParameter_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/stock/search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchByBarcode_WithEmptyBarcode_ShouldReturnBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/api/stock/search?barcode=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchByBarcode_WithValidBarcode_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/stock/search?barcode=ABC");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<IEnumerable<StockItemDto>>();
        content.Should().NotBeNull();
    }
}