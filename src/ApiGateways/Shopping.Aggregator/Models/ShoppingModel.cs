using System.Collections.Generic;

namespace Shopping.Aggregator.Models
{
    /// <summary>
    /// Defines the ShoppingModel type.
    /// </summary>
    public class ShoppingModel
    {
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the BasketWithProducts value.
        /// </summary>
        public BasketModel BasketWithProducts { get; set; }
        /// <summary>
        /// Gets or sets the Orders value.
        /// </summary>
        public IEnumerable<OrderResponseModel> Orders { get; set; }
    }
}
