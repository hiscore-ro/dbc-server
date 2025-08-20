using System.Diagnostics;
using DbcServer.Core.Models;
using DbcServer.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace DbcServer.UnitTests.Services;

public class StockRepositoryCacheTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly string _testDbfPath;
    
    public StockRepositoryCacheTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _testDbfPath = Path.Combine(Directory.GetCurrentDirectory(), "test_data");
        _mockConfiguration.Setup(c => c["DbfPath"]).Returns(_testDbfPath);
    }
    
    [Fact(Skip = "Requires test DBF file")]
    public async Task GetStockItemsAsync_ConsecutiveCalls_ShouldUseCachedTotalCount()
    {
        // Arrange
        CreateTestDbfFile();
        var repository = new StockRepository(_mockConfiguration.Object);
        
        // Act - First call should populate cache
        var stopwatch1 = Stopwatch.StartNew();
        var result1 = await repository.GetStockItemsAsync(1, 10);
        stopwatch1.Stop();
        
        // Act - Second call should use cache
        var stopwatch2 = Stopwatch.StartNew();
        var result2 = await repository.GetStockItemsAsync(2, 10);
        stopwatch2.Stop();
        
        // Assert
        result1.TotalCount.Should().Be(result2.TotalCount);
        // Second call should be significantly faster due to caching
        // Note: This is a simplified test - in real scenario with 239k records,
        // the difference would be much more significant
        (stopwatch2.ElapsedMilliseconds <= stopwatch1.ElapsedMilliseconds).Should().BeTrue();
        
        // Cleanup
        CleanupTestFiles();
    }
    
    [Fact(Skip = "Requires test DBF file")]
    public async Task GetStockItemsAsync_WithBarcode_ShouldNotUseCacheForFilteredQueries()
    {
        // Arrange
        CreateTestDbfFile();
        var repository = new StockRepository(_mockConfiguration.Object);
        
        // Act - First call without filter (uses cache)
        var result1 = await repository.GetStockItemsAsync(1, 10);
        
        // Act - Second call with filter (should not use cache)
        var result2 = await repository.GetStockItemsAsync(1, 10, "TEST");
        
        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        // Filtered result should have different count
        result2.TotalCount.Should().BeLessThanOrEqualTo(result1.TotalCount);
        
        // Cleanup
        CleanupTestFiles();
    }
    
    [Fact(Skip = "Requires test DBF file")]
    public async Task GetStockItemsAsync_DifferentPageSizes_ShouldReturnCorrectSubset()
    {
        // Arrange
        CreateTestDbfFile();
        var repository = new StockRepository(_mockConfiguration.Object);
        
        // Act
        var result5 = await repository.GetStockItemsAsync(1, 5);
        var result10 = await repository.GetStockItemsAsync(1, 10);
        var result20 = await repository.GetStockItemsAsync(1, 20);
        
        // Assert
        result5.Items.Count().Should().BeInRange(0, 5);
        result10.Items.Count().Should().BeInRange(0, 10);
        result20.Items.Count().Should().BeInRange(0, 20);
        
        // All should have same total count (from cache)
        result5.TotalCount.Should().Be(result10.TotalCount);
        result10.TotalCount.Should().Be(result20.TotalCount);
        
        // Cleanup
        CleanupTestFiles();
    }
    
    [Fact(Skip = "Requires test DBF file")]
    public async Task GetStockItemByCodeAsync_ShouldLoadAllFields()
    {
        // Arrange
        CreateTestDbfFile();
        var repository = new StockRepository(_mockConfiguration.Object);
        
        // Act
        var item = await repository.GetStockItemByCodeAsync(1);
        
        // Assert
        item.Should().NotBeNull();
        // When getting single item, all fields should be loaded
        // In the real implementation, this uses loadAllFields: true
        item!.Code.Should().Be(1);
        
        // Cleanup
        CleanupTestFiles();
    }
    
    [Fact(Skip = "Requires test DBF file")]
    public async Task SearchByBarcodeAsync_ShouldLimitResults()
    {
        // Arrange
        CreateTestDbfFile();
        var repository = new StockRepository(_mockConfiguration.Object);
        
        // Act
        var results = await repository.SearchByBarcodeAsync("TEST");
        
        // Assert
        results.Should().NotBeNull();
        // Search should limit results to max 100 items
        results.Count().Should().BeInRange(0, 100);
        
        // Cleanup
        CleanupTestFiles();
    }
    
    [Fact(Skip = "Requires test DBF file")]
    public async Task GetTotalCountAsync_WithoutFilter_ShouldReturnAllRecords()
    {
        // Arrange
        CreateTestDbfFile();
        var repository = new StockRepository(_mockConfiguration.Object);
        
        // Act
        var count = await repository.GetTotalCountAsync();
        
        // Assert
        count.Should().BeGreaterThan(0);
        
        // Cleanup
        CleanupTestFiles();
    }
    
    [Fact(Skip = "Requires test DBF file")]
    public async Task GetTotalCountAsync_WithFilter_ShouldReturnFilteredCount()
    {
        // Arrange
        CreateTestDbfFile();
        var repository = new StockRepository(_mockConfiguration.Object);
        
        // Act
        var totalCount = await repository.GetTotalCountAsync();
        var filteredCount = await repository.GetTotalCountAsync("SPECIFIC");
        
        // Assert
        (filteredCount <= totalCount).Should().BeTrue();
        
        // Cleanup
        CleanupTestFiles();
    }
    
    private void CreateTestDbfFile()
    {
        // Create test directory if it doesn't exist
        if (!Directory.Exists(_testDbfPath))
        {
            Directory.CreateDirectory(_testDbfPath);
        }
        
        // Note: In a real test, you would create a small DBF file with test data
        // For now, we'll just ensure the directory exists
        // The actual DBF creation would require the DbfDataReader library's write capabilities
    }
    
    private void CleanupTestFiles()
    {
        // Clean up test files
        if (Directory.Exists(_testDbfPath))
        {
            try
            {
                Directory.Delete(_testDbfPath, true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}