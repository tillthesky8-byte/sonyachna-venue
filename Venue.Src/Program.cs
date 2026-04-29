// Src/Program.cs
using Microsoft.Extensions.Logging;
using Venue.Src.Application;
using Venue.Src.Infrastructure.Data;
using Venue.Src.Infrastructure.Logging;
using Venue.Src.Core.Portfolio;
using Venue.Src.Core.TradeLogging;
using Venue.Src.Core.Execution.Broker;
using Venue.Src.Strategies.Library.MACStrategy;
using Venue.Src.Core.Execution.Slippage;
using Venue.Src.Indicators.Library;

using ScottPlot;
using ScottPlot.Plottables;
using Venue.Src.Domain;
using System.Globalization;
using Venue.Src.Strategies.Library.BBOut;

namespace Venue.Src;
class Program
{
    static void Main(string[] args)
    {
        using var loggerFactory = LoggerFactoryProvider.Create(LogLevel.Information);
        var logger = loggerFactory.CreateLogger<Program>();
        logger.LogInformation("Venue backtest engine started.");


        var slippageModel = new DefaultSlippageModel
        (
            spreadPenaltyRatio: 0m,
            maxVolumeParticipation: 2m
        );
        logger.LogTrace("Initialized DefaultSlippageModel with SpreadPenaltyRatio={ValueColor}{SpreadPenaltyRatio}{Reset} and MaxVolumeParticipation={ValueColor}{MaxVolumeParticipation}{Reset}",
            LoggerColors.ValueColor, slippageModel.SpreadPenaltyRatio, LoggerColors.Reset, LoggerColors.ValueColor, slippageModel.MaxVolumeParticipation, LoggerColors.Reset);

        var broker = new Broker
        (
            logger: loggerFactory.CreateLogger<Broker>(),
            slippageModel: slippageModel,
            commissionPerShare: 0m
        );
        logger.LogTrace("Broker initialized with DefaultSlippageModel. with CommissionPerShare={ValueColor}{CommissionPerShare}{Reset}", LoggerColors.ValueColor, 0.01m, LoggerColors.Reset);

        var portfolio = new PortfolioManager
        (
            logger: loggerFactory.CreateLogger<PortfolioManager>(),
            initialCash: 10000m
        );
        logger.LogTrace("PortfolioManager initialized with InitialCash={ValueColor}{InitialCash}{Reset}", LoggerColors.ValueColor, 10000m, LoggerColors.Reset);

        var strategy = new BBOutStrategy
        ( 
            logger: loggerFactory.CreateLogger<BBOutStrategy>(),
            config: new BBOutConfig
            {
                Period = 200,
                StdDevMultiplier = 1m,
                Source = "close",
                AllocationPercentage = 0.05m
            }
        );
        logger.LogTrace("BBOutStrategy initialized with Period={ValueColor}{Period}{Reset}, StdDevMultiplier={ValueColor}{StdDevMultiplier}{Reset}, Source={ValueColor}{Source}{Reset}, AllocationPercentage={ValueColor}{AllocationPercentage}{Reset}.",
            LoggerColors.ValueColor, 200, LoggerColors.Reset, LoggerColors.ValueColor, 1m, LoggerColors.Reset, LoggerColors.ValueColor, "close", LoggerColors.Reset, LoggerColors.ValueColor, 0.05m, LoggerColors.Reset);

        var tradeLogger = new TradeLogger
        (
            loggerFactory.CreateLogger<TradeLogger>()
        );
        logger.LogTrace("TradeLogger initialized.");

        var simulator = new Simulator
        (
            loggerFactory.CreateLogger<Simulator>(),
            broker,
            portfolio,
            strategy,
            tradeLogger
        );
        logger.LogTrace("Simulator initialized with Broker, PortfolioManager, Strategy, and TradeLogger.");

        var exampleDataPoints = new RandomWalkGenerator().Generate(DateTime.UtcNow.AddDays(-1), 10000, 150.00m);

        //debug: plot the close prices of the generated data points to verify they look reasonable
        // double[] closePrices = exampleDataPoints.Select(dp => (double)dp.Close).ToArray();
        // int[] volumes = exampleDataPoints.Select(dp => (int)dp.Volume).ToArray();

        // var pltClosePrices = new Plot();
        // pltClosePrices.Add.Signal(closePrices);
        // pltClosePrices.SavePng("example_data.png", 1000, 600);

        // var pltVolumes = new Plot();
        // pltVolumes.Add.Signal(volumes);
        // pltVolumes.SavePng("example_volumes.png", 1000, 600);


        logger.LogInformation("Generated {ValueColor}{Count}{Reset} example data points for simulation.", LoggerColors.ValueColor, exampleDataPoints.Count(), LoggerColors.Reset);
        var tradeRecords = simulator.Run(exampleDataPoints);

        var tradeRecordsCsvPath = Path.Combine(Environment.CurrentDirectory, "trade_records.csv");
        var tradeRecordsCsvLines = new List<string>
        {
            "Symbol,EntryTime,ExitTime,EntryPrice,ExitPrice,Quantity,Direction,CommissionPaid,NetProfit"
        };

        tradeRecordsCsvLines.AddRange(tradeRecords.Select(tradeRecord => string.Join(",",
            CsvEscape(tradeRecord.Symbol),
            CsvEscape(tradeRecord.EntryTime.ToString("O", CultureInfo.InvariantCulture)),
            CsvEscape(tradeRecord.ExitTime.ToString("O", CultureInfo.InvariantCulture)),
            tradeRecord.EntryPrice.ToString(CultureInfo.InvariantCulture),
            tradeRecord.ExitPrice.ToString(CultureInfo.InvariantCulture),
            tradeRecord.Quantity.ToString(CultureInfo.InvariantCulture),
            tradeRecord.Direction.ToString(),
            tradeRecord.CommissionPaid.ToString(CultureInfo.InvariantCulture),
            tradeRecord.NetProfit.ToString(CultureInfo.InvariantCulture))));

        File.WriteAllLines(tradeRecordsCsvPath, tradeRecordsCsvLines);
        logger.LogInformation("Saved trade records to {Path}.", tradeRecordsCsvPath);

        // var plt = new Plot();
        // var dataPoints = exampleDataPoints.ToList();
        // var dataIndexByTimestamp = dataPoints
        //     .Select((dataPoint, index) => new { dataPoint.Timestamp, Index = index })
        //     .ToDictionary(x => x.Timestamp, x => x.Index);
        // var xs = dataPoints.Select((c, i) => (double)i).ToArray();
        // var closePrices = dataPoints.Select(dp => (double)dp.Close).ToArray();

        // plt.Add.Scatter(xs, closePrices);  // removed line




        // var sma10 = new SMA(10, "close");
        // var sma50 = new SMA(50, "close");
        // double[] sma10Values = new double[dataPoints.Count];
        // double[] sma50Values = new double[dataPoints.Count];

        // for (int i = 0; i < dataPoints.Count; i++)
        // {
        //     var dataPoint = dataPoints[i];
        //     sma10.Update(dataPoint);
        //     sma50.Update(dataPoint);

        //     sma10Values[i] = sma10.IsReady ? (double)sma10.Value : double.NaN;
        //     sma50Values[i] = sma50.IsReady ? (double)sma50.Value : double.NaN;
        // }

        // plt.Add.Scatter(xs, closePrices).LineWidth = 1;
        // plt.Add.Scatter(xs, sma10Values).LineWidth = 2;
        // plt.Add.Scatter(xs, sma50Values).LineWidth = 2;

        // var buys = tradeRecords.Where(tr => tr.Direction == OrderDirection.Buy).ToList();
        // var sells = tradeRecords.Where(tr => tr.Direction == OrderDirection.Sell).ToList();

        // plt.Add.Scatter
        // (
        //     buys.Select(tr => (double)dataIndexByTimestamp[tr.EntryTime]).ToArray(),
        //     buys.Select(tr => (double)tr.EntryPrice).ToArray()

        // ).MarkerSize = 10;

        // plt.Add.Scatter
        // (
        //     sells.Select(tr => (double)dataIndexByTimestamp[tr.EntryTime]).ToArray(),
        //     sells.Select(tr => (double)tr.EntryPrice).ToArray()

        // ).MarkerSize = 10;
        // // 4k plot
        // plt.SavePng("trade_signals.png", 3840, 2160);
    }

    private static string CsvEscape(string value)
        => $"\"{value.Replace("\"", "\"\"") }\"";
}