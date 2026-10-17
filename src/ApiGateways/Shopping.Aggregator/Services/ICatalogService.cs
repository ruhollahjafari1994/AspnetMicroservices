using Shopping.Aggregator.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shopping.Aggregator.Services
{
    /// <summary>
    /// Defines the ICatalogService type.
    /// </summary>
    public interface ICatalogService
    {
        /// <summary>
        /// Describes the GetCatalog contract member.
        /// </summary>
        Task<IEnumerable<CatalogModel>> GetCatalog();
        /// <summary>
        /// Describes the GetCatalogByCategory contract member.
        /// </summary>
        Task<IEnumerable<CatalogModel>> GetCatalogByCategory(string category);
        /// <summary>
        /// Describes the GetCatalog contract member.
        /// </summary>
        Task<CatalogModel> GetCatalog(string id);        
    }
}
