namespace StockMonitoring.Api.Models;

public record IndustryBenchmark
{
    public string? Industry { get; set; }
    public decimal AverageCompensation { get; set; }
}
