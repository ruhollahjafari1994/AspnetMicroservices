namespace Discount.API.Entities
{
    /// <summary>
    /// Defines the Coupon type.
    /// </summary>
    public class Coupon
    {
        /// <summary>
        /// Gets or sets the Id value.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the ProductName value.
        /// </summary>
        public string ProductName { get; set; }
        /// <summary>
        /// Gets or sets the Description value.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the Amount value.
        /// </summary>
        public int Amount { get; set; }
    }
}
