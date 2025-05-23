namespace StockMonitoring.Api.Configuration;

public class ApiOptions
{
    public const string SectionName = "ApiSettings";
    
    public string AuthCode { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
}
