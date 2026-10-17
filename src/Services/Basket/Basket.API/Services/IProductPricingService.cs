using System.Threading;
using System.Threading.Tasks;

namespace Basket.API.Services
{
    /// <summary>
    /// Exposes authoritative catalog pricing to the basket API.
    /// </summary>
    public interface IProductPricingService
    {
        /// <summary>
        /// Loads a product from Catalog and applies the current server-side discount.
        /// </summary>
        Task<ProductPrice> GetPriceAsync(string productId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Contains trusted product data used to price a basket line.
    /// </summary>
    public sealed class ProductPrice
    {
        /// <summary>
        /// Gets or initializes the catalog product identifier.
        /// </summary>
        public string ProductId { get; init; }

        /// <summary>
        /// Gets or initializes the current catalog product name.
        /// </summary>
        public string ProductName { get; init; }

        /// <summary>
        /// Gets or initializes the discounted unit price.
        /// </summary>
        public decimal UnitPrice { get; init; }

        /// <summary>
        /// Gets or initializes whether the catalog currently permits this product to be sold.
        /// </summary>
        public bool IsAvailable { get; init; }
    }
}
