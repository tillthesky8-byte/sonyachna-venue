// Src/Core/TradeLogging/TradeLogger.cs
using Venue.Src.Domain;
using Microsoft.Extensions.Logging;
using Venue.Src.Infrastructure.Logging;
namespace Venue.Src.Core.TradeLogging;
public class TradeLogger(ILogger<TradeLogger> logger) : ITradeLogger
{
    private readonly ILogger<TradeLogger> _logger = logger;
    public SortedDictionary<DateTime, decimal> EquityCurve { get; } = new();
    public List<TradeRecord> Trades { get; } = new();
    public decimal MaxDrawdown => GetMaxDrawdown();
    public decimal GetMaxDrawdown()
    {
        if (EquityCurve.Count == 0) return 0;
        decimal peak = 0;
        decimal maxDrawdown = 0;

        foreach (var equity in EquityCurve.Values)
        {
            if (equity > peak) peak = equity;
            var drawdown = peak - equity;
            if (drawdown > maxDrawdown) maxDrawdown = drawdown;
        }
        return maxDrawdown;
    }

    public void RecordEquity(decimal equity, DateTime timestamp)
    {
        EquityCurve[timestamp] = equity;
        _logger.LogTrace("{TickTimestamp}{Timestamp}{Reset} Recorded equity {ValueColor}{Equity}{Reset} ", LoggerColors.TickTimestampColor, timestamp, LoggerColors.Reset, LoggerColors.ValueColor, equity, LoggerColors.Reset);
    }

    public void AppendTrades(IEnumerable<TradeRecord> trades)
    {
        Trades.AddRange(trades);
        _logger.LogTrace("Appended {ValueColor}{Count}{Reset} trades. Total trades: {ValueColor}{TotalCount}{Reset}", LoggerColors.ValueColor, trades.Count(), LoggerColors.Reset, LoggerColors.ValueColor, Trades.Count, LoggerColors.Reset);
    }

    public string CompileReport()
    {
        var finalEquity = EquityCurve.LastOrDefault().Value;
        var initialEquity = EquityCurve.FirstOrDefault().Value;
        var returnPct = initialEquity != 0 ? (finalEquity - initialEquity) / initialEquity * 100 : 0;
        var winCount = Trades.Count(t => t.NetProfit > 0);
        var winRate = Trades.Count > 0 ? (decimal)winCount / Trades.Count * 100 : 0;

        return
                $"Initial Equity: {initialEquity:C2}\n" +
                $"Final Equity: {finalEquity:C2}\n" + 
                $"Total Return: {returnPct:F2}%\n" +
                $"Max Drawdown: {MaxDrawdown:C2}\n" +
                $"Total Trades: {Trades.Count}\n" +
                $"Win Rate: {winRate:F2}%\n" +
                $"Win trades: {winCount}\n" +
                $"Loss trades: {Trades.Count - winCount}";

    }
}