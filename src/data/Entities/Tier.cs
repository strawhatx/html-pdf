namespace data.Entities;

public class Tier
{
    public string Id { get; set; } = default!;
    public int MaxConcurrency { get; set; }
    public int MonthlyIncludedConversions { get; set; }
    public decimal PricePerExtraConversion { get; set; }
}