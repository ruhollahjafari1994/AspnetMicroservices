using Catalog.API.Data.Interfaces;
using Catalog.API.Entities;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace Catalog.API.Data
{
    /// <summary>
    /// Defines the CatalogContext type.
    /// </summary>
    public class CatalogContext : ICatalogContext
    {
        /// <summary>
        /// Describes the CatalogContext operation.
        /// </summary>
        public CatalogContext(IConfiguration configuration)
        {            
            var client = new MongoClient(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
            var database = client.GetDatabase(configuration.GetValue<string>("DatabaseSettings:DatabaseName"));

            Products = database.GetCollection<Product>(configuration.GetValue<string>("DatabaseSettings:CollectionName"));
            CatalogContextSeed.SeedData(Products);
        }

        /// <summary>
        /// Gets the Products value.
        /// </summary>
        public IMongoCollection<Product> Products { get; }
    }
}
