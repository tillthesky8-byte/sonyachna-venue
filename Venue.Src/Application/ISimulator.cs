// Src/Application/ISimulator.cs
using Venue.Src.Domain;
namespace Venue.Src.Application;
public interface ISimulator
{
    List<TradeRecord> Run(IEnumerable<ProcessedDataRow> processedDataPoints);
}