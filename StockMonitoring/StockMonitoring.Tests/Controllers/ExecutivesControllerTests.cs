using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using StockMonitoring.Api.Controllers;
using StockMonitoring.Core.Interfaces.Services;
using StockMonitoring.Core.Models;

namespace StockMonitoring.Tests.Controllers;

public class ExecutivesControllerTests
{
    private readonly Mock<IStockService> _mockStockService;
    private readonly Mock<ILogger<ExecutivesController>> _mockLogger;
    private readonly ExecutivesController _controller;
    
    public ExecutivesControllerTests()
    {
        _mockStockService = new Mock<IStockService>();
        _mockLogger = new Mock<ILogger<ExecutivesController>>();
        _controller = new ExecutivesController(_mockStockService.Object, _mockLogger.Object);
    }
    
    [Fact]
    public async Task GetHighCompensationExecutives_ReturnsOkResult_WithExecutives()
    {
        // Arrange
        var executiveCompensations = new List<ExecutiveCompensation>
        {
            new()
            {
                NameAndPosition = "John Doe, CEO",
                Compensation = 1000000,
                AverageIndustryCompensation = 800000
            }
        };
        
        _mockStockService.Setup(x => x.GetHighCompensationExecutivesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(executiveCompensations);
        
        // Act
        var result = await _controller.GetHighCompensationExecutives();
        
        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        
        var returnedCompensations = okResult.Value.Should().BeAssignableTo<IEnumerable<ExecutiveCompensation>>().Subject;
        returnedCompensations.Should().BeEquivalentTo(executiveCompensations);
    }
    
    [Fact]
    public async Task GetHighCompensationExecutives_ReturnsInternalServerError_WhenExceptionThrown()
    {
        // Arrange
        _mockStockService.Setup(x => x.GetHighCompensationExecutivesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Test exception"));
        
        // Act
        var result = await _controller.GetHighCompensationExecutives();
        
        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        statusCodeResult.Value.Should().Be("An error occurred while processing your request");
    }
}
