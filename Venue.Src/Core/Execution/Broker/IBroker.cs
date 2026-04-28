// Src/Core/Execution/Broker/IBroker.cs
using Venue.Src.Application;
using Venue.Src.Domain;
namespace Venue.Src.Core.Execution.Broker;
public interface IBroker
{    
    // <summary>
    // Process orders based on the latest market data update and the orders submitted at this tick.
    // This method is responsible for handling the order execution logic, including checking for fills, applying slippage, and updating order statuses.
    // </summary>
    /// <param name="dataUpdate">The latest market data update for the current tick.</param>
    /// <param name="orderSubmittedAtThisTick">The list of orders that were submitted at this tick and need to be processed. Can be null if no orders were submitted.</param>
   void ProcessOrders(ProcessedDataRow dataUpdate);

    // Methods for order management and event handling
    // <summary>
    // Submits a new order to the broker for execution.
    // </summary>
    /// <param name="order">The order to be submitted.</param>
    /// <param name="cash">The available cash for the order, used to validate market orders.</param>
    /// <remarks>The broker will handle the order according to its execution logic, which may include queuing, matching, and applying slippage based on the current market conditions.</remarks>
   Guid? SubmitOrder(Order order, decimal cash);

   // <summary>
   // Cancels an existing order based on its unique identifier.
   // </summary>
   /// <param name="orderId">The unique identifier of the order to be canceled.</param>
   /// <remarks>The broker will attempt to cancel the order if it has not been filled.</remarks>
   void CancelOrder(Guid orderId);

    // <summary>
    // Modifies an existing order with new parameters.
    // </summary>
    /// <param name="modifiedOrder">The order object containing the updated parameters. The order must have the same unique identifier as the original order to be modified.</param>
    /// <remarks>The broker will attempt to modify the order if it has not been filled, applying the new parameters according to the broker's execution logic.</remarks>
   void ModifyOrder(Order modifiedOrder);

    // Event triggered when an order is filled, providing details about the fill.
    // <remarks>Subscribers to this event can use the provided OrderEvent data to update their strategy state, portfolio, or perform any necessary actions based on the order fill.</remarks>
    // <param name="OrderEvent">An object containing details about the filled order, including the order itself, fill price, fill quantity, commission, and timestamp.</param>
   event EventHandler<OrderEvent> OnOrderFilledEvent;

   // Method to be called by the broker when an order is filled, which will trigger the OnOrderFilledEvent with the appropriate details.
   // <param name="order">The order that was filled.</param>
    // <param name="fillPrice">The price at which the order was filled, after
    // accounting for any slippage or execution adjustments.</param>
    // <param name="fillQuantity">The quantity of the order that was filled.</param>
    // <param name="timestamp">The timestamp of when the order was filled.</param>
   void FillOrder(Order order, decimal fillPrice, decimal fillQuantity, DateTime timestamp);

}