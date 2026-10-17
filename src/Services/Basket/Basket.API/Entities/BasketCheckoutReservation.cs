using System;

namespace Basket.API.Entities
{
    /// <summary>
    /// Describes the durable result of reserving a basket for checkout.
    /// </summary>
    public sealed class BasketCheckoutReservation
    {
        /// <summary>
        /// Gets the reservation state returned by Redis.
        /// </summary>
        public BasketCheckoutReservationState State { get; init; }

        /// <summary>
        /// Gets the basket snapshot associated with the idempotency key.
        /// </summary>
        public ShoppingCart Snapshot { get; init; }
    }

    /// <summary>
    /// Lists the durable outcomes of a basket checkout reservation.
    /// </summary>
    public enum BasketCheckoutReservationState
    {
        /// <summary>Checkout owns the reserved basket snapshot.</summary>
        Reserved,
        /// <summary>Checkout completed and the reservation is committed.</summary>
        Committed,
        /// <summary>Compensation restored the reserved items.</summary>
        Restored,
        /// <summary>A different checkout currently owns the basket.</summary>
        Busy,
        /// <summary>No basket items were available to reserve.</summary>
        Empty
    }
}
