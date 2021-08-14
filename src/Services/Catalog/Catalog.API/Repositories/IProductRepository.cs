using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Catalog.API.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Entities.Product>> GetProducts();
        Task<Entities.Product> GetProduct(string id);
        Task<IEnumerable<Entities.Product>> GetProductByName(string name);
        Task<IEnumerable<Entities.Product>> GetProductByCategory(string categoryName);
        Task CreateProduct(Entities.Product product);
        Task<bool> UpdateProduct(Entities.Product product);
        Task<bool> DeleteProduct(string id);
    }
}
