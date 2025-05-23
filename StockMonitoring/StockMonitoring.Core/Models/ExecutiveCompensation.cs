namespace StockMonitoring.Core.Models;

public record ExecutiveCompensation
{
    public string? NameAndPosition { get; set; }
    public decimal Compensation { get; set; }
    public decimal AverageIndustryCompensation { get; set; }
}
