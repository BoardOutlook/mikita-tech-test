namespace StockMonitoring.Api.Models;

public record Executive
{
    public string? Name { get; set; }
    public string? Position { get; set; }
    public decimal Compensation { get; set; }
    public string? Industry { get; set; }
}
