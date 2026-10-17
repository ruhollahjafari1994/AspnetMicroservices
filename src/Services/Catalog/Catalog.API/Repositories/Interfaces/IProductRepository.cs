using Catalog.API.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catalog.API.Repositories.Interfaces
{
    /// <summary>
    /// Defines the IProductRepository type.
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// Describes the GetProducts contract member.
        /// </summary>
        Task<IEnumerable<Product>> GetProducts();
        /// <summary>
        /// Describes the GetProduct contract member.
        /// </summary>
        Task<Product> GetProduct(string id);
        /// <summary>
        /// Describes the GetProductByName contract member.
        /// </summary>
        Task<IEnumerable<Product>> GetProductByName(string name);
        /// <summary>
        /// Describes the GetProductByCategory contract member.
        /// </summary>
        Task<IEnumerable<Product>> GetProductByCategory(string categoryName);

        /// <summary>
        /// Describes the CreateProduct contract member.
        /// </summary>
        Task CreateProduct(Product product);
        /// <summary>
        /// Describes the UpdateProduct contract member.
        /// </summary>
        Task<bool> UpdateProduct(Product product);
        /// <summary>
        /// Describes the DeleteProduct contract member.
        /// </summary>
        Task<bool> DeleteProduct(string id);
    }
}
