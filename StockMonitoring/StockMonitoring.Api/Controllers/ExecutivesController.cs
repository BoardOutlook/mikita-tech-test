using Microsoft.AspNetCore.Mvc;
using StockMonitoring.Api.Services;
using StockMonitoring.Core.Models;

namespace StockMonitoring.Api.Controllers;

[ApiController]
[Route("api/companies/executives")]
public class ExecutivesController(IStockService stockService, ILogger<ExecutivesController> logger)
    : ControllerBase
{
    [HttpGet("compensation")]
    [ProducesResponseType(typeof(IEnumerable<ExecutiveCompensation>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHighCompensationExecutives()
    {
        try
        {
            var executives = await stockService.GetHighCompensationExecutivesAsync();
            return Ok(executives);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting high compensation executives");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request");
        }
    }
}
