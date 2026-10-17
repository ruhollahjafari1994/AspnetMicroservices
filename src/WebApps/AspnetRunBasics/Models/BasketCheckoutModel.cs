namespace AspnetRunBasics.Models
{
    /// <summary>
    /// Defines the BasketCheckoutModel type.
    /// </summary>
    public class BasketCheckoutModel
    {
        /// <summary>
        /// Gets or sets the CheckoutId value.
        /// </summary>
        public System.Guid CheckoutId { get; set; }
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the TotalPrice value.
        /// </summary>
        public decimal TotalPrice { get; set; }

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

        // Provider token reference; raw card data is collected by the provider's hosted UI.
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
