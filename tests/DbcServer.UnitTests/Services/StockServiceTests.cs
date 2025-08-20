using DbcServer.Application.Services;
using DbcServer.Core.Interfaces;
using DbcServer.Core.Models;
using FluentAssertions;
using Moq;

namespace DbcServer.UnitTests.Services;

public class StockServiceTests
{
    private readonly Mock<IStockRepository> _mockRepository;
    private readonly StockService _service;

    public StockServiceTests()
    {
        _mockRepository = new Mock<IStockRepository>();
        _service = new StockService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetStockItemsAsync_ShouldReturnPaginatedResults()
    {
        // Arrange
        var mockItems = new List<StockItem>
        {
            new StockItem
            {
                Code = 1,
                Name = "Item 1",
                Barcode = "123456",
                Price = 10.50m,
                Quantity = 100,
                Category = "Electronics",
                Unit = "pcs"
            },
            new StockItem
            {
                Code = 2,
                Name = "Item 2",
                Barcode = "789012",
                Price = 25.00m,
                Quantity = 50,
                Category = "Books",
                Unit = "pcs"
            }
        };

        var mockResult = new PaginatedResult<StockItem>
        {
            Items = mockItems,
            TotalCount = 2,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetStockItemsAsync(1, 10, null))
            .ReturnsAsync(mockResult);

        // Act
        var result = await _service.GetStockItemsAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
        result.Items.Should().HaveCount(2);
        result.Items.First().Code.Should().Be(1);
        result.Items.First().Barcode.Should().Be("123456");
        result.Items.First().Category.Should().Be("Electronics");
    }

    [Fact]
    public async Task GetStockItemsAsync_WithPagination_ShouldCalculateCorrectPages()
    {
        // Arrange
        var mockItems = Enumerable.Range(1, 5).Select(i => new StockItem
        {
            Code = i,
            Name = $"Item {i}",
            Barcode = $"BC{i:000}",
            Price = i * 10m
        }).ToList();

        var mockResult = new PaginatedResult<StockItem>
        {
            Items = mockItems.Take(2),
            TotalCount = 25,
            PageNumber = 3,
            PageSize = 2
        };

        _mockRepository.Setup(r => r.GetStockItemsAsync(3, 2, null))
            .ReturnsAsync(mockResult);

        // Act
        var result = await _service.GetStockItemsAsync(3, 2);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(25);
        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(13); // 25 / 2 = 12.5, rounded up to 13
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task GetStockItemsAsync_WithBarcodeFilter_ShouldReturnFilteredResults()
    {
        // Arrange
        var barcode = "123";
        var mockItems = new List<StockItem>
        {
            new StockItem
            {
                Code = 1,
                Name = "Filtered Item",
                Barcode = "123456"
            }
        };

        var mockResult = new PaginatedResult<StockItem>
        {
            Items = mockItems,
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetStockItemsAsync(1, 10, barcode))
            .ReturnsAsync(mockResult);

        // Act
        var result = await _service.GetStockItemsAsync(1, 10, barcode);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().Barcode.Should().Contain(barcode);
        _mockRepository.Verify(r => r.GetStockItemsAsync(1, 10, barcode), Times.Once);
    }

    [Fact]
    public async Task GetStockItemsAsync_WithEmptyResult_ShouldReturnEmptyPaginatedResponse()
    {
        // Arrange
        var mockResult = new PaginatedResult<StockItem>
        {
            Items = new List<StockItem>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetStockItemsAsync(1, 10, null))
            .ReturnsAsync(mockResult);

        // Act
        var result = await _service.GetStockItemsAsync(1, 10);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(0);
        result.Items.Should().BeEmpty();
        result.TotalPages.Should().Be(0);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task GetStockItemByCodeAsync_WhenItemExists_ShouldReturnItem()
    {
        // Arrange
        var code = 123;
        var mockItem = new StockItem
        {
            Code = code,
            Name = "Test Item",
            Barcode = "987654",
            Price = 15.75m,
            PriceB = 18.00m,
            PriceC = 20.00m,
            Warehouse = 1,
            Lot = "LOT001",
            Warranty = new DateTime(2025, 12, 31)
        };

        _mockRepository.Setup(r => r.GetStockItemByCodeAsync(code))
            .ReturnsAsync(mockItem);

        // Act
        var result = await _service.GetStockItemByCodeAsync(code);

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be(code);
        result.Name.Should().Be("Test Item");
        result.Barcode.Should().Be("987654");
        result.Price.Should().Be(15.75m);
        result.PriceB.Should().Be(18.00m);
        result.PriceC.Should().Be(20.00m);
        result.Warehouse.Should().Be(1);
        result.Lot.Should().Be("LOT001");
        result.Warranty.Should().Be(new DateTime(2025, 12, 31));
    }

    [Fact]
    public async Task GetStockItemByCodeAsync_WhenItemDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var code = 999;
        _mockRepository.Setup(r => r.GetStockItemByCodeAsync(code))
            .ReturnsAsync((StockItem?)null);

        // Act
        var result = await _service.GetStockItemByCodeAsync(code);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetStockItemByCodeAsync(code), Times.Once);
    }

    [Fact]
    public async Task SearchByBarcodeAsync_ShouldReturnMatchingItems()
    {
        // Arrange
        var barcode = "ABC";
        var mockItems = new List<StockItem>
        {
            new StockItem
            {
                Code = 1,
                Name = "Item ABC",
                Barcode = "ABC123",
                Category = "Category A",
                Notes = "Test notes 1"
            },
            new StockItem
            {
                Code = 2,
                Name = "Item 2 ABC",
                Barcode = "456ABC",
                Category = "Category B",
                Notes = "Test notes 2"
            }
        };

        _mockRepository.Setup(r => r.SearchByBarcodeAsync(barcode))
            .ReturnsAsync(mockItems);

        // Act
        var result = await _service.SearchByBarcodeAsync(barcode);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(i => i.Barcode!.Contains(barcode)).Should().BeTrue();
        result.First().Category.Should().Be("Category A");
        result.Last().Category.Should().Be("Category B");
    }

    [Fact]
    public async Task SearchByBarcodeAsync_WhenNoMatches_ShouldReturnEmptyList()
    {
        // Arrange
        var barcode = "XYZ";
        _mockRepository.Setup(r => r.SearchByBarcodeAsync(barcode))
            .ReturnsAsync(new List<StockItem>());

        // Act
        var result = await _service.SearchByBarcodeAsync(barcode);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.SearchByBarcodeAsync(barcode), Times.Once);
    }

    [Fact]
    public async Task SearchByBarcodeAsync_WithPartialMatch_ShouldReturnResults()
    {
        // Arrange
        var barcode = "12";
        var mockItems = new List<StockItem>
        {
            new StockItem { Code = 1, Name = "Item 1", Barcode = "123456" },
            new StockItem { Code = 2, Name = "Item 2", Barcode = "121212" },
            new StockItem { Code = 3, Name = "Item 3", Barcode = "991200" }
        };

        _mockRepository.Setup(r => r.SearchByBarcodeAsync(barcode))
            .ReturnsAsync(mockItems);

        // Act
        var result = await _service.SearchByBarcodeAsync(barcode);

        // Assert
        result.Should().HaveCount(3);
        result.All(i => i.Barcode!.Contains(barcode)).Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(5, 20)]
    [InlineData(10, 50)]
    public async Task GetStockItemsAsync_WithDifferentPageSizes_ShouldReturnCorrectPageSize(int pageNumber, int pageSize)
    {
        // Arrange
        var mockResult = new PaginatedResult<StockItem>
        {
            Items = new List<StockItem>(),
            TotalCount = 100,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        _mockRepository.Setup(r => r.GetStockItemsAsync(pageNumber, pageSize, null))
            .ReturnsAsync(mockResult);

        // Act
        var result = await _service.GetStockItemsAsync(pageNumber, pageSize);

        // Assert
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalPages.Should().Be((int)Math.Ceiling(100.0 / pageSize));
    }

    [Fact]
    public async Task MapToDto_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var stockItem = new StockItem
        {
            Code = 100,
            Name = "Complete Item",
            Category = "Test Category",
            Quantity = 50.5m,
            Unit = "kg",
            Price = 99.99m,
            Barcode = "FULL123",
            Warehouse = 2,
            Notes = "Complete test item",
            PriceB = 110.00m,
            PriceC = 120.00m,
            Lot = "LOT2024",
            Warranty = new DateTime(2025, 6, 30)
        };

        var mockResult = new PaginatedResult<StockItem>
        {
            Items = new[] { stockItem },
            TotalCount = 1,
            PageNumber = 1,
            PageSize = 10
        };

        _mockRepository.Setup(r => r.GetStockItemsAsync(1, 10, null))
            .ReturnsAsync(mockResult);

        // Act
        var result = await _service.GetStockItemsAsync(1, 10);
        var dto = result.Items.First();

        // Assert
        dto.Code.Should().Be(100);
        dto.Name.Should().Be("Complete Item");
        dto.Category.Should().Be("Test Category");
        dto.Quantity.Should().Be(50.5m);
        dto.Unit.Should().Be("kg");
        dto.Price.Should().Be(99.99m);
        dto.Barcode.Should().Be("FULL123");
        dto.Warehouse.Should().Be(2);
        dto.Notes.Should().Be("Complete test item");
        dto.PriceB.Should().Be(110.00m);
        dto.PriceC.Should().Be(120.00m);
        dto.Lot.Should().Be("LOT2024");
        dto.Warranty.Should().Be(new DateTime(2025, 6, 30));
    }
}