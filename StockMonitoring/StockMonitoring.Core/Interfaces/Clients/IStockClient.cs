using StockMonitoring.Core.Models;

namespace StockMonitoring.Core.Interfaces.Clients;

public interface IStockClient
{
    Task<IEnumerable<Company>> GetCompaniesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Executive>> GetExecutivesAsync(string companySymbol, CancellationToken cancellationToken = default);
    Task<IndustryBenchmark?> GetIndustryBenchmarkAsync(string industry, CancellationToken cancellationToken = default);
}
