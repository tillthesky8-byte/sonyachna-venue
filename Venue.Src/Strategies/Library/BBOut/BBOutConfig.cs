// Src/Strategies/Library/BBOut/BBOutConfig.cs
namespace Venue.Src.Strategies.Library.BBOut;

public class BBOutConfig
{
    public int Period { get; set; }
    public decimal StdDevMultiplier { get; set; }
    public string Source { get; set; } = "close";
    public decimal AllocationPercentage { get; set; }
}