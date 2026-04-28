// Src/Core/Execution/Slippage/DefaultSlippageModel.cs
using Venue.Src.Domain;
using Venue.Src.Core.Execution.Slippage;
namespace Venue.Src.Core.Execution.Slippage;
public class DefaultSlippageModel() : ISlippageModel
{

    // A simple slippage model that applies a fixed percentage penalty to the execution price.
    public decimal SpreadPenalty { get; set; } = 0.0005m; 

    // Defines maximum percentage of market volume that can be executed to avoid excessive market impact.
    public decimal MaxVolumeParticipationRatio { get; set; } = 0.1m;

    public decimal GetExecutionPrice(Order order, decimal mktPrice)
    {
        if (order.Type == OrderType.Market)
            return order.Direction == OrderDirection.Buy ?
                mktPrice * (1 + SpreadPenalty) : // Buy orders execute at a higher price
                mktPrice * (1 - SpreadPenalty);  // Sell orders execute at a lower price
        
        else        
            return order.LimitPrice ?? mktPrice;
    }

    public decimal GetVolumeConstraint(Order order, decimal mktVolume)
    {
        return mktVolume * MaxVolumeParticipationRatio;
    }
}
