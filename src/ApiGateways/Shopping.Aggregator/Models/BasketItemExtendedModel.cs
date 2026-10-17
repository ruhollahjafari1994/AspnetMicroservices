namespace Shopping.Aggregator.Models
{
    /// <summary>
    /// Defines the BasketItemExtendedModel type.
    /// </summary>
    public class BasketItemExtendedModel
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

        //Product Related Additional Fields
        /// <summary>
        /// Gets or sets the Category value.
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Gets or sets the Summary value.
        /// </summary>
        public string Summary { get; set; }
        /// <summary>
        /// Gets or sets the Description value.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the ImageFile value.
        /// </summary>
        public string ImageFile { get; set; }
    }
}
