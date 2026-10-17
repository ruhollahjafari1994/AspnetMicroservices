using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Catalog.API.Entities
{
    /// <summary>
    /// Defines the Product type.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Gets or sets the Id value.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Name value.
        /// </summary>
        [BsonElement("Name")]
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the Category value.
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Gets or sets the Summary value.
        /// </summary>
        public string Summary { get; set; }
        /// <summary>
        /// Gets or sets the Description value.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the ImageFile value.
        /// </summary>
        public string ImageFile { get; set; }
        /// <summary>
        /// Gets or sets the Price value.
        /// </summary>
        public decimal Price { get; set; }
    }
}
