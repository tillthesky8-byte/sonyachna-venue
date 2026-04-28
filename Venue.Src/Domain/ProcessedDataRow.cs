// Src/Domain/ProcessedDataRow.cs
namespace Venue.Src.Domain;
public class ProcessedDataRow
{
    public DateTime Timestamp { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
    public decimal Spread { get; set; }
    public Dictionary<string, decimal> Externals { get; set; } = new (StringComparer.OrdinalIgnoreCase);
    public decimal this[string column]
    {
        get
        {
            return column switch
            {
                "timestamp" => Timestamp.Ticks,
                "open" => Open,
                "high" => High,
                "low" => Low,
                "close" => Close,
                "volume" => Volume,
                "spread" => Spread,
                _ => Externals.TryGetValue(column, out var value) ? value : throw new ArgumentException($"Column '{column}' not found.")
            };
        }
    }
}