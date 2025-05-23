using StockMonitoring.Core.Models;

namespace StockMonitoring.Core.Interfaces.Services;

public interface IStockService
{
    Task<IEnumerable<ExecutiveCompensation>> GetHighCompensationExecutivesAsync(CancellationToken cancellationToken = default);
}
