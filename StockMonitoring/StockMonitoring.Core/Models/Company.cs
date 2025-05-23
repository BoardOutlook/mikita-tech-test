namespace StockMonitoring.Core.Models;

public record Company
{
    public string? Symbol { get; set; }
    public string? Name { get; set; }
}
