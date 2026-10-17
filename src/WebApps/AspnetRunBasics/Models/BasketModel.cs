using System.Collections.Generic;

namespace AspnetRunBasics.Models
{
    /// <summary>
    /// Defines the BasketModel type.
    /// </summary>
    public class BasketModel
    {
        /// <summary>
        /// Gets or sets the UserName value.
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the Items value.
        /// </summary>
        public List<BasketItemModel> Items { get; set; } = new List<BasketItemModel>();

        /// <summary>
        /// Gets or sets the optimistic concurrency version used by Basket API.
        /// </summary>
        public long Version { get; set; }
        /// <summary>
        /// Gets or sets the TotalPrice value.
        /// </summary>
        public decimal TotalPrice { get; set; }
    }
}
