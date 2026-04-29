//Src/Core/Execution/Broker/Broker.cs
using Venue.Src.Domain;
using Venue.Src.Core.Execution.Slippage;
using Venue.Src.Infrastructure.Logging;
using Microsoft.Extensions.Logging;


namespace Venue.Src.Core.Execution.Broker;
public class Broker : IBroker
{
    private readonly ISlippageModel _slippageModel;
    private readonly ILogger<Broker> _logger;
    private List<Order> _activeOrders = new();
    private readonly decimal _commissionPerShare;
    public event EventHandler<OrderEvent>? OnOrderFilledEvent;

    public Broker(ILogger<Broker> logger, ISlippageModel slippageModel, decimal commissionPerShare)
    {
        _logger = logger;
        _slippageModel = slippageModel;
        _commissionPerShare = commissionPerShare;
    }

    public Guid? SubmitOrder(Order order, decimal cash)
    {
        _logger.LogDebug("Received order submission request. Symbol: {ValueColor}{Symbol}{Reset}, Type: {ValueColor}{Type}{Reset}, Direction: {ValueColor}{Direction}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}, LimitPrice: {ValueColor}{LimitPrice}{Reset}, StopPrice: {ValueColor}{StopPrice}{Reset}.", LoggerColors.ValueColor, order.Symbol, LoggerColors.Reset, LoggerColors.ValueColor, order.Type, LoggerColors.Reset, LoggerColors.ValueColor, order.Direction, LoggerColors.Reset, LoggerColors.ValueColor, order.Quantity, LoggerColors.Reset, LoggerColors.ValueColor, order.LimitPrice, LoggerColors.Reset, LoggerColors.ValueColor, order.StopPrice, LoggerColors.Reset);
        if (order.Quantity <= 0) { _logger.LogWarning("Attempted to submit an order with non-positive quantity: {ValueColor}{Quantity}{Reset}. Order ignored.", LoggerColors.ValueColor, order.Quantity, LoggerColors.Reset); return null; }
        if (order.Type == OrderType.Limit && order.LimitPrice == null) { _logger.LogWarning("Attempted to submit a limit order without a limit price. Order ignored."); return null; }
        if (order.Type == OrderType.Stop && order.StopPrice == null) { _logger.LogWarning("Attempted to submit a stop order without a stop price. Order ignored."); return null; }
        if (order.Type == OrderType.Market && cash < order.Quantity * 100) { _logger.LogWarning("Insufficient cash to submit market order. Required: {ValueColor}{RequiredCash}{Reset}, Available: {ValueColor}{AvailableCash}{Reset}. Order ignored.", LoggerColors.ValueColor, order.Quantity * 100, LoggerColors.Reset, LoggerColors.ValueColor, cash, LoggerColors.Reset); return null; }
        
        order.Status = OrderStatus.Accepted;
        _activeOrders.Add(order);
        _logger.LogDebug("Order accepted and added to active orders. Order ID: {OrderIdColor}{OrderId}{Reset}, Symbol: {ValueColor}{Symbol}{Reset}, Type: {ValueColor}{Type}{Reset}, Direction: {ValueColor}{Direction}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, order.Symbol, LoggerColors.Reset, LoggerColors.ValueColor, order.Type, LoggerColors.Reset, LoggerColors.ValueColor, order.Direction, LoggerColors.Reset, LoggerColors.ValueColor, order.Quantity, LoggerColors.Reset);

        order.UnfilledQuantity = order.Quantity;
        return order.Id;
    }

    public void CancelOrder(Guid orderId)
    {
        _logger.LogDebug("Received order cancellation request. Order ID: {OrderIdColor}{OrderId}{Reset}.", LoggerColors.OrderIdColor, orderId, LoggerColors.Reset);
        var order = _activeOrders.FirstOrDefault(o => o.Id == orderId);
        if (order != null)
        {
            order.Status = OrderStatus.Canceled;
            _activeOrders.Remove(order);
            _logger.LogInformation("Order canceled and removed from active orders. Order ID: {OrderIdColor}{OrderId}{Reset}, Symbol: {ValueColor}{Symbol}{Reset}, Type: {ValueColor}{Type}{Reset}, Direction: {ValueColor}{Direction}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, order.Symbol, LoggerColors.Reset, LoggerColors.ValueColor, order.Type, LoggerColors.Reset, LoggerColors.ValueColor, order.Direction, LoggerColors.Reset, LoggerColors.ValueColor, order.Quantity, LoggerColors.Reset);
        }
        _logger.LogWarning("Attempted to cancel an order that does not exist. Order ID: {OrderIdColor}{OrderId}{Reset}. Cancellation ignored.", LoggerColors.OrderIdColor, orderId, LoggerColors.Reset);
    }

    public void ModifyOrder(Order modifiedOrder)
    {
        _logger.LogDebug("Received order modification request. Order ID: {OrderIdColor}{OrderId}{Reset}.", LoggerColors.OrderIdColor, modifiedOrder.Id, LoggerColors.Reset);
        var existingOrder = _activeOrders.FirstOrDefault(o => o.Id == modifiedOrder.Id);
        if (existingOrder != null && existingOrder.Status != OrderStatus.Filled)
        {
            existingOrder.Symbol = modifiedOrder.Symbol;
            existingOrder.Type = modifiedOrder.Type;
            existingOrder.Direction = modifiedOrder.Direction;
            existingOrder.Quantity = modifiedOrder.Quantity;
            existingOrder.UnfilledQuantity = modifiedOrder.UnfilledQuantity;
            existingOrder.AverageFillPrice = modifiedOrder.AverageFillPrice;
            existingOrder.LimitPrice = modifiedOrder.LimitPrice;
            existingOrder.StopPrice = modifiedOrder.StopPrice;
            _logger.LogInformation("Order modified. Order ID: {OrderIdColor}{OrderId}{Reset}, Symbol: {ValueColor}{Symbol}{Reset}, Type: {ValueColor}{Type}{Reset}, Direction: {ValueColor}{Direction}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.OrderIdColor, existingOrder.Id, LoggerColors.Reset, LoggerColors.ValueColor, existingOrder.Symbol, LoggerColors.Reset, LoggerColors.ValueColor, existingOrder.Type, LoggerColors.Reset, LoggerColors.ValueColor, existingOrder.Direction, LoggerColors.Reset, LoggerColors.ValueColor, existingOrder.Quantity, LoggerColors.Reset);
        }
        else _logger.LogWarning("Attempted to modify an order that does not exist or is already filled. Order ID: {OrderIdColor}{OrderId}{Reset}. Modification ignored.", LoggerColors.OrderIdColor, modifiedOrder.Id, LoggerColors.Reset);
    }


    public void ProcessOrders(ProcessedDataRow row)
    {
        _logger.LogTrace("{TickTimestampColor}{Timestamp}{Reset} Processing orders.", LoggerColors.TickTimestampColor, row.Timestamp, LoggerColors.Reset);
        if (_activeOrders.Count == 0) {_logger.LogDebug("No active orders to process."); return;}

        var filledOrders = new List<Order>();
        foreach (var order in _activeOrders)
        {
            _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Evaluating order. Symbol: {ValueColor}{Symbol}{Reset}, Type: {ValueColor}{Type}{Reset}, Direction: {ValueColor}{Direction}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}, UnfilledQuantity: {ValueColor}{UnfilledQuantity}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, order.Symbol, LoggerColors.Reset, LoggerColors.ValueColor, order.Type, LoggerColors.Reset, LoggerColors.ValueColor, order.Direction, LoggerColors.Reset, LoggerColors.ValueColor, order.Quantity, LoggerColors.Reset, LoggerColors.ValueColor, order.UnfilledQuantity, LoggerColors.Reset);
            
            var tradableQuantity = _slippageModel.GetVolumeConstraint(order, row);
            _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Calculated tradable quantity for order. Tradable Quantity: {ValueColor}{TradableQuantity}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, tradableQuantity, LoggerColors.Reset);

            if (tradableQuantity <= 0) _logger.LogWarning("Volume constraint is zero or negative at timestamp {TickTimestampColor}{Timestamp}{Reset}. No orders will be filled.", LoggerColors.TickTimestampColor, row.Timestamp, LoggerColors.Reset);
            if (order.UnfilledQuantity <= 0) {_logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Order is already filled.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset); continue;}
            
            switch (order.Type)
            {
                case OrderType.Market:
                    _logger.LogTrace("Order ID: {OrderIdColor}{OrderId}{Reset} Processing market order.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset);

                    var executionPrice = _slippageModel.GetExecutionPrice(order, row);
                    FillOrder(order, executionPrice, order.UnfilledQuantity, row.Timestamp);
                    break;

                case OrderType.Limit:
                    _logger.LogTrace("Order ID: {OrderIdColor}{OrderId}{Reset} Processing limit order.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset);

                    if (order.Direction == OrderDirection.Buy && row.Low <= order.LimitPrice)
                    {
                        decimal fillPrice = Math.Min(order.LimitPrice.Value, row.Open);
                        FillOrder(order, fillPrice, tradableQuantity, row.Timestamp);
                        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Limit buy order filled. Fill Price: {ValueColor}{FillPrice}{Reset}, Fill Quantity: {ValueColor}{FillQuantity}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, fillPrice, LoggerColors.Reset, LoggerColors.ValueColor, tradableQuantity, LoggerColors.Reset);
                    }
                    else if (order.Direction == OrderDirection.Sell && row.High >= order.LimitPrice)
                    {
                        decimal fillPrice = Math.Max(order.LimitPrice.Value, row.Open);
                        FillOrder(order, fillPrice, tradableQuantity, row.Timestamp);
                        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Limit sell order filled. Fill Price: {ValueColor}{FillPrice}{Reset}, Fill Quantity: {ValueColor}{FillQuantity}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, fillPrice, LoggerColors.Reset, LoggerColors.ValueColor, tradableQuantity, LoggerColors.Reset);
                    }
                    break;

                case OrderType.Stop:
                    _logger.LogTrace("Order ID: {OrderIdColor}{OrderId}{Reset} Processing stop order.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset);

                    if (order.Direction == OrderDirection.Buy && row.High >= order.StopPrice)
                    {
                        decimal basePrice = Math.Max(order.StopPrice.Value, row.Open); 
                        FillOrder(order, basePrice + (row.Spread * 0.5m), tradableQuantity, row.Timestamp);
                        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Stop buy order is executable. Row High: {ValueColor}{RowHigh}{Reset}, Stop Price: {ValueColor}{StopPrice}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, row.High, LoggerColors.Reset, LoggerColors.ValueColor, order.StopPrice, LoggerColors.Reset);
                    }
                    else if (order.Direction == OrderDirection.Sell && row.Low <= order.StopPrice)
                    {
                        decimal basePrice = Math.Min(order.StopPrice.Value, row.Open); 
                        FillOrder(order, basePrice - (row.Spread * 0.5m), tradableQuantity, row.Timestamp);
                        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Stop sell order is executable. Row Low: {ValueColor}{RowLow}{Reset}, Stop Price: {ValueColor}{StopPrice}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, row.Low, LoggerColors.Reset, LoggerColors.ValueColor, order.StopPrice, LoggerColors.Reset);
                    }
                    break;
            }
            if (order.UnfilledQuantity == 0) {filledOrders.Add(order); _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Order fully filled.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset);}
        }

        foreach (var filledOrder in filledOrders)
        {
            _activeOrders.Remove(filledOrder);
            _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Removed filled order from active orders.", LoggerColors.OrderIdColor, filledOrder.Id, LoggerColors.Reset);
        }
    }

    public void FillOrder(Order order, decimal fillPrice, decimal fillQuantity, DateTime timestamp)
    {
        _logger.LogTrace("Order ID: {OrderIdColor}{OrderId}{Reset} Filling order. Fill Price: {ValueColor}{FillPrice}{Reset}, Fill Quantity: {ValueColor}{FillQuantity}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, fillPrice, LoggerColors.Reset, LoggerColors.ValueColor, fillQuantity, LoggerColors.Reset);
        var previousValue = order.FilledQuantity * order.AverageFillPrice;
        var newValue = fillQuantity * fillPrice;
        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Calculated previous filled value and new filled value for order. Previous Filled Value: {ValueColor}{PreviousValue}{Reset}, New Filled Value: {ValueColor}{NewValue}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, previousValue, LoggerColors.Reset, LoggerColors.ValueColor, newValue, LoggerColors.Reset);

        order.UnfilledQuantity -= fillQuantity;
        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Updated unfilled quantity for order. Unfilled Quantity: {ValueColor}{UnfilledQuantity}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, order.UnfilledQuantity, LoggerColors.Reset);

        order.AverageFillPrice = (previousValue + newValue) / order.FilledQuantity;
        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Updated average fill price for order. Average Fill Price: {ValueColor}{AverageFillPrice}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, order.AverageFillPrice, LoggerColors.Reset);

        order.Status = order.UnfilledQuantity == 0 ? OrderStatus.Filled : OrderStatus.PartiallyFilled;

        var commission = fillQuantity * _commissionPerShare;
        _logger.LogDebug("Order ID: {OrderIdColor}{OrderId}{Reset} Calculated commission for filled order. Commission: {ValueColor}{Commission}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, commission, LoggerColors.Reset);

        _logger.LogTrace("Order ID: {OrderIdColor}{OrderId}{Reset} Invoking order filled event. Fill Price: {ValueColor}{FillPrice}{Reset}, Fill Quantity: {ValueColor}{FillQuantity}{Reset}, Commission: {ValueColor}{Commission}{Reset}.", LoggerColors.OrderIdColor, order.Id, LoggerColors.Reset, LoggerColors.ValueColor, fillPrice, LoggerColors.Reset, LoggerColors.ValueColor, fillQuantity, LoggerColors.Reset, LoggerColors.ValueColor, commission, LoggerColors.Reset);
        OnOrderFilledEvent?.Invoke(this, new OrderEvent
        {
            Order = order,
            FillPrice = fillPrice,
            FillQuantity = fillQuantity,
            Commission = commission,
            Timestamp = timestamp
        });
    }
}