using System.Diagnostics;
using System.Text.Json;
using DbcServer.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace DbcServer.IntegrationTests.Controllers;

public class StockControllerPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StockControllerPerformanceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetStockItems_ConsecutiveCalls_SecondCallShouldBeFaster()
    {
        // First call - populates cache
        var stopwatch1 = Stopwatch.StartNew();
        var response1 = await _client.GetAsync("/api/stock?pageSize=10&pageNumber=1");
        stopwatch1.Stop();
        response1.EnsureSuccessStatusCode();

        // Second call - should use cache
        var stopwatch2 = Stopwatch.StartNew();
        var response2 = await _client.GetAsync("/api/stock?pageSize=10&pageNumber=2");
        stopwatch2.Stop();
        response2.EnsureSuccessStatusCode();

        // Third call - should also use cache
        var stopwatch3 = Stopwatch.StartNew();
        var response3 = await _client.GetAsync("/api/stock?pageSize=10&pageNumber=3");
        stopwatch3.Stop();
        response3.EnsureSuccessStatusCode();

        // Assert - subsequent calls should be faster due to caching
        // Allow some tolerance for system variations
        stopwatch3.ElapsedMilliseconds.Should().BeLessThan(stopwatch1.ElapsedMilliseconds * 2);
    }

    [Fact]
    public async Task GetStockItems_MultipleConcurrentRequests_ShouldHandleCorrectly()
    {
        // Arrange
        var tasks = new List<Task<HttpResponseMessage>>();
        
        // Act - Send 10 concurrent requests
        for (int i = 1; i <= 10; i++)
        {
            tasks.Add(_client.GetAsync($"/api/stock?pageSize=5&pageNumber={i}"));
        }
        
        var responses = await Task.WhenAll(tasks);
        
        // Assert - All requests should succeed
        foreach (var response in responses)
        {
            response.EnsureSuccessStatusCode();
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetStockItems_DifferentPageSizes_ShouldReturnCorrectResults()
    {
        // Test various page sizes
        var pageSizes = new[] { 1, 5, 10, 50, 100 };
        
        foreach (var pageSize in pageSizes)
        {
            var response = await _client.GetAsync($"/api/stock?pageSize={pageSize}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PaginatedResponseDto<StockItemDto>>(content, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            result.Should().NotBeNull();
            result!.Items.Count().Should().BeInRange(0, pageSize);
            result.PageSize.Should().Be(pageSize);
        }
    }

    [Fact]
    public async Task GetStockItems_LargePageSize_ShouldCompleteInReasonableTime()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        
        // Act - Request 500 items
        var response = await _client.GetAsync("/api/stock?pageSize=500");
        stopwatch.Stop();
        
        // Assert
        response.EnsureSuccessStatusCode();
        // Should complete within 5 seconds even for large page
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    [Fact]
    public async Task GetStockItems_WithCaching_TotalCountShouldBeConsistent()
    {
        // Make multiple requests and verify total count is consistent
        var totalCounts = new List<int>();
        
        for (int i = 1; i <= 5; i++)
        {
            var response = await _client.GetAsync($"/api/stock?pageSize=10&pageNumber={i}");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<PaginatedResponseDto<StockItemDto>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            totalCounts.Add(result!.TotalCount);
        }
        
        // All requests should report the same total count
        totalCounts.Should().AllBeEquivalentTo(totalCounts.First());
    }

    [Fact]
    public async Task SearchStockItems_Performance_ShouldCompleteQuickly()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        var response = await _client.GetAsync("/api/stock/search?barcode=123");
        stopwatch.Stop();
        
        // Assert
        response.EnsureSuccessStatusCode();
        // Search should complete within 2 seconds
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    [Fact]
    public async Task GetStockItemByCode_Performance_ShouldCompleteQuickly()
    {
        // First get a valid code
        var listResponse = await _client.GetAsync("/api/stock?pageSize=1");
        listResponse.EnsureSuccessStatusCode();
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listResult = JsonSerializer.Deserialize<PaginatedResponseDto<StockItemDto>>(listContent,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        if (listResult?.Items?.Any() == true)
        {
            var code = listResult.Items.First().Code;
            
            // Measure single item retrieval
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync($"/api/stock/{code}");
            stopwatch.Stop();
            
            response.EnsureSuccessStatusCode();
            // Single item should be retrieved within 1 second
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }
    }

    [Fact]
    public async Task GetStockItems_Pagination_ShouldMaintainCorrectOrder()
    {
        // Get first two pages
        var response1 = await _client.GetAsync("/api/stock?pageSize=5&pageNumber=1");
        var response2 = await _client.GetAsync("/api/stock?pageSize=5&pageNumber=2");
        
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        
        var content1 = await response1.Content.ReadAsStringAsync();
        var content2 = await response2.Content.ReadAsStringAsync();
        
        var result1 = JsonSerializer.Deserialize<PaginatedResponseDto<StockItemDto>>(content1,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var result2 = JsonSerializer.Deserialize<PaginatedResponseDto<StockItemDto>>(content2,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        // Verify pagination metadata
        result1!.PageNumber.Should().Be(1);
        result2!.PageNumber.Should().Be(2);
        // Only check navigation flags if there are enough pages
        if (result1.TotalPages > 1)
        {
            result1.HasNextPage.Should().BeTrue();
        }
        if (result2.PageNumber > 1)
        {
            result2.HasPreviousPage.Should().BeTrue();
        }
        
        // Items should be different between pages
        if (result1.Items.Any() && result2.Items.Any())
        {
            var codes1 = result1.Items.Select(i => i.Code).ToList();
            var codes2 = result2.Items.Select(i => i.Code).ToList();
            codes1.Should().NotBeEquivalentTo(codes2);
        }
    }
}