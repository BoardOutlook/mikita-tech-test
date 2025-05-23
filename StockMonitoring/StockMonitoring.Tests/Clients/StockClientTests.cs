using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using StockMonitoring.Core.Exceptions;
using StockMonitoring.Core.Models;
using StockMonitoring.Infrastructure.Clients;
using StockMonitoring.Infrastructure.Configuration;

namespace StockMonitoring.Tests.Clients;

public class StockClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<ILogger<StockClient>> _mockLogger;
    private readonly Mock<IOptions<ApiOptions>> _mockOptions;
    private readonly HttpClient _httpClient;
    private readonly StockClient _client;
    
    public StockClientTests()
    {
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockLogger = new Mock<ILogger<StockClient>>();
        _mockOptions = new Mock<IOptions<ApiOptions>>();
        
        _mockOptions.Setup(x => x.Value).Returns(new ApiOptions { AuthCode = "test-auth-code" });
        
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://api.example.com/")
        };
        
        _client = new StockClient(_httpClient, _mockLogger.Object, _mockOptions.Object);
    }
    
    [Fact]
    public async Task GetCompaniesAsync_ReturnsCompanies_WhenApiResponseIsSuccessful()
    {
        // Arrange
        var companies = new List<Company>
        {
            new() { Symbol = "AAPL", Name = "Apple Inc." },
            new() { Symbol = "MSFT", Name = "Microsoft Corporation" }
        };
        
        SetupMockResponseWithContent(
            HttpStatusCode.OK, 
            companies, 
            "exchanges/ASX/companies?code=test-auth-code");
        
        // Act
        var result = await _client.GetCompaniesAsync();
        
        // Assert
        result.Should().BeEquivalentTo(companies);
        VerifyHttpRequest("exchanges/ASX/companies?code=test-auth-code", HttpMethod.Get);
    }
    
    [Fact]
    public async Task GetCompaniesAsync_ThrowsStockClientException_WhenApiResponseFails()
    {
        // Arrange
        SetupMockResponseWithStatusCode(
            HttpStatusCode.InternalServerError, 
            "exchanges/ASX/companies?code=test-auth-code");
        
        // Act & Assert
        await Assert.ThrowsAsync<StockClientException>(() => _client.GetCompaniesAsync());
        VerifyHttpRequest("exchanges/ASX/companies?code=test-auth-code", HttpMethod.Get);
    }
    
    [Fact]
    public async Task GetExecutivesAsync_ReturnsExecutives_WhenApiResponseIsSuccessful()
    {
        // Arrange
        var executives = new List<Executive>
        {
            new() { NameAndPosition = "John Doe, CEO", Symbol = "AAPL" },
            new() { NameAndPosition = "Jane Smith, CTO", Symbol = "AAPL" }
        };
        
        SetupMockResponseWithContent(
            HttpStatusCode.OK, 
            executives, 
            "companies/AAPL/executives?code=test-auth-code");
        
        // Act
        var result = await _client.GetExecutivesAsync("AAPL");
        
        // Assert
        result.Should().BeEquivalentTo(executives);
        VerifyHttpRequest("companies/AAPL/executives?code=test-auth-code", HttpMethod.Get);
    }
    
    [Fact]
    public async Task GetIndustryBenchmarkAsync_ReturnsBenchmark_WhenApiResponseIsSuccessful()
    {
        // Arrange
        var benchmark = new IndustryBenchmark
        {
            IndustryTitle = "Technology",
            AverageCompensation = 700000
        };
        
        SetupMockResponseWithContent(
            HttpStatusCode.OK, 
            benchmark, 
            "industries/Technology/benchmark?code=test-auth-code");
        
        // Act
        var result = await _client.GetIndustryBenchmarkAsync("Technology");
        
        // Assert
        result.Should().BeEquivalentTo(benchmark);
        VerifyHttpRequest("industries/Technology/benchmark?code=test-auth-code", HttpMethod.Get);
    }
    
    private void SetupMockResponseWithContent<T>(HttpStatusCode statusCode, T content, string requestUrl)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith(requestUrl)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = JsonContent.Create(content)
            });
    }
    
    private void SetupMockResponseWithStatusCode(HttpStatusCode statusCode, string requestUrl)
    {
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith(requestUrl)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode
            });
    }
    
    private void VerifyHttpRequest(string requestUrl, HttpMethod method)
    {
        _mockHttpMessageHandler
            .Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == method && 
                    req.RequestUri != null &&
                    req.RequestUri.ToString().EndsWith(requestUrl)),
                ItExpr.IsAny<CancellationToken>());
    }
}
