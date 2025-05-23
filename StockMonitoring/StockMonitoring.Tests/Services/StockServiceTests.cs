using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StockMonitoring.Application.Services;
using StockMonitoring.Core.Exceptions;
using StockMonitoring.Core.Interfaces.Clients;
using StockMonitoring.Core.Models;

namespace StockMonitoring.Tests.Services;

public class StockServiceTests
{
    private readonly Mock<IStockClient> _mockStockClient;
    private readonly Mock<ILogger<StockService>> _mockLogger;
    private readonly StockService _service;
    
    public StockServiceTests()
    {
        _mockStockClient = new Mock<IStockClient>();
        _mockLogger = new Mock<ILogger<StockService>>();
        _service = new StockService(_mockStockClient.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task GetHighCompensationExecutivesAsync_ReturnsEmptyList_WhenNoCompaniesFound()
    {
        // Arrange
        _mockStockClient.Setup(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Company>());
        
        // Act
        var result = await _service.GetHighCompensationExecutivesAsync();
        
        // Assert
        result.Should().BeEmpty();
        _mockStockClient.Verify(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
    
    [Fact]
    public async Task GetHighCompensationExecutivesAsync_ReturnsFilteredExecutives_WhenCompensationIsHigherThanBenchmark()
    {
        // Arrange
        var companies = new List<Company>
        {
            new() { Symbol = "AAPL", Name = "Apple Inc." },
            new() { Symbol = "MSFT", Name = "Microsoft Corporation" }
        };
        
        var executives = new List<Executive>
        {
            new()
            {
                NameAndPosition = "John Doe, CEO",
                Symbol = "AAPL",
                IndustryTitle = "Technology",
                Total = 1000000
            },
            new()
            {
                NameAndPosition = "Jane Smith, CTO",
                Symbol = "AAPL",
                IndustryTitle = "Technology",
                Total = 750000
            }
        };
        
        var benchmark = new IndustryBenchmark
        {
            IndustryTitle = "Technology",
            AverageCompensation = 700000
        };
        
        _mockStockClient.Setup(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(companies);
        
        _mockStockClient.Setup(x => x.GetExecutivesAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(executives);
        
        _mockStockClient.Setup(x => x.GetExecutivesAsync("MSFT", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Executive>());
        
        _mockStockClient.Setup(x => x.GetIndustryBenchmarkAsync("Technology", It.IsAny<CancellationToken>()))
            .ReturnsAsync(benchmark);
        
        // Act
        var result = await _service.GetHighCompensationExecutivesAsync();
        
        // Assert
        result.Should().HaveCount(1);
        result.First().NameAndPosition.Should().Be("John Doe, CEO");
        result.First().Compensation.Should().Be(1000000);
        result.First().AverageIndustryCompensation.Should().Be(700000);
    }
    
    [Fact]
    public async Task GetHighCompensationExecutivesAsync_ShouldRetryOnException()
    {
        // Arrange
        var companies = new List<Company>
        {
            new() { Symbol = "AAPL", Name = "Apple Inc." }
        };
        
        _mockStockClient.SetupSequence(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StockClientException("API error", "exchanges/ASX/companies", new Exception()))
            .ReturnsAsync(companies);
        
        _mockStockClient.Setup(x => x.GetExecutivesAsync("AAPL", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Executive>());
        
        // Act
        await _service.GetHighCompensationExecutivesAsync();
        
        // Assert
        _mockStockClient.Verify(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
    
    [Fact]
    public async Task GetHighCompensationExecutivesAsync_HandlesClientExceptions()
    {
        // Arrange
        _mockStockClient.Setup(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StockClientException("API error", "exchanges/ASX/companies", new Exception()));
        
        _mockStockClient.Setup(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StockClientException("API error", "exchanges/ASX/companies", new Exception()));
        
        _mockStockClient.Setup(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StockClientException("API error", "exchanges/ASX/companies", new Exception()));
        
        _mockStockClient.Setup(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new StockClientException("API error", "exchanges/ASX/companies", new Exception()));
        
        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => _service.GetHighCompensationExecutivesAsync());
        _mockStockClient.Verify(x => x.GetCompaniesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}
