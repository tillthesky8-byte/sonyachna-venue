//Src/Core/Execution/Broker/Broker.cs
using Venue.Src.Domain;
using Venue.Src.Core.Execution.Slippage;
using Microsoft.Extensions.Logging;

namespace Venue.Src.Core.Execution.Broker;
public class Broker(ISlippageModel slippageModel, ILogger<Broker> logger, decimal commissionPerShare = 0.01m) : IBroker
{
    private readonly ISlippageModel _slippageMode = slippageModel;
    private readonly ILogger<Broker> _logger = logger;
    private List<Order> _activeOrders = new();
    private readonly decimal _commissionPerShare = commissionPerShare;
    public event EventHandler<OrderEvent>? OnOrderFilledEvent;

    public Guid? SubmitOrder(Order order, decimal cash)
    {
        if (order.Quantity <= 0) { _logger.LogWarning("Attempted to submit an order with non-positive quantity: {Quantity}. Order ignored.", order.Quantity); return null; }
        if (order.Type == OrderType.Limit && order.LimitPrice == null) { _logger.LogWarning("Attempted to submit a limit order without a limit price. Order ignored."); return null; }
        if (order.Type == OrderType.Stop && order.StopPrice == null) { _logger.LogWarning("Attempted to submit a stop order without a stop price. Order ignored."); return null; }
        if (order.Type == OrderType.Market && cash < order.Quantity * 100) { _logger.LogWarning("Insufficient cash to submit market order. Required: {RequiredCash}, Available: {AvailableCash}. Order ignored.", order.Quantity * 100, cash); return null; }
        
        order.Status = OrderStatus.Accepted;
        _activeOrders.Add(order);
        order.Quantity = order.UnfilledQuantity;
        return order.Id;
    }

    public void CancelOrder(Guid orderId)
    {
        var order = _activeOrders.FirstOrDefault(o => o.Id == orderId);
        if (order != null)
        {
            order.Status = OrderStatus.Canceled;
            _activeOrders.Remove(order);
        }
    }

    public void ModifyOrder(Order modifiedOrder)
    {
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
        }
        else _logger.LogWarning("Attempted to modify an order that does not exist or is already filled. Order ID: {OrderId}. Modification ignored.", modifiedOrder.Id);
    }


    public void ProcessOrders(ProcessedDataRow row)
    {
        if (_activeOrders.Count == 0) return;

        var filledOrders = new List<Order>();
        foreach (var order in _activeOrders)
        {
            var tradableQuantity = _slippageMode.GetVolumeConstraint(order, row);
            if (tradableQuantity <= 0) _logger.LogWarning("Volume constraint is zero or negative at timestamp {Timestamp}. No orders will be filled.", row.Timestamp);
            if (order.UnfilledQuantity <= 0) continue;
            
            switch (order.Type)
            {
                case OrderType.Market:
                    var executionPrice = _slippageMode.GetExecutionPrice(order, row);
                    FillOrder(order, executionPrice, order.UnfilledQuantity, row.Timestamp);
                    break;

                case OrderType.Limit:
                    if (order.Direction == OrderDirection.Buy && row.Low <= order.LimitPrice)
                    {
                        decimal fillPrice = Math.Min(order.LimitPrice.Value, row.Open);
                        FillOrder(order, fillPrice, tradableQuantity, row.Timestamp);
                    }
                    else if (order.Direction == OrderDirection.Sell && row.High >= order.LimitPrice)
                    {
                        decimal fillPrice = Math.Max(order.LimitPrice.Value, row.Open);
                        FillOrder(order, fillPrice, tradableQuantity, row.Timestamp);
                    }
                    break;

                case OrderType.Stop:
                    if (order.Direction == OrderDirection.Buy &&
                         row.High >= order.StopPrice)
                    {
                        decimal basePrice = Math.Max(order.StopPrice.Value, row.Open); 
                        FillOrder(order, basePrice + (row.Spread * 0.5m), tradableQuantity, row.Timestamp);
                    }
                    else if (order.Direction == OrderDirection.Sell && row.Low <= order.StopPrice)
                    {
                        decimal basePrice = Math.Min(order.StopPrice.Value, row.Open); 
                        FillOrder(order, basePrice - (row.Spread * 0.5m), tradableQuantity, row.Timestamp);
                    }
                    break;
            }
            if (order.UnfilledQuantity <= 0) filledOrders.Add(order);
        }

        foreach (var filledOrder in filledOrders)
        {
            _activeOrders.Remove(filledOrder);
            _logger.LogInformation("Order filled and removed from active orders. Order ID: {OrderId}, Symbol: {Symbol}, Quantity: {Quantity}, Fill Price: {FillPrice}, Timestamp: {Timestamp}.", filledOrder.Id, filledOrder.Symbol, filledOrder.FilledQuantity, filledOrder.AverageFillPrice, row.Timestamp);
        }
    }

    public void FillOrder(Order order, decimal fillPrice, decimal fillQuantity, DateTime timestamp)
    {
        var previousValue = order.FilledQuantity * order.AverageFillPrice;
        var newValue = fillQuantity * fillPrice;

        order.UnfilledQuantity -= fillQuantity;
        order.AverageFillPrice = (previousValue + newValue) / order.FilledQuantity;

        order.Status = order.UnfilledQuantity == 0 ? OrderStatus.Filled : OrderStatus.PartiallyFilled;
        var commission = fillQuantity * _commissionPerShare;
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