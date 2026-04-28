// Src/Portfolio/PortfolioManager.cs
using Venue.Src.Domain;
using Venue.Src.Core.Execution.Broker;
using Venue.Src.Core.Portfolio;
using Microsoft.Extensions.Logging;
namespace Venue.Src.Core.Portfolio;

public class PortfolioManager(decimal initialCash, ILogger<PortfolioManager> logger) : IPortfolioManager
{
    private decimal _currentCash = initialCash;
    private readonly Dictionary<string, Position> _activePositions = new();
    private readonly List<TradeRecord> _tradeHistory = new();
    private readonly Dictionary<string, DateTime> _lastEntryTime = new();
    private readonly ILogger<PortfolioManager> _logger = logger;


    public decimal CurrentCash => _currentCash;
    public decimal CurrentEquity => _currentCash + _activePositions.Values.Sum(p => p.Quantity * p.AverageEntryPrice);
    public IEnumerable<Position> ActivePositions => _activePositions.Values;
    public IEnumerable<TradeRecord> TradeHistory => _tradeHistory;


    public void OnOrderFilledEvent(object sender, OrderEvent orderEvent)
    {

        var order = orderEvent.Order;
        var q = orderEvent.FillQuantity;
        var p = orderEvent.FillPrice;
        var symbol = order.Symbol;

        var qtyChange = PayForOrderAndGetQuantity(order, p, q, orderEvent.Commission);

        if (!_activePositions.TryGetValue(symbol, out var position))
        {
            position = new Position
            {
                Symbol = symbol,
                Quantity = qtyChange,
                AverageEntryPrice = p,
                CurrentPrice = p,
            };

            _activePositions[symbol] = position;
            _lastEntryTime[symbol] = orderEvent.Timestamp;
            return;
        }

        bool orderIncreasesPosition = !(
            position.Quantity > 0 && qtyChange < 0 || // closing or flipping long
            position.Quantity < 0 && qtyChange > 0    // closing or flipping short
        );

        if (orderIncreasesPosition)
        {
            position.AverageEntryPrice = UpdateAverageEntryPrice(position, p, qtyChange);
            position.Quantity += qtyChange; // add to the existing position
            _lastEntryTime[symbol] = orderEvent.Timestamp;
        }
        else
        {

            var remainingChange = ClosePortionOfPosition
            (
                position: position,
                quantityToClose: Math.Abs(qtyChange),
                entryTime: _lastEntryTime[symbol],
                exitTime: orderEvent.Timestamp,
                exitPrice: p,
                commission: orderEvent.Commission);

            if (remainingChange != 0)
            {
                position.Quantity += remainingChange;
                position.AverageEntryPrice = p;
                _lastEntryTime[symbol] = orderEvent.Timestamp;
            }

            if (position.Quantity == 0) _activePositions.Remove(symbol);            
        } 
    }

    public void UpdateMarketPrice(ProcessedDataRow row)
    {
        foreach (var position in _activePositions.Values)
        {
            position.CurrentPrice = row.Close;
        }
    }

    public decimal PayForOrderAndGetQuantity(Order order, decimal price, decimal quantity, decimal commission)
    {
        _currentCash -= commission + price * quantity;
        var qtyChange = order.Direction == OrderDirection.Buy ? quantity : -quantity;
        return qtyChange;
    }

    public decimal UpdateAverageEntryPrice(Position position, decimal fillPrice, decimal qtyChange)
    {
        var totalCost = position.AverageEntryPrice * Math.Abs(position.Quantity) + fillPrice * Math.Abs(qtyChange);
        var totalQty = Math.Abs(position.Quantity) + Math.Abs(qtyChange);
        return totalCost / totalQty;
    }

    public decimal ClosePortionOfPosition(Position position, decimal quantityToClose, DateTime entryTime, DateTime exitTime, decimal exitPrice, decimal commission)
    {
        var closedPortion = Math.Min(Math.Abs(position.Quantity), quantityToClose);
        _tradeHistory.Add(new TradeRecord
        {
            Symbol = position.Symbol,
            EntryTime = entryTime,
            ExitTime = exitTime,
            EntryPrice = position.AverageEntryPrice,
            ExitPrice = exitPrice,
            Quantity = closedPortion,
            Direction = position.IsLong ? OrderDirection.Buy : OrderDirection.Sell,
            CommissionPaid = commission
        });
        position.Quantity -= closedPortion * Math.Sign(position.Quantity);
        return quantityToClose - closedPortion;
    }
}
