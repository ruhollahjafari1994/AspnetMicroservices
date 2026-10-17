using System.Collections.Generic;

namespace EventBus.Messages.Events
{
    /// <summary>
    /// Defines the BasketCheckoutItem type.
    /// </summary>
    public class BasketCheckoutItem
    {
        /// <summary>
        /// Gets or sets the Quantity value.
        /// </summary>
        public int Quantity { get; set; }
        /// <summary>
        /// Gets or sets the Color value.
        /// </summary>
        public string Color { get; set; }
        /// <summary>
        /// Gets or sets the Price value.
        /// </summary>
        public decimal Price { get; set; }
        /// <summary>
        /// Gets or sets the ProductId value.
        /// </summary>
        public string ProductId { get; set; }
        /// <summary>
        /// Gets or sets the ProductName value.
        /// </summary>
        public string ProductName { get; set; }
    }

    // The order id is also the durable saga identity.
    /// <summary>
    /// Defines the BeginCheckoutSaga type.
    /// </summary>
    public class BeginCheckoutSaga
    {
        // Explicit Wolverine saga correlation identity.
        /// <summary>
        /// Gets or sets the CheckoutSagaId value.
        /// </summary>
        public int CheckoutSagaId { get; set; }

        // Kept for compatibility with already queued messages.
        /// <summary>
        /// Gets or sets the Id value.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the Items value.
        /// </summary>
        public System.Collections.Generic.List<BasketCheckoutItem> Items { get; set; } = new System.Collections.Generic.List<BasketCheckoutItem>();
    }

    /// <summary>
    /// Defines the ClearBasketForOrder type.
    /// </summary>
    public class ClearBasketForOrder
    {
        /// <summary>
        /// Gets or sets the CheckoutSagaId value.
        /// </summary>
        public int CheckoutSagaId { get; set; }
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }
    }

    /// <summary>
    /// Defines the BasketClearedForOrder type.
    /// </summary>
    public class BasketClearedForOrder
    {
        /// <summary>
        /// Gets or sets the CheckoutSagaId value.
        /// </summary>
        public int CheckoutSagaId { get; set; }
    }

    /// <summary>
    /// Defines the CancelOrderSaga type.
    /// </summary>
    public class CancelOrderSaga
    {
        /// <summary>
        /// Gets or sets the CheckoutSagaId value.
        /// </summary>
        public int CheckoutSagaId { get; set; }
        /// <summary>
        /// Gets or sets the RetryCount value.
        /// </summary>
        public int RetryCount { get; set; }
    }

    /// <summary>
    /// Defines the RestoreBasketForOrder type.
    /// </summary>
    public class RestoreBasketForOrder
    {
        /// <summary>
        /// Gets or sets the CheckoutSagaId value.
        /// </summary>
        public int CheckoutSagaId { get; set; }
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the Items value.
        /// </summary>
        public System.Collections.Generic.List<BasketCheckoutItem> Items { get; set; } = new System.Collections.Generic.List<BasketCheckoutItem>();
    }

    /// <summary>
    /// Defines the BasketRestoredForOrder type.
    /// </summary>
    public class BasketRestoredForOrder
    {
        /// <summary>
        /// Gets or sets the CheckoutSagaId value.
        /// </summary>
        public int CheckoutSagaId { get; set; }
    }
}
