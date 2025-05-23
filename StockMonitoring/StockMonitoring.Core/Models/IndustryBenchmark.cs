namespace StockMonitoring.Core.Models;

public record IndustryBenchmark
{
    public string? Industry { get; set; }
    public decimal AverageCompensation { get; set; }
}
