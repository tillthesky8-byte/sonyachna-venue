// Src/Domain/Enums.cs
namespace Venue.Src.Domain;
public enum OrderType
{
    Market,
    Limit,
    Stop
}

public enum OrderDirection
{
    Buy,
    Sell
}

public enum OrderStatus
{
    Submitted,
    Accepted,
    Filled,
    PartiallyFilled,
    Canceled,
    Rejected
}