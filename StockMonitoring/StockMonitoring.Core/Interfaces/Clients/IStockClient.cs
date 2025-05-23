using StockMonitoring.Core.Models;

namespace StockMonitoring.Core.Interfaces.Clients;

public interface IStockClient
{
    Task<IEnumerable<Company>> GetCompaniesAsync();
    Task<IEnumerable<Executive>> GetExecutivesAsync(string companySymbol);
    Task<IndustryBenchmark?> GetIndustryBenchmarkAsync(string industry);
}
