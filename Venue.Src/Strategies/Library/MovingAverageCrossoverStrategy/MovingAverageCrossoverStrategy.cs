// Src/Strategies/Library/MovingAverageCrossoverStrategy/MovingAverageCrossoverStrategy.cs
using Venue.Src.Indicators.Library;
using Venue.Src.Indicators;
using Venue.Src.Domain;
using Venue.Src.Core.Execution.Broker;
using Venue.Src.Core.Portfolio;

namespace Venue.Src.Strategies.Library.MovingAverageCrossoverStrategy;
public class MovingAverageCrossoverStrategy : IStrategy
{
    private readonly IIndicator _shortTermMA;
    private readonly IIndicator _longTermMA;
    private readonly MovingAverageCrossoverConfig _config;

    public MovingAverageCrossoverStrategy(MovingAverageCrossoverConfig config)
    {
        if (config is MovingAverageCrossoverConfig c) _config = c;
        else throw new ArgumentException("Invalid config type for MovingAverageCrossoverStrategy");
        _shortTermMA = new SMA(_config.ShortTermPeriod, _config.Source);
        _longTermMA = new SMA(_config.LongTermPeriod, _config.Source);
    }

    public void OnData(ProcessedDataRow row, IPortfolioManager portfolio, IBroker broker)
    {
        _shortTermMA.Update(row);
        _longTermMA.Update(row);

        if (!_shortTermMA.IsReady || !_longTermMA.IsReady)
            return;

        var position = portfolio.ActivePositions.FirstOrDefault();
        bool IsFlat = position == null;

        if (IsFlat && _shortTermMA.Value > _longTermMA.Value) // signal to buy
        {
            decimal allocation = portfolio.CurrentCash * _config.AllocationPercentage;
            decimal quantity = Math.Floor(allocation / row["close"]);

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
            }

        }

        else if (IsFlat && _shortTermMA.Value < _longTermMA.Value) // signal to sell
        {
            decimal allocation = portfolio.CurrentCash * _config.AllocationPercentage;
            decimal quantity = Math.Floor(allocation / row["close"]);

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
        }
    }          
}

