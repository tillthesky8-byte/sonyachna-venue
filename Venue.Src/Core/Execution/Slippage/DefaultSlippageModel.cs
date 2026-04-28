// Src/Core/Execution/Slippage/DefaultSlippageModel.cs
using Venue.Src.Domain;
using Venue.Src.Core.Execution.Slippage;
namespace Venue.Src.Core.Execution.Slippage;
public class DefaultSlippageModel() : ISlippageModel
{

    public decimal SpreadPenaltyRatio { get; init; } = 0.5m; 
    public decimal MaxVolumeParticipation { get; init; } = 0.1m;

    public decimal GetExecutionPrice(Order order, ProcessedDataRow row)
    {

        decimal slippage = row.Spread * SpreadPenaltyRatio;
        return order.Direction == OrderDirection.Buy 
            ? row.Open + slippage
            : row.Open - slippage;
    }

    public decimal GetVolumeConstraint(Order order, ProcessedDataRow row)
    {
        return Math.Min(order.UnfilledQuantity, row.Volume * MaxVolumeParticipation);
    }
}
