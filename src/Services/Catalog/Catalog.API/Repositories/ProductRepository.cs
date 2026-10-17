using Catalog.API.Data.Interfaces;
using Catalog.API.Entities;
using Catalog.API.Repositories.Interfaces;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Catalog.API.Repositories
{
    /// <summary>
    /// Defines the ProductRepository type.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly ICatalogContext _context;

        /// <summary>
        /// Describes the ProductRepository operation.
        /// </summary>
        public ProductRepository(ICatalogContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Describes the GetProducts operation.
        /// </summary>
        public async Task<IEnumerable<Product>> GetProducts()
        {
            return await _context
                            .Products
                            .Find(p => true)
                            .ToListAsync();
        }

        /// <summary>
        /// Describes the GetProduct operation.
        /// </summary>
        public async Task<Product> GetProduct(string id)
        {
            return await _context
                           .Products
                           .Find(p => p.Id == id)
                           .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Describes the GetProductByName operation.
        /// </summary>
        public async Task<IEnumerable<Product>> GetProductByName(string name)
        {
            FilterDefinition<Product> filter = Builders<Product>.Filter.Regex(
                p => p.Name,
                new MongoDB.Bson.BsonRegularExpression(System.Text.RegularExpressions.Regex.Escape(name), "i"));

            return await _context
                            .Products
                            .Find(filter)
                            .ToListAsync();
        }

        /// <summary>
        /// Describes the GetProductByCategory operation.
        /// </summary>
        public async Task<IEnumerable<Product>> GetProductByCategory(string categoryName)
        {
            FilterDefinition<Product> filter = Builders<Product>.Filter.Eq(p => p.Category, categoryName);

            return await _context
                            .Products
                            .Find(filter)
                            .ToListAsync();
        }


        /// <summary>
        /// Describes the CreateProduct operation.
        /// </summary>
        public async Task CreateProduct(Product product)
        {
            await _context.Products.InsertOneAsync(product);
        }

        /// <summary>
        /// Describes the UpdateProduct operation.
        /// </summary>
        public async Task<bool> UpdateProduct(Product product)
        {
            var updateResult = await _context
                                        .Products
                                        .ReplaceOneAsync(filter: g => g.Id == product.Id, replacement: product);

            return updateResult.IsAcknowledged
                    && updateResult.ModifiedCount > 0;
        }

        /// <summary>
        /// Describes the DeleteProduct operation.
        /// </summary>
        public async Task<bool> DeleteProduct(string id)
        {
            FilterDefinition<Product> filter = Builders<Product>.Filter.Eq(p => p.Id, id);

            DeleteResult deleteResult = await _context
                                                .Products
                                                .DeleteOneAsync(filter);

            return deleteResult.IsAcknowledged
                && deleteResult.DeletedCount > 0;
        }
    }
}
