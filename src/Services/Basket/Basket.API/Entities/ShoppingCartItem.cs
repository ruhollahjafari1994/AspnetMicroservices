namespace Basket.API.Entities
{
    /// <summary>
    /// Defines the ShoppingCartItem type.
    /// </summary>
    public class ShoppingCartItem
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
}
