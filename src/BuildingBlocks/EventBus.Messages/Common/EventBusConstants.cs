namespace EventBus.Messages.Common
{
    /// <summary>
    /// Defines the EventBusConstants type.
    /// </summary>
    public static class EventBusConstants
	{
		/// <summary>
		/// Provides the BasketCheckoutQueue value.
		/// </summary>
		public const string BasketCheckoutQueue = "wolverine-basket-checkout-queue";
		/// <summary>
		/// Provides the BasketWorkflowQueue value.
		/// </summary>
		public const string BasketWorkflowQueue = "basket-workflow-queue";
		/// <summary>
		/// Provides the OrderSagaQueue value.
		/// </summary>
		public const string OrderSagaQueue = "order-saga-queue";
		/// <summary>
		/// Provides the OrderSagaEventsQueue value.
		/// </summary>
		public const string OrderSagaEventsQueue = "order-saga-events-queue";
	}
}
