// Src/Core/Execution/Slippage/ISlippageModel.cs
using Venue.Src.Domain;
namespace Venue.Src.Core.Execution.Slippage;
public interface ISlippageModel
{
    //<summary>
    // Calculate the execution price based on the order and current market price, incorporating slippage.
    //</summary>
    /// <param name="order">The order being executed.</param>
    /// <param name="mktPrice">The current market price of the asset.</param>
    /// <returns>The adjusted execution price after accounting for slippage.</returns>
    decimal GetExecutionPrice(Order order, decimal mktPrice);

    /// <summary>
    /// Gets the volume constraint based on the order and current market volume.
    /// </summary>
    /// <param name="order">The order for which to get the volume constraint.</param>
    /// <param name="mktVolume">The current market volume of the asset.</param>
    /// <returns>The volume constraint.</returns>
    decimal GetVolumeConstraint(Order order, decimal mktVolume);
}