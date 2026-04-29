// Src/Domain/TradeRecord.cs

namespace Venue.Src.Domain;
public class TradeRecord
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime EntryTime { get; set; }
    public DateTime ExitTime { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public decimal Quantity { get; set; }
    public OrderDirection Direction { get; set; }
    public decimal CommissionPaid { get; set; }
    public decimal NetProfit => Direction == OrderDirection.Buy ?
        (ExitPrice - EntryPrice) * Quantity - CommissionPaid :
        (EntryPrice - ExitPrice) * Quantity - CommissionPaid;
}
