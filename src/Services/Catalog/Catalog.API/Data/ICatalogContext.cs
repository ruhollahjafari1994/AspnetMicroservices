using MongoDB.Driver;
 

namespace Catalog.API.Data
{
  public  interface ICatalogContext
    {
        IMongoCollection<Entities.Product> Products { get; }

    }
}
