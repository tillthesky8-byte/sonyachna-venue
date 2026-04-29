// Src/Strategies/Library/MACStrategy/MACStrategy.cs
using Venue.Src.Indicators.Library;
using Venue.Src.Indicators;
using Venue.Src.Domain;
using Venue.Src.Core.Execution.Broker;
using Venue.Src.Core.Portfolio;
using Microsoft.Extensions.Logging;
using Venue.Src.Infrastructure.Logging;
using Venue.Src.Strategies.Library.MACStrategy;

namespace Venue.Src.Strategies.Library.MACStrategy;
public class MACStrategy : IStrategy
{
    private readonly SMA _shortTermMA;
    private readonly SMA _longTermMA;
    private readonly MACConfig _config;
    private readonly ILogger<MACStrategy> _logger;

    public MACStrategy(ILogger<MACStrategy> logger, MACConfig config)
    {
        _logger = logger;
        if (config is MACConfig c) _config = c;
        else throw new ArgumentException("Invalid config type for MACStrategy");
        _logger.LogTrace("Initializing MACStrategy with config. ShortTermPeriod: {ValueColor}{ShortTermPeriod}{Reset}, LongTermPeriod: {ValueColor}{LongTermPeriod}{Reset}, Source: {ValueColor}{Source}{Reset}, AllocationPercentage: {ValueColor}{AllocationPercentage}{Reset}.",
            LoggerColors.ValueColor, _config.ShortTermPeriod, LoggerColors.Reset, LoggerColors.ValueColor, _config.LongTermPeriod, LoggerColors.Reset, LoggerColors.ValueColor, _config.Source, LoggerColors.Reset, LoggerColors.ValueColor, _config.AllocationPercentage, LoggerColors.Reset);
        
        _shortTermMA = new SMA(_config.ShortTermPeriod, _config.Source);
        _longTermMA = new SMA(_config.LongTermPeriod, _config.Source);
    }

    public void OnData(ProcessedDataRow row, IPortfolioManager portfolio, IBroker broker)
    {
        _shortTermMA.Update(row);
        _longTermMA.Update(row);
        _logger.LogTrace("Updated indicators. ShortTermMA: {ValueColor}{ShortTermMA}{Reset}, LongTermMA: {ValueColor}{LongTermMA}{Reset}.", LoggerColors.ValueColor, _shortTermMA.Value, LoggerColors.Reset, LoggerColors.ValueColor, _longTermMA.Value, LoggerColors.Reset);

        if (!_shortTermMA.IsReady || !_longTermMA.IsReady)
        {
            _logger.LogDebug("Indicators not ready. ShortTermMA IsReady: {ValueColor}{ShortTermMAIsReady}{Reset}, LongTermMA IsReady: {ValueColor}{LongTermMAIsReady}{Reset}. Skipping trading logic.", LoggerColors.ValueColor, _shortTermMA.IsReady, LoggerColors.Reset, LoggerColors.ValueColor, _longTermMA.IsReady, LoggerColors.Reset);
            return;
        }

        var position = portfolio.ActivePositions.FirstOrDefault();
        bool IsFlat = position == null;
        _logger.LogTrace("Current position status. IsFlat: {ValueColor}{IsFlat}{Reset}, Position: {ValueColor}{Position}{Reset}.", LoggerColors.ValueColor, IsFlat, LoggerColors.Reset, LoggerColors.ValueColor, position == null ? "None" : $"Symbol: {position.Symbol}, Direction: {(position.IsShort ? "Short" : "Long")}, Quantity: {position.Quantity}, AverageEntryPrice: {position.AverageEntryPrice}", LoggerColors.Reset);

        if (IsFlat && _shortTermMA.Value > _longTermMA.Value) // signal to buy
        {
            decimal allocation = portfolio.CurrentCash * _config.AllocationPercentage;
            decimal quantity = Math.Floor(allocation / row["close"]);
            _logger.LogTrace("qty: {ValueColor}{Quantity}{Reset}, row[close]: {ValueColor}{ClosePrice}{Reset}, allocation: {ValueColor}{Allocation}{Reset}.", LoggerColors.ValueColor, quantity, LoggerColors.Reset, LoggerColors.ValueColor, row["close"], LoggerColors.Reset, LoggerColors.ValueColor, allocation, LoggerColors.Reset);
            _logger.LogDebug("Signal to buy. Allocation: {ValueColor}{Allocation}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.ValueColor, allocation, LoggerColors.Reset, LoggerColors.ValueColor, quantity, LoggerColors.Reset);

            if (quantity > 0)
            {
                broker.SubmitOrder(new Order
                {
                    Symbol = "EURUSD",
                    Type = OrderType.Market,
                    Direction = OrderDirection.Buy,
                    Quantity = quantity,
                },
                portfolio.CurrentCash);
                _logger.LogTrace("Submitted buy order. Symbol: EURUSD, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.ValueColor, quantity, LoggerColors.Reset);
            }

        }

        else if (IsFlat && _shortTermMA.Value < _longTermMA.Value) // signal to sell
        {
            decimal allocation = portfolio.CurrentCash * _config.AllocationPercentage;
            decimal quantity = Math.Floor(allocation / row["close"]);
            _logger.LogTrace("qty: {ValueColor}{Quantity}{Reset}, row[close]: {ValueColor}{ClosePrice}{Reset}, allocation: {ValueColor}{Allocation}{Reset}.", LoggerColors.ValueColor, quantity, LoggerColors.Reset, LoggerColors.ValueColor, row["close"], LoggerColors.Reset, LoggerColors.ValueColor, allocation, LoggerColors.Reset);
            _logger.LogDebug("Signal to sell. Allocation: {ValueColor}{Allocation}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.ValueColor, allocation, LoggerColors.Reset, LoggerColors.ValueColor, quantity, LoggerColors.Reset);
            if (quantity > 0)
            {
                broker.SubmitOrder(new Order
                {
                    Symbol = "EURUSD",
                    Type = OrderType.Market,
                    Direction = OrderDirection.Sell,
                    Quantity = quantity,
                },
                portfolio.CurrentCash);
                _logger.LogTrace("Submitted sell order. Symbol: EURUSD, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.ValueColor, quantity, LoggerColors.Reset);
            }
        }

        else if (!IsFlat && position!.IsLong && _shortTermMA.Value < _longTermMA.Value) // signal to flip long to short
        {
            broker.SubmitOrder(new Order
            {
                Symbol = "EURUSD",
                Type = OrderType.Market,
                Direction = OrderDirection.Sell,
                Quantity = position.Quantity * 2, // close long and open short
            },
            portfolio.CurrentCash);
            _logger.LogTrace("Submitted flip order. Symbol: EURUSD, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.ValueColor, position.Quantity * 2, LoggerColors.Reset);

        }
        else if (!IsFlat && position!.IsShort && _shortTermMA.Value > _longTermMA.Value) // signal to flip short to long
        {
            broker.SubmitOrder(new Order
            {
                Symbol = "EURUSD",
                Type = OrderType.Market,
                Direction = OrderDirection.Buy,
                Quantity = Math.Abs(position.Quantity) * 2, // close short and open long
            },
            portfolio.CurrentCash);
            _logger.LogTrace("Submitted flip order. Symbol: EURUSD, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.ValueColor, Math.Abs(position.Quantity) * 2, LoggerColors.Reset);
        }
    }          
}

