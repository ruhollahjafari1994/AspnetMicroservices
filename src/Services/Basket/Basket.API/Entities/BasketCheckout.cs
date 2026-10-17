using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Basket.API.Entities
{
    /// <summary>
    /// Defines the BasketCheckout type.
    /// </summary>
    public class BasketCheckout
    {
        /// <summary>
        /// Gets or sets the CheckoutId value.
        /// </summary>
        [System.ComponentModel.DataAnnotations.Required]
        public Guid? CheckoutId { get; set; }
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

        // Provider-owned token reference only. Card data must never reach this API.
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
