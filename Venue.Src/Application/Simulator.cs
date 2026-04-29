// Src/Application/Simulator.cs
using Venue.Src.Domain;
using Microsoft.Extensions.Logging;
using Venue.Src.Core.Execution.Broker;
using Venue.Src.Core.Portfolio;
using Venue.Src.Strategies;
using Venue.Src.Core.TradeLogging;
using Venue.Src.Infrastructure.Logging;
namespace Venue.Src.Application;
public class Simulator : ISimulator
{
    private readonly ILogger<Simulator> _logger;
    private readonly IBroker _broker;
    private readonly IPortfolioManager _portfolio;
    private readonly IStrategy _strategy;
    private readonly ITradeLogger _tradeLogger;
    public Simulator(ILogger<Simulator> logger, IBroker broker, IPortfolioManager portfolio, IStrategy strategy, ITradeLogger tradeLogger)
    {
        _logger = logger;
        _broker = broker;
        _portfolio = portfolio;
        _strategy = strategy;
        _tradeLogger = tradeLogger;
        _broker.OnOrderFilledEvent += (sender, orderEvent) => _portfolio.OnOrderFilledEvent(sender ?? _broker, orderEvent);
    }
    public List<TradeRecord> Run(IEnumerable<ProcessedDataRow> dataStream)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("Simulator started with {ValueColor}{Count}{Reset} data points.", LoggerColors.ValueColor, dataStream.Count(), LoggerColors.Reset);
        foreach (var tick in dataStream)
        {
            _logger.LogTrace("{TickTimestampColor}{Timestamp}{Reset}: Processing tick: O={ValueColor}{Open}{Reset}, H={ValueColor}{High}{Reset}, L={ValueColor}{Low}{Reset}, C={ValueColor}{Close}{Reset}, V={ValueColor}{Volume}{Reset}, Spread={ValueColor}{Spread}{Reset}",
                LoggerColors.TickTimestampColor, tick.Timestamp, LoggerColors.Reset, LoggerColors.ValueColor, tick.Open, LoggerColors.Reset, LoggerColors.ValueColor, tick.High, LoggerColors.Reset, LoggerColors.ValueColor, tick.Low, LoggerColors.Reset, LoggerColors.ValueColor, tick.Close, LoggerColors.Reset, LoggerColors.ValueColor, tick.Volume, LoggerColors.Reset, LoggerColors.ValueColor, tick.Spread, LoggerColors.Reset);
            
            _portfolio.UpdateMarketPrice(tick);

            _strategy.OnData(tick, _portfolio, _broker);
            
            _broker.ProcessOrders(tick);

            _tradeLogger.RecordEquity(_portfolio.CurrentEquity, tick.Timestamp);
            _logger.LogTrace(" ");
            _logger.LogTrace("{TextColor}===========================================END OF TICK==========================================={Reset}", LoggerColors.TextColor, LoggerColors.Reset);
            _logger.LogTrace(" ");
        }
        _tradeLogger.AppendTrades(_portfolio.TradeHistory);
        _logger.LogInformation("Simulator finished processing data stream.");

        var report = _tradeLogger.CompileReport();
        _logger.LogInformation("Backtest Report:\n{ValueColor}{Report}{Reset}", LoggerColors.ValueColor, report, LoggerColors.Reset);


        stopwatch.Stop();
        _logger.LogInformation("Total simulation time: {ValueColor}{ElapsedSeconds}{Reset} seconds.", LoggerColors.ValueColor, stopwatch.Elapsed.TotalSeconds, LoggerColors.Reset);
        return _tradeLogger.Trades;
    }
}
