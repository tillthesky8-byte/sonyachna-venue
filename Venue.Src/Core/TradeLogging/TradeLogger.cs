// Src/Core/TradeLogging/TradeLogger.cs
using Venue.Src.Domain;
using Microsoft.Extensions.Logging;
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
        _logger.LogInformation("Recorded equity {Equity} at {Timestamp}", equity, timestamp);
    }

    public void AppendTrades(IEnumerable<TradeRecord> trades)
    {
        Trades.AddRange(trades);
        _logger.LogInformation("Appended {Count} trades. Total trades: {TotalCount}", trades.Count(), Trades.Count);
    }

    public string CompileReport()
    {
        var finalEquity = EquityCurve.LastOrDefault().Value;
        var initialEquity = EquityCurve.FirstOrDefault().Value;
        var returnPct = initialEquity != 0 ? (finalEquity - initialEquity) / initialEquity * 100 : 0;
        var winCount = Trades.Count(t => t.NetProfit > 0);
        var winRate = Trades.Count > 0 ? (decimal)winCount / Trades.Count * 100 : 0;

        return $@"
        ===== Trade Report =====
        Initial Equity: {initialEquity}
        Final Equity: {finalEquity}
        Return (%): {returnPct:F2}
        Win Rate (%): {winRate:F2}
        Total Trades: {Trades.Count}
        Wins: {winCount}
        Losses: {Trades.Count - winCount}
        Max Drawdown: {MaxDrawdown}
        ";
    }
}