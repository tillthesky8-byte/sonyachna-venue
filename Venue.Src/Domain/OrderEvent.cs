// Src/Domain/OrderEvent.cs
namespace Venue.Src.Domain;
public class OrderEvent
{
    public Order Order { get; set; } = new();
    public decimal FillPrice { get; set; }
    public decimal FillQuantity { get; set; }
    public decimal Commission { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}