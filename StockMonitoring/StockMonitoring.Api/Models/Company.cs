namespace StockMonitoring.Api.Models;

public record Company
{
    public string? Symbol { get; set; }
    public string? Name { get; set; }
}
