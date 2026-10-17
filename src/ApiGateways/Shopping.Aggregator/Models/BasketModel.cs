using System.Collections.Generic;

namespace Shopping.Aggregator.Models
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
        public List<BasketItemExtendedModel> Items { get; set; } = new List<BasketItemExtendedModel>();
        /// <summary>
        /// Gets or sets the TotalPrice value.
        /// </summary>
        public decimal TotalPrice { get; set; }
    }
}
