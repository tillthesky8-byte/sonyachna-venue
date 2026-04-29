
// Src/Strategies/Library/BBOut/BBOutStrategy.cs
using Venue.Src.Core.Execution.Broker;
using Venue.Src.Core.Portfolio;
using Venue.Src.Domain;
using Venue.Src.Indicators;
using Venue.Src.Indicators.Library;
using Venue.Src.Strategies;
using Venue.Src.Strategies.Library.BBOut;
using Microsoft.Extensions.Logging;
using Venue.Src.Infrastructure.Logging;

namespace Venue.Src.Strategies.Library.BBOut;

public class BBOutStrategy : IStrategy
{
    private readonly BollingerBands _bollingerBands;
    private readonly BBOutConfig _config;

    private readonly ILogger<BBOutStrategy> _logger;

    public BBOutStrategy(ILogger<BBOutStrategy> logger, BBOutConfig config)
    {
        _logger = logger;
        if (config is BBOutConfig c) _config = c;
        else throw new ArgumentException("Invalid config type for BBOutStrategy");
        _logger.LogTrace("Initializing BBOutStrategy with config. Period: {ValueColor}{Period}{Reset}, StdDevMultiplier: {ValueColor}{StdDevMultiplier}{Reset}, Source: {ValueColor}{Source}{Reset}, AllocationPercentage: {ValueColor}{AllocationPercentage}{Reset}.",
            LoggerColors.ValueColor, _config.Period, LoggerColors.Reset, LoggerColors.ValueColor, _config.StdDevMultiplier, LoggerColors.Reset, LoggerColors.ValueColor, _config.Source, LoggerColors.Reset, LoggerColors.ValueColor, _config.AllocationPercentage, LoggerColors.Reset);
        
        _bollingerBands = new BollingerBands(_config.Period, _config.StdDevMultiplier, _config.Source);
    }

    public void OnData(ProcessedDataRow row, IPortfolioManager portfolio, IBroker broker)
    {
        _bollingerBands.Update(row);
        _logger.LogTrace("Updated Bollinger Bands. MiddleBand: {ValueColor}{MiddleBand}{Reset}, UpperBand: {ValueColor}{UpperBand}{Reset}, LowerBand: {ValueColor}{LowerBand}{Reset}.", LoggerColors.ValueColor, _bollingerBands.MiddleBand, LoggerColors.Reset, LoggerColors.ValueColor, _bollingerBands.UpperBand, LoggerColors.Reset, LoggerColors.ValueColor, _bollingerBands.LowerBand, LoggerColors.Reset);

        if (!_bollingerBands.IsReady)
        {
            _logger.LogDebug("Bollinger Bands not ready. Skipping trading logic.");
            return;
        }

        var position = portfolio.ActivePositions.FirstOrDefault();
        bool IsFlat = position == null;
        _logger.LogTrace("Current position status. IsFlat: {ValueColor}{IsFlat}{Reset}, Position: {ValueColor}{Position}{Reset}.", LoggerColors.ValueColor, IsFlat, LoggerColors.Reset, LoggerColors.ValueColor, position == null ? "None" : $"Symbol: {position.Symbol}, Direction: {(position.IsShort ? "Short" : "Long")}, Quantity: {position.Quantity}, AverageEntryPrice: {position.AverageEntryPrice}", LoggerColors.Reset);

        if (IsFlat && row.High >= _bollingerBands.UpperBand && row.Low <= _bollingerBands.UpperBand) // signal to buy long
        {
            decimal allocation = portfolio.CurrentCash * _config.AllocationPercentage;
            decimal quantity = Math.Floor(allocation / row["close"]);
            _logger.LogTrace("qty: {ValueColor}{Quantity}{Reset}, row[close]: {ValueColor}{ClosePrice}{Reset}, allocation: {ValueColor}{Allocation}{Reset}.", LoggerColors.ValueColor, quantity, LoggerColors.Reset, LoggerColors.ValueColor, row["close"], LoggerColors.Reset, LoggerColors.ValueColor, allocation, LoggerColors.Reset);
            _logger.LogDebug("Signal to buy long. Allocation: {ValueColor}{Allocation}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.ValueColor, allocation, LoggerColors.Reset, LoggerColors.ValueColor, quantity, LoggerColors.Reset);

            if (quantity > 0)
            {
                broker.SubmitOrder(new Order
                {
                    Symbol = "EURUSD",
                    Type = OrderType.Market,
                    Direction = OrderDirection.Buy,
                    Quantity = quantity
                }, portfolio.CurrentCash);
            }
        }
        else if (IsFlat && row.Low <= _bollingerBands.LowerBand && row.High >= _bollingerBands.LowerBand) // signal to sell short
        {
            decimal allocation = portfolio.CurrentCash * _config.AllocationPercentage;
            decimal quantity = Math.Floor(allocation / row["close"]);
            _logger.LogTrace("qty: {ValueColor}{Quantity}{Reset}, row[close]: {ValueColor}{ClosePrice}{Reset}, allocation: {ValueColor}{Allocation}{Reset}.", LoggerColors.ValueColor, quantity, LoggerColors.Reset, LoggerColors.ValueColor, row["close"], LoggerColors.Reset, LoggerColors.ValueColor, allocation, LoggerColors.Reset);
            _logger.LogDebug("Signal to sell short. Allocation: {ValueColor}{Allocation}{Reset}, Quantity: {ValueColor}{Quantity}{Reset}.", LoggerColors.ValueColor, allocation, LoggerColors.Reset, LoggerColors.ValueColor, quantity, LoggerColors.Reset);

            if (quantity > 0)
            {
                broker.SubmitOrder(new Order
                {
                    Symbol = "EURUSD",
                    Type = OrderType.Market,
                    Direction = OrderDirection.Sell,
                    Quantity = quantity
                }, portfolio.CurrentCash);
            }
        }
        else if (!IsFlat && position!.IsShort && row.High >= _bollingerBands.MiddleBand && row.Low <= _bollingerBands.MiddleBand) // signal to exit short
        {
            _logger.LogDebug("Signal to exit short position. Current Price: {ValueColor}{CurrentPrice}{Reset}, MiddleBand: {ValueColor}{MiddleBand}{Reset}.", LoggerColors.ValueColor, row["close"], LoggerColors.Reset, LoggerColors.ValueColor, _bollingerBands.MiddleBand, LoggerColors.Reset);
            broker.SubmitOrder(new Order
            {
                Symbol = "EURUSD",
                Type = OrderType.Market,
                Direction = OrderDirection.Buy,
                Quantity = position.Quantity
            }, portfolio.CurrentCash);
        }
        else if (!IsFlat && position!.IsLong && row.High >= _bollingerBands.MiddleBand && row.Low <= _bollingerBands.MiddleBand) // signal to exit long
        {
            _logger.LogDebug("Signal to exit long position. Current Price: {ValueColor}{CurrentPrice}{Reset}, MiddleBand: {ValueColor}{MiddleBand}{Reset}.", LoggerColors.ValueColor, row["close"], LoggerColors.Reset, LoggerColors.ValueColor, _bollingerBands.MiddleBand, LoggerColors.Reset);
        
            broker.SubmitOrder(new Order
            {
                Symbol = "EURUSD",
                Type = OrderType.Market,
                Direction = position.IsShort ? OrderDirection.Buy : OrderDirection.Sell,
                Quantity = position.Quantity
            }, portfolio.CurrentCash);
            
        }
            
    }
}


