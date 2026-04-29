// Src/Core/Portfolio/IPortfolioManager.cs
using Venue.Src.Domain;
namespace Venue.Src.Core.Portfolio;
public interface IPortfolioManager
{
    decimal CurrentCash { get; }
    decimal CurrentEquity { get; }
    IEnumerable<Position> ActivePositions { get; }
    IEnumerable<TradeRecord> TradeHistory { get; }
    void OnOrderFilledEvent(object sender, OrderEvent orderEvent);
    void UpdateMarketPrice(ProcessedDataRow ro);
}
