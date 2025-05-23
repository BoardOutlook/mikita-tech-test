using System.Net.Http.Json;
using System.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly string _baseUrl = apiOptions.Value.BaseUrl;

    public async Task<IEnumerable<Company>> GetCompaniesAsync()
    {
        try
        {
            var encodedCode = HttpUtility.UrlEncode(_authCode);
            var url = $"{_baseUrl}/exchanges/ASX/companies?code={encodedCode}";
            
            logger.LogInformation("Fetching companies from {Url}", url);
            return await httpClient.GetFromJsonAsync<IEnumerable<Company>>(url) ?? Enumerable.Empty<Company>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching companies from ASX exchange");
            return [];
        }
    }
    
    public async Task<IEnumerable<Executive>> GetExecutivesAsync(string companySymbol)
    {
        try
        {
            var url = $"{_baseUrl}/companies/{companySymbol}/executives?code={_authCode}";
            
            logger.LogInformation("Fetching executives for company {Symbol} from {Url}", companySymbol, url);
            return await httpClient.GetFromJsonAsync<IEnumerable<Executive>>(url) ?? Enumerable.Empty<Executive>();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching executives for company {Symbol}", companySymbol);
            return [];
        }
    }
    
    public async Task<IndustryBenchmark?> GetIndustryBenchmarkAsync(string industry)
    {
        try
        {
            var url = $"{_baseUrl}/industries/{Uri.EscapeDataString(industry)}/benchmark?code={_authCode}";
            
            logger.LogInformation("Fetching benchmark for industry {Industry} from {Url}", industry, url);
            return await httpClient.GetFromJsonAsync<IndustryBenchmark>(url);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching benchmark for industry {Industry}", industry);
            return null;
        }
    }
}
