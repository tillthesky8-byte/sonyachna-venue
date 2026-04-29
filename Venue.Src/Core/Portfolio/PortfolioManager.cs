// Src/Portfolio/PortfolioManager.cs
using Venue.Src.Domain;
using Microsoft.Extensions.Logging;
using Venue.Src.Infrastructure.Logging;

namespace Venue.Src.Core.Portfolio;

public class PortfolioManager : IPortfolioManager
{
    private decimal _currentCash;
    private readonly Dictionary<string, Position> _activePositions = new();
    private readonly List<TradeRecord> _tradeHistory = new();
    private readonly Dictionary<string, DateTime> _lastEntryTime = new();
    private readonly ILogger<PortfolioManager> _logger;

    public decimal CurrentCash => _currentCash;
    public decimal CurrentEquity => _currentCash + _activePositions.Values.Sum(position => position.Quantity * position.CurrentPrice);
    public IEnumerable<Position> ActivePositions => _activePositions.Values;
    public IEnumerable<TradeRecord> TradeHistory => _tradeHistory;

    public PortfolioManager(ILogger<PortfolioManager> logger, decimal initialCash)
    {
        _logger = logger;
        _currentCash = initialCash;
    }
    public void OnOrderFilledEvent(object sender, OrderEvent orderEvent)
    {
        _logger.LogTrace("Order ID: {OrderIdColor}{OrderId}{Reset} Processing order filled event. Symbol: {ValueColor}{Symbol}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}, Fill Price: {ValueColor}{FillPrice}{Reset}, Timestamp: {ValueColor}{Timestamp}{Reset}.", LoggerColors.OrderIdColor, orderEvent.Order.Id, LoggerColors.Reset, LoggerColors.ValueColor, orderEvent.Order.Symbol, LoggerColors.Reset, LoggerColors.ValueColor, orderEvent.FillQuantity, LoggerColors.Reset, LoggerColors.ValueColor, orderEvent.FillPrice, LoggerColors.Reset, LoggerColors.ValueColor, orderEvent.Timestamp, LoggerColors.Reset);
        var order = orderEvent.Order;
        var q = orderEvent.FillQuantity;
        var p = orderEvent.FillPrice;
        var symbol = order.Symbol;

        var qtyChange = PayForOrderAndGetQuantity(order, p, q, orderEvent.Commission);
        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Calculated quantity change for order  Quantity Change: {ValueColor}{QtyChange}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, qtyChange, LoggerColors.Reset);

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
            _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Created new position for symbol. Symbol: {ValueColor}{Symbol}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}, Average Entry Price: {ValueColor}{AverageEntryPrice}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, symbol, LoggerColors.Reset, LoggerColors.ValueColor, position.Quantity, LoggerColors.Reset, LoggerColors.ValueColor, position.AverageEntryPrice, LoggerColors.Reset);
            return;
        }

        bool orderIncreasesPosition = !(
            position.Quantity > 0 && qtyChange < 0 || // closing or flipping long
            position.Quantity < 0 && qtyChange > 0    // closing or flipping short
        );

        if (orderIncreasesPosition)
        {
            _logger.LogTrace("Order increases existing position. Symbol: {ValueColor}{Symbol}{Reset}, Existing Quantity: {ValueColor}{ExistingQuantity}{Reset}, Quantity Change: {ValueColor}{QtyChange}{Reset}.", LoggerColors.ValueColor, symbol, LoggerColors.Reset, LoggerColors.ValueColor, position.Quantity, LoggerColors.Reset, LoggerColors.ValueColor, qtyChange, LoggerColors.Reset);
            
            position.AverageEntryPrice = UpdateAverageEntryPrice(position, p, qtyChange);
            position.Quantity += qtyChange;
            _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Updated average entry price and quantity for position. Symbol: {ValueColor}{Symbol}{Reset}, New Quantity: {ValueColor}{Quantity}{Reset}, New Average Entry Price: {ValueColor}{AverageEntryPrice}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, symbol, LoggerColors.Reset, LoggerColors.ValueColor, position.Quantity, LoggerColors.Reset, LoggerColors.ValueColor, position.AverageEntryPrice, LoggerColors.Reset);
            
            _lastEntryTime[symbol] = orderEvent.Timestamp;
        }
        else
        {
            _logger.LogTrace("Order ID: {OrderIdColor}{OrderId}{Reset} Order decreases or flips existing position. Symbol: {ValueColor}{Symbol}{Reset}, Existing Quantity: {ValueColor}{ExistingQuantity}{Reset}, Quantity Change: {ValueColor}{QtyChange}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, symbol, LoggerColors.Reset, LoggerColors.ValueColor, position.Quantity, LoggerColors.Reset, LoggerColors.ValueColor, qtyChange, LoggerColors.Reset);
           
            var remainingChange = ClosePortionOfPosition
            (
                position: position,
                quantityToClose: Math.Abs(qtyChange),
                entryTime: _lastEntryTime[symbol],
                exitTime: orderEvent.Timestamp,
                exitPrice: p,
                commission: orderEvent.Commission
            );
            _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Closed portion of position. Symbol: {ValueColor}{Symbol}{Reset}, Closed Quantity: {ValueColor}{ClosedQuantity}{Reset}, Remaining Change: {ValueColor}{RemainingChange}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, symbol, LoggerColors.Reset, LoggerColors.ValueColor, Math.Abs(qtyChange) - Math.Abs(remainingChange), LoggerColors.Reset, LoggerColors.ValueColor, remainingChange, LoggerColors.Reset);

            if (remainingChange != 0)
            {
                position.Quantity += remainingChange * Math.Sign(qtyChange);
                position.AverageEntryPrice = p;
                _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Flipped position. Symbol: {ValueColor}{Symbol}{Reset}, New Quantity: {ValueColor}{Quantity}{Reset}, New Average Entry Price: {ValueColor}{AverageEntryPrice}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, symbol, LoggerColors.Reset, LoggerColors.ValueColor, position.Quantity, LoggerColors.Reset, LoggerColors.ValueColor, position.AverageEntryPrice, LoggerColors.Reset);

                _lastEntryTime[symbol] = orderEvent.Timestamp;
            }

            if (position.Quantity == 0) {_activePositions.Remove(symbol);   _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Position fully closed and removed from active positions. Symbol: {ValueColor}{Symbol}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, symbol, LoggerColors.Reset);}        
        } 
    }

    public void UpdateMarketPrice(ProcessedDataRow row)
    {
        foreach (var position in _activePositions.Values)
        {
            position.CurrentPrice = row.Close;
            _logger.LogTrace("Updated market price for position. Symbol: {ValueColor}{Symbol}{Reset}, Current Price: {ValueColor}{CurrentPrice}{Reset}.", LoggerColors.ValueColor, position.Symbol, LoggerColors.Reset, LoggerColors.ValueColor, position.CurrentPrice, LoggerColors.Reset);
        }
    }

    public decimal PayForOrderAndGetQuantity(Order order, decimal price, decimal quantity, decimal commission)
    {
        var qtyChange = order.Direction == OrderDirection.Buy ? quantity : -quantity;
        var cashChange = -(commission + price * qtyChange);
        _currentCash += cashChange;
        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Updated cash balance after order fill. Cash Change: {ValueColor}{CashChange}{Reset}, New Cash Balance: {ValueColor}{NewCashBalance}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, cashChange, LoggerColors.Reset, LoggerColors.ValueColor, _currentCash, LoggerColors.Reset);
        return qtyChange;
    }

    public decimal UpdateAverageEntryPrice(Position position, decimal fillPrice, decimal qtyChange)
    {
        var totalCost = position.AverageEntryPrice * Math.Abs(position.Quantity) + fillPrice * Math.Abs(qtyChange);
        var totalQty = Math.Abs(position.Quantity) + Math.Abs(qtyChange);
        _logger.LogDebug("Calculating new average entry price. Position Symbol: {ValueColor}{Symbol}{Reset}, Existing Quantity: {ValueColor}{ExistingQuantity}{Reset}, Existing Average Entry Price: {ValueColor}{ExistingAverageEntryPrice}{Reset}, Fill Price: {ValueColor}{FillPrice}{Reset}, Quantity Change: {ValueColor}{QtyChange}{Reset}, Total Cost: {ValueColor}{TotalCost}{Reset}, Total Quantity: {ValueColor}{TotalQty}{Reset}.", LoggerColors.ValueColor, position.Symbol, LoggerColors.Reset, LoggerColors.ValueColor, position.Quantity, LoggerColors.Reset, LoggerColors.ValueColor, position.AverageEntryPrice, LoggerColors.Reset, LoggerColors.ValueColor, fillPrice, LoggerColors.Reset, LoggerColors.ValueColor, qtyChange, LoggerColors.Reset, LoggerColors.ValueColor, totalCost, LoggerColors.Reset, LoggerColors.ValueColor, totalQty, LoggerColors.Reset);
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
        _logger.LogDebug("Recorded closed trade. Symbol: {ValueColor}{Symbol}{Reset}, Closed Quantity: {ValueColor}{ClosedQuantity}{Reset}, Entry Price: {ValueColor}{EntryPrice}{Reset}, Exit Price: {ValueColor}{ExitPrice}{Reset}, Commission Paid: {ValueColor}{CommissionPaid}{Reset}.", LoggerColors.ValueColor, position.Symbol, LoggerColors.Reset, LoggerColors.ValueColor, closedPortion, LoggerColors.Reset, LoggerColors.ValueColor, position.AverageEntryPrice, LoggerColors.Reset, LoggerColors.ValueColor, exitPrice, LoggerColors.Reset, LoggerColors.ValueColor, commission, LoggerColors.Reset);
        return quantityToClose - closedPortion;
    }
}
