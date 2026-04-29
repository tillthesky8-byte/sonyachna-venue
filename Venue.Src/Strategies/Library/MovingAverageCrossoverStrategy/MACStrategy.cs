// Src/Strategies/Library/MovingAverageCrossoverStrategy/MovingAverageCrossoverConfig.cs
namespace Venue.Src.Strategies.Library.MACStrategy;
public class MACConfig
{
    public int ShortTermPeriod { get; set; } = 10;
    public int LongTermPeriod { get; set; } = 50;
    public string Source { get; set; } = "close";
    public decimal AllocationPercentage { get; set; } = 0.1m;
}