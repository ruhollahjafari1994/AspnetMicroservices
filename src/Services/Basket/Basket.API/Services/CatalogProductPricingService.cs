using Basket.API.GrpcServices;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Basket.API.Services
{
    /// <summary>
    /// Resolves product details from Catalog and discounts from the Discount gRPC service.
    /// </summary>
    public sealed class CatalogProductPricingService : IProductPricingService
    {
        private readonly HttpClient _catalogClient;
        private readonly DiscountGrpcService _discounts;

        /// <summary>
        /// Creates the authoritative product-pricing client.
        /// </summary>
        public CatalogProductPricingService(HttpClient catalogClient, DiscountGrpcService discounts)
        {
            _catalogClient = catalogClient ?? throw new ArgumentNullException(nameof(catalogClient));
            _discounts = discounts ?? throw new ArgumentNullException(nameof(discounts));
        }

        /// <inheritdoc />
        public async Task<ProductPrice> GetPriceAsync(string productId, CancellationToken cancellationToken = default)
        {
            using var response = await _catalogClient.GetAsync($"api/v1/Catalog/{Uri.EscapeDataString(productId)}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            var product = await response.Content.ReadFromJsonAsync<CatalogProductDto>(cancellationToken: cancellationToken);
            if (product == null)
            {
                return null;
            }

            var coupon = await _discounts.GetDiscount(product.Name);
            var maximumDiscount = (int)Math.Min(product.Price, int.MaxValue);
            var discount = Math.Clamp(coupon.Amount, 0, maximumDiscount);
            return new ProductPrice
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = Math.Max(0m, product.Price - discount),
                IsAvailable = product.IsAvailable && product.Price >= 0m
            };
        }
    }
}
