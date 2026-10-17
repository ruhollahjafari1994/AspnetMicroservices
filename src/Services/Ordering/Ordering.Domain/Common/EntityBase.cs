using System;

namespace Ordering.Domain.Common
{
    /// <summary>
    /// Defines the EntityBase type.
    /// </summary>
    public abstract class EntityBase
    {
        /// <summary>
        /// Gets or sets the Id value.
        /// </summary>
        public int Id { get; protected set; }
        /// <summary>
        /// Gets or sets the CreatedBy value.
        /// </summary>
        public string CreatedBy { get; set; }
        /// <summary>
        /// Gets or sets the CreatedDate value.
        /// </summary>
        public DateTime CreatedDate { get; set; }
        /// <summary>
        /// Gets or sets the LastModifiedBy value.
        /// </summary>
        public string LastModifiedBy { get; set; }
        /// <summary>
        /// Gets or sets the LastModifiedDate value.
        /// </summary>
        public DateTime? LastModifiedDate { get; set; }
    }
}
