namespace StockMonitoring.Core.Models;

public record IndustryBenchmark
{
    public string? IndustryTitle { get; set; }
    public decimal AverageCompensation { get; set; }
    public int Year { get; set; }
}
