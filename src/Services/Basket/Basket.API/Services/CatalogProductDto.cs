using Newtonsoft.Json;

namespace Basket.API.Services
{
    /// <summary>
    /// Represents the product fields consumed from the Catalog API.
    /// </summary>
    public sealed class CatalogProductDto
    {
        /// <summary>
        /// Gets or sets the Catalog identifier.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Catalog display name.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Catalog unit price.
        /// </summary>
        [JsonProperty("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets whether the item is currently available to purchase.
        /// </summary>
        [JsonProperty("isAvailable")]
        public bool IsAvailable { get; set; } = true;
    }
}
