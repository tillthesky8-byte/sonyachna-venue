// Src/Domain/Order.cs

namespace Venue.Src.Domain;
public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Symbol { get; set; } = string.Empty;
    public OrderType Type { get; set; }
    public OrderDirection Direction { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnfilledQuantity { get; set; }
    public decimal FilledQuantity => Quantity - UnfilledQuantity;
    public decimal AverageFillPrice { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? StopPrice { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Submitted;
}
