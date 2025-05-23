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
            
            var allExecutives = await GetAllExecutivesAsync(companies);
            
            var industryBenchmarks = await GetIndustryBenchmarksAsync(allExecutives);
            
            return ProcessExecutiveCompensation(allExecutives, industryBenchmarks);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting high compensation executives");
            throw;
        }
    }

    private async Task<List<Executive>> GetAllExecutivesAsync(List<Company> companies)
    {
        var allExecutives = new List<Executive>();
        
        var validCompanies = companies.Where(c => !string.IsNullOrEmpty(c.Symbol)).ToList();
        
        var executiveTasks = validCompanies.Select(async company => 
        {
            try
            {
                return await stockClient.GetExecutivesAsync(company.Symbol);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting executives for company {Symbol}", company.Symbol);
                return [];
            }
        });
        
        var executiveResults = await Task.WhenAll(executiveTasks);
        
        foreach (var executiveList in executiveResults)
        {
            if (executiveList.Any())
            {
                allExecutives.AddRange(executiveList);
            }
        }
        
        return allExecutives;
    }

    private async Task<Dictionary<string, decimal>> GetIndustryBenchmarksAsync(List<Executive> executives)
    {
        var industryBenchmarks = new Dictionary<string, decimal>();
        
        var uniqueIndustries = executives
            .Select(e => e.Industry)
            .Where(i => !string.IsNullOrEmpty(i))
            .Distinct()
            .ToList();
        
        var benchmarkTasks = uniqueIndustries.Select(async industry => 
        {
            try
            {
                var benchmark = await stockClient.GetIndustryBenchmarkAsync(industry!);
                return (Industry: industry!, Benchmark: benchmark);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting benchmark for industry {Industry}", industry);
                return (Industry: industry!, Benchmark: null);
            }
        });
        
        var benchmarkResults = await Task.WhenAll(benchmarkTasks);
        
        foreach (var result in benchmarkResults)
        {
            if (result.Benchmark != null)
            {
                industryBenchmarks[result.Industry] = result.Benchmark.AverageCompensation;
            }
        }
        
        return industryBenchmarks;
    }

    private List<ExecutiveCompensation> ProcessExecutiveCompensation(
        List<Executive> executives, 
        Dictionary<string, decimal> industryBenchmarks)
    {
        var result = new List<ExecutiveCompensation>();
        
        foreach (var executive in executives)
        {
            if (string.IsNullOrEmpty(executive.Industry) || !industryBenchmarks.TryGetValue(executive.Industry, out var averageCompensation))
                continue;

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
}
