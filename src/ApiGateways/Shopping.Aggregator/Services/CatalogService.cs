using Shopping.Aggregator.Extensions;
using Shopping.Aggregator.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Shopping.Aggregator.Services
{
    /// <summary>
    /// Defines the CatalogService type.
    /// </summary>
    public class CatalogService : ICatalogService
    {
        private readonly HttpClient _client;

        /// <summary>
        /// Describes the CatalogService operation.
        /// </summary>
        public CatalogService(HttpClient client)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Describes the GetCatalog operation.
        /// </summary>
        public async Task<IEnumerable<CatalogModel>> GetCatalog()
        {
            var response = await _client.GetAsync("/api/v1/Catalog");
            return await response.ReadContentAs<List<CatalogModel>>();
        }

        /// <summary>
        /// Describes the GetCatalog operation.
        /// </summary>
        public async Task<CatalogModel> GetCatalog(string id)
        {
            var response = await _client.GetAsync($"/api/v1/Catalog/{id}");
            return await response.ReadContentAs<CatalogModel>();
        }

        /// <summary>
        /// Describes the GetCatalogByCategory operation.
        /// </summary>
        public async Task<IEnumerable<CatalogModel>> GetCatalogByCategory(string category)
        {
            var response = await _client.GetAsync($"/api/v1/Catalog/GetProductByCategory/{category}");
            return await response.ReadContentAs<List<CatalogModel>>();
        }              
    }
}
