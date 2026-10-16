using System.Collections.Generic;

namespace EventBus.Messages.Events
{
    public class BasketCheckoutItem
    {
        public int Quantity { get; set; }
        public string Color { get; set; }
        public decimal Price { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
    }

    // The order id is also the durable saga identity.
    public class BeginCheckoutSaga
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public System.Collections.Generic.List<BasketCheckoutItem> Items { get; set; } = new System.Collections.Generic.List<BasketCheckoutItem>();
    }

    public class ClearBasketForOrder
    {
        public int CheckoutSagaId { get; set; }
        public string UserName { get; set; }
    }

    public class BasketClearedForOrder
    {
        public int CheckoutSagaId { get; set; }
    }

    public class CancelOrderSaga
    {
        public int CheckoutSagaId { get; set; }
        public int RetryCount { get; set; }
    }

    public class RestoreBasketForOrder
    {
        public int CheckoutSagaId { get; set; }
        public string UserName { get; set; }
        public System.Collections.Generic.List<BasketCheckoutItem> Items { get; set; } = new System.Collections.Generic.List<BasketCheckoutItem>();
    }

    public class BasketRestoredForOrder
    {
        public int CheckoutSagaId { get; set; }
    }
}
