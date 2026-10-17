namespace EventBus.Messages.Events
{
    /// <summary>
    /// Defines the BasketCheckoutEvent type.
    /// </summary>
    public class BasketCheckoutEvent : IntegrationBaseEvent
    {
        /// <summary>
        /// Executes the BasketCheckoutEvent operation.
        /// </summary>
        public BasketCheckoutEvent() { }

        /// <summary>
        /// Describes the BasketCheckoutEvent operation.
        /// </summary>
        public BasketCheckoutEvent(System.Guid checkoutId)
            : base(checkoutId, System.DateTime.UtcNow)
        {
        }
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the TotalPrice value.
        /// </summary>
        public decimal TotalPrice { get; set; }
        /// <summary>
        /// Gets or sets the Items value.
        /// </summary>
        public System.Collections.Generic.List<BasketCheckoutItem> Items { get; set; } = new System.Collections.Generic.List<BasketCheckoutItem>();

        // BillingAddress
        /// <summary>
        /// Gets or sets the FirstName value.
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// Gets or sets the LastName value.
        /// </summary>
        public string LastName { get; set; }
        /// <summary>
        /// Gets or sets the EmailAddress value.
        /// </summary>
        public string EmailAddress { get; set; }
        /// <summary>
        /// Gets or sets the AddressLine value.
        /// </summary>
        public string AddressLine { get; set; }
        /// <summary>
        /// Gets or sets the Country value.
        /// </summary>
        public string Country { get; set; }
        /// <summary>
        /// Gets or sets the State value.
        /// </summary>
        public string State { get; set; }
        /// <summary>
        /// Gets or sets the ZipCode value.
        /// </summary>
        public string ZipCode { get; set; }

        // Provider-owned payment token reference only. Never send card data in messages.
        /// <summary>
        /// Gets or sets the PaymentIntentId value.
        /// </summary>
        public string PaymentIntentId { get; set; }
        /// <summary>
        /// Gets or sets the PaymentMethod value.
        /// </summary>
        public int PaymentMethod { get; set; }
    }
}
