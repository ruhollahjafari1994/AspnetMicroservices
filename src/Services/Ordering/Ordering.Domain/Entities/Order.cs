using Ordering.Domain.Common;

using System;

namespace Ordering.Domain.Entities
{
    /// <summary>
    /// Defines the Order type.
    /// </summary>
    public class Order : EntityBase
    {
        /// <summary>
        /// Gets or sets the CheckoutEventId value.
        /// </summary>
        public Guid? CheckoutEventId { get; set; }
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

        // Provider reference only; never persist PAN, expiry, or CVV.
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
