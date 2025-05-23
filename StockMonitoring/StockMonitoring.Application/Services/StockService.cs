using Microsoft.Extensions.Logging;
using StockMonitoring.Core.Exceptions;
using StockMonitoring.Core.Interfaces.Clients;
using StockMonitoring.Core.Interfaces.Services;
using StockMonitoring.Core.Models;

namespace StockMonitoring.Application.Services;

public class StockService(
    IStockClient stockClient,
    ILogger<StockService> logger)
    : IStockService
{
    private const int MaxRetries = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);

    public async Task<IEnumerable<ExecutiveCompensation>> GetHighCompensationExecutivesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var companies = await ExecuteWithRetryAsync(
                ct => stockClient.GetCompaniesAsync(ct),
                "Failed to retrieve companies after multiple attempts",
                cancellationToken);
            
            var companiesList = companies.ToList();
            if (companiesList.Count == 0)
            {
                logger.LogWarning("No companies found for ASX exchange");
                return [];
            }
            
            var allExecutives = await GetAllExecutivesAsync(companiesList, cancellationToken);
            
            var industryBenchmarks = await GetIndustryBenchmarksAsync(allExecutives, cancellationToken);
            
            return ProcessExecutiveCompensation(allExecutives, industryBenchmarks);
        }
        catch (StockClientException scEx)
        {
            logger.LogError(scEx, "Stock API error at endpoint {Endpoint}: {Message}", 
                scEx.Endpoint, scEx.Message);
            throw new ApplicationException("Unable to retrieve stock data from the external service. Please try again later.", scEx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error getting high compensation executives");
            throw new ApplicationException("An unexpected error occurred while processing executive compensation data.", ex);
        }
    }

    private async Task<List<Executive>> GetAllExecutivesAsync(List<Company> companies, CancellationToken cancellationToken)
    {
        var allExecutives = new List<Executive>();
        
        var validCompanies = companies.Where(c => !string.IsNullOrEmpty(c.Symbol)).ToList();
        
        var executiveTasks = validCompanies.Select(async company => 
        {
            try
            {
                return await ExecuteWithRetryAsync(
                    ct => stockClient.GetExecutivesAsync(company.Symbol, ct),
                    $"Failed to retrieve executives for company {company.Symbol} after multiple attempts",
                    cancellationToken);
            }
            catch (StockClientException scEx)
            {
                logger.LogError(scEx, "Stock API error at endpoint {Endpoint} for company {Symbol}: {Message}", 
                    scEx.Endpoint, company.Symbol, scEx.Message);
                return [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting executives for company {Symbol}", company.Symbol);
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

    private async Task<Dictionary<string, decimal>> GetIndustryBenchmarksAsync(List<Executive> executives, CancellationToken cancellationToken)
    {
        var industryBenchmarks = new Dictionary<string, decimal>();
        
        var uniqueIndustries = executives
            .Select(e => e.IndustryTitle)
            .Where(i => !string.IsNullOrEmpty(i))
            .Distinct()
            .ToList();
        
        var benchmarkTasks = uniqueIndustries.Select(async industry => 
        {
            try
            {
                var benchmark = await ExecuteWithRetryAsync(
                    ct => stockClient.GetIndustryBenchmarkAsync(industry!, ct),
                    $"Failed to retrieve benchmark for industry {industry} after multiple attempts",
                    cancellationToken);
                return (Industry: industry!, Benchmark: benchmark);
            }
            catch (StockClientException scEx)
            {
                logger.LogError(scEx, "Stock API error at endpoint {Endpoint} for industry {Industry}: {Message}", 
                    scEx.Endpoint, industry, scEx.Message);
                return (Industry: industry!, Benchmark: null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error getting benchmark for industry {Industry}", industry);
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
            if (string.IsNullOrEmpty(executive.IndustryTitle) || !industryBenchmarks.TryGetValue(executive.IndustryTitle, out var averageCompensation))
                continue;

            if (executive.Total >= averageCompensation * 1.1m)
            {
                result.Add(new ExecutiveCompensation
                {
                    NameAndPosition = executive.NameAndPosition,
                    Compensation = executive.Total,
                    AverageIndustryCompensation = averageCompensation
                });
            }
        }
        
        return result;
    }
    
    private async Task<T> ExecuteWithRetryAsync<T>(Func<CancellationToken, Task<T>> operation, string errorMessage, CancellationToken cancellationToken)
    {
        int attemptCount = 0;
        Exception? lastException = null;

        while (attemptCount < MaxRetries)
        {
            try
            {
                if (attemptCount > 0)
                {
                    await Task.Delay(RetryDelay * attemptCount, cancellationToken);
                    logger.LogInformation("Retry attempt {Attempt} for operation", attemptCount);
                }

                return await operation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Operation was canceled");
                throw;
            }
            catch (StockClientException ex)
            {
                lastException = ex;
                logger.LogWarning(ex, "Attempt {Attempt} failed at endpoint {Endpoint}: {Message}", 
                    attemptCount + 1, ex.Endpoint, ex.Message);
                attemptCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Non-retryable error occurred");
                throw;
            }
        }

        logger.LogError(lastException, errorMessage);
        throw lastException ?? new ApplicationException(errorMessage);
    }
}
