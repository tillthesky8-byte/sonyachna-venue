// Src/Strategies/IStrategy.cs

using Venue.Src.Core.Execution.Broker;
using Venue.Src.Core.Portfolio;
using Venue.Src.Domain;


namespace Venue.Src.Strategies;
public interface IStrategy
{
    void OnData(ProcessedDataRow row, IPortfolioManager portfolio, IBroker broker);
}