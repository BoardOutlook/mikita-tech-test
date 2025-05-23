using StockMonitoring.Api.Models;

namespace StockMonitoring.Api.Services;

public interface IStockService
{
    Task<IEnumerable<ExecutiveCompensation>> GetHighCompensationExecutivesAsync();
}
