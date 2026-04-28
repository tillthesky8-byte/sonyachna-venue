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
        _currentCash -= orderEvent.Commission;

        var order = orderEvent.Order;
        var q = orderEvent.FillQuantity;
        var p = orderEvent.FillPrice;
        var symbol = order.Symbol;


        decimal qtyChange = order.Direction == OrderDirection.Buy ? q : -q;
        decimal cashChange = -qtyChange * p;
        _currentCash += cashChange;

        if (!_activePositions.TryGetValue(symbol, out var position))
        {
            position = new Position
            {
                Symbol = symbol,
                Quantity = qtyChange,
                AverageEntryPrice = p,
                CurrentPrice = p
            };

            _activePositions[symbol] = position;
        }

        if (!(position.Quantity > 0 && qtyChange < 0 || position.Quantity < 0 && qtyChange > 0))
        {
            var totalCost = position.AverageEntryPrice * Math.Abs(position.Quantity) + p * Math.Abs(qtyChange);
            var totalQty = Math.Abs(position.Quantity) + Math.Abs(qtyChange);
            position.AverageEntryPrice = totalCost / totalQty;
            position.Quantity += qtyChange;
        }
        else
        {
            var closedQty = Math.Min(Math.Abs(position.Quantity), Math.Abs(qtyChange));
            var remainingChange = qtyChange - closedQty * Math.Sign(qtyChange);
            var realizedPnl = position.IsLong ?
                closedQty * (p - position.AverageEntryPrice) :
                closedQty * (position.AverageEntryPrice - p);

            _tradeHistory.Add(new TradeRecord
            {
                Symbol = symbol,
                EntryTime = _lastEntryTime.ContainsKey(symbol) ? _lastEntryTime[symbol] : DateTime.MinValue,
                ExitTime = orderEvent.Timestamp,
                EntryPrice = position.AverageEntryPrice,
                ExitPrice = p,
                Quantity = closedQty,
                ComissionPaid = orderEvent.Commission
            });
            position.Quantity += closedQty * Math.Sign(qtyChange);

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
}