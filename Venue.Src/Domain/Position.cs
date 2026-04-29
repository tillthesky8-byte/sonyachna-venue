// Src/Domain/Position.cs
using System.Runtime.CompilerServices;

namespace Venue.Src.Domain;
public class Position
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public bool IsLong => Quantity > 0;
    public bool IsShort => Quantity < 0;
    public decimal AverageEntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnL => Quantity * (CurrentPrice - AverageEntryPrice);
}