using Microsoft.Extensions.Logging;
using StockMonitoring.Core.Interfaces.Clients;
using StockMonitoring.Core.Interfaces.Services;
using StockMonitoring.Core.Models;

namespace StockMonitoring.Application.Services;

public class StockService(
    IStockClient stockClient,
    ILogger<StockService> logger)
    : IStockService
{
    public async Task<IEnumerable<ExecutiveCompensation>> GetHighCompensationExecutivesAsync()
    {
        try
        {
            var companies = (await stockClient.GetCompaniesAsync()).ToList();
            
            if (companies.Count == 0)
            {
                logger.LogWarning("No companies found for ASX exchange");
                return [];
            }
            
            var allExecutives = new List<Executive>();
            var industryBenchmarks = new Dictionary<string, decimal>();
            
            foreach (var company in companies)
            {
                if (string.IsNullOrEmpty(company.Symbol))
                    continue;
                
                var executives = (await stockClient.GetExecutivesAsync(company.Symbol)).ToList();
                if (executives.Count != 0)
                {
                    allExecutives.AddRange(executives);
                }
            }
            
            foreach (var industry in allExecutives.Select(e => e.Industry).Distinct().Where(i => !string.IsNullOrEmpty(i)))
            {
                var benchmark = await stockClient.GetIndustryBenchmarkAsync(industry!);
                if (benchmark != null)
                {
                    industryBenchmarks[industry!] = benchmark.AverageCompensation;
                }
            }
            
            var result = new List<ExecutiveCompensation>();
            foreach (var executive in allExecutives)
            {
                if (string.IsNullOrEmpty(executive.Industry) || !industryBenchmarks.ContainsKey(executive.Industry))
                    continue;
                
                var averageCompensation = industryBenchmarks[executive.Industry];
                if (executive.Compensation >= averageCompensation * 1.1m)
                {
                    result.Add(new ExecutiveCompensation
                    {
                        NameAndPosition = $"{executive.Name}, {executive.Position}",
                        Compensation = executive.Compensation,
                        AverageIndustryCompensation = averageCompensation
                    });
                }
            }
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting high compensation executives");
            throw;
        }
    }
}
