using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockMonitoring.Core.Exceptions;
using StockMonitoring.Core.Interfaces.Clients;
using StockMonitoring.Core.Models;
using StockMonitoring.Infrastructure.Configuration;

namespace StockMonitoring.Infrastructure.Clients;

public class StockClient(
    HttpClient httpClient,
    ILogger<StockClient> logger,
    IOptions<ApiOptions> apiOptions)
    : IStockClient
{
    private readonly string _authCode = apiOptions.Value.AuthCode;

    public async Task<IEnumerable<Company>> GetCompaniesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedCode = HttpUtility.UrlEncode(_authCode);
            var url = $"exchanges/ASX/companies?code={encodedCode}";
            
            logger.LogInformation("Fetching companies from {Url}", url);
            var result = await httpClient.GetFromJsonAsync<IEnumerable<Company>>(url, cancellationToken);
            return result ?? Enumerable.Empty<Company>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching companies from ASX exchange");
            throw new StockClientException("Failed to retrieve companies from the stock API", 
                "exchanges/ASX/companies", ex);
        }
    }
    
    public async Task<IEnumerable<Executive>> GetExecutivesAsync(string companySymbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedCode = HttpUtility.UrlEncode(_authCode);
            var url = $"companies/{companySymbol}/executives?code={encodedCode}";
            
            logger.LogInformation("Fetching executives for company {Symbol} from {Url}", companySymbol, url);
            var result = await httpClient.GetFromJsonAsync<IEnumerable<Executive>>(url, cancellationToken);
            return result ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching executives for company {Symbol}", companySymbol);
            throw new StockClientException($"Failed to retrieve executives for company {companySymbol}", 
                $"companies/{companySymbol}/executives", ex);
        }
    }
    
    public async Task<IndustryBenchmark?> GetIndustryBenchmarkAsync(string industry, CancellationToken cancellationToken = default)
    {
        try
        {
            var encodedCode = HttpUtility.UrlEncode(_authCode);
            var url = $"industries/{Uri.EscapeDataString(industry)}/benchmark?code={encodedCode}";
            
            logger.LogInformation("Fetching benchmark for industry {Industry} from {Url}", industry, url);
            return await httpClient.GetFromJsonAsync<IndustryBenchmark>(url, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching benchmark for industry {Industry}", industry);
            throw new StockClientException($"Failed to retrieve benchmark for industry {industry}", 
                $"industries/{industry}/benchmark", ex);
        }
    }
}
