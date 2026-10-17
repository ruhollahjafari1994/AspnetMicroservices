using Catalog.API.Entities;
using MongoDB.Driver;

namespace Catalog.API.Data.Interfaces
{
    /// <summary>
    /// Defines the ICatalogContext type.
    /// </summary>
    public interface ICatalogContext
    {
        /// <summary>
        /// Describes the Products contract member.
        /// </summary>
        IMongoCollection<Product> Products { get; }
    }
}
