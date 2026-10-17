using AspnetRunBasics.Extensions;
using AspnetRunBasics.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AspnetRunBasics.Services
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
        public CatalogService(HttpClient client, ILogger<CatalogService> logger)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Describes the GetCatalog operation.
        /// </summary>
        public async Task<IEnumerable<CatalogModel>> GetCatalog()
        {
            var response = await _client.GetAsync("/Catalog");
            return await response.ReadContentAs<List<CatalogModel>>();
        }

        /// <summary>
        /// Describes the GetCatalog operation.
        /// </summary>
        public async Task<CatalogModel> GetCatalog(string id)
        {
            var response = await _client.GetAsync($"/Catalog/{id}");
            return await response.ReadContentAs<CatalogModel>();
        }

        /// <summary>
        /// Describes the GetCatalogByCategory operation.
        /// </summary>
        public async Task<IEnumerable<CatalogModel>> GetCatalogByCategory(string category)
        {
            var response = await _client.GetAsync($"/Catalog/GetProductByCategory/{category}");
            return await response.ReadContentAs<List<CatalogModel>>();
        }

        /// <summary>
        /// Describes the GetCatalogByName operation.
        /// </summary>
        public async Task<IEnumerable<CatalogModel>> GetCatalogByName(string name)
        {
            var response = await _client.GetAsync($"/Catalog/GetProductByName/{Uri.EscapeDataString(name)}");
            return await response.ReadContentAs<List<CatalogModel>>();
        }

        /// <summary>
        /// Describes the CreateCatalog operation.
        /// </summary>
        public async Task<CatalogModel> CreateCatalog(CatalogModel model)
        {            
            var response = await _client.PostAsJson($"/Catalog", model);
            if (response.IsSuccessStatusCode)
                return await response.ReadContentAs<CatalogModel>();
            else
            {
                throw new Exception("Something went wrong when calling api.");
            }
        }        
    }
}
