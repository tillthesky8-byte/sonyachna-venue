// Src/Core/TradeLogging/ITradeLogger.cs
using Venue.Src.Domain;
namespace Venue.Src.Core.TradeLogging;
public interface ITradeLogger
{
    List<TradeRecord> Trades { get; }
    void RecordEquity(decimal equity, DateTime timestamp);
    void AppendTrades(IEnumerable<TradeRecord> trades);
    decimal GetMaxDrawdown();
    string CompileReport();
}