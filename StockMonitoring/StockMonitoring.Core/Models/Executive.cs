namespace StockMonitoring.Core.Models;

public record Executive
{
    public string? Cik { get; set; }
    public string? Symbol { get; set; }
    public string? CompanyName { get; set; }
    public string? IndustryTitle { get; set; }
    public string? AcceptedDate { get; set; }
    public string? FilingDate { get; set; }
    public string? NameAndPosition { get; set; }
    public int Year { get; set; }
    public decimal Salary { get; set; }
    public decimal Bonus { get; set; }
    public decimal? StockAward { get; set; }
    public decimal? IncentivePlanCompensation { get; set; }
    public decimal? AllOtherCompensation { get; set; }
    public decimal Total { get; set; }
    public string? Url { get; set; }
}
