// Src/Core/Execution/Slippage/ISlippageModel.cs
using Venue.Src.Domain;
namespace Venue.Src.Core.Execution.Slippage;
public interface ISlippageModel
{
    //<summary>
    // Calculate the execution price based on the order and current market price, incorporating slippage.
    //</summary>
    /// <param name="order">The order being executed.</param>
    /// <param name="row">The processed data row containing market information.</param>
    /// <returns>The adjusted execution price after accounting for slippage.</returns>
    decimal GetExecutionPrice(Order order, ProcessedDataRow row);

    /// <summary>
    /// Gets the volume constraint based on current market volume.
    /// </summary>
    /// <param name="order">The order being executed.</param>
    /// <param name="row">The processed data row containing market information.</param>
    /// <returns>The volume constraint.</returns>
    decimal GetVolumeConstraint(Order order, ProcessedDataRow row);
}