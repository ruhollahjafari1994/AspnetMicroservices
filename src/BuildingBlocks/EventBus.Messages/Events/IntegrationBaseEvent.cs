using System;

namespace EventBus.Messages.Events
{
    /// <summary>
    /// Defines the IntegrationBaseEvent type.
    /// </summary>
    public class IntegrationBaseEvent
    {
        /// <summary>
        /// Describes the IntegrationBaseEvent operation.
        /// </summary>
        public IntegrationBaseEvent()
        {
            Id = Guid.NewGuid();
            CreationDate = DateTime.UtcNow;
        }
        
        /// <summary>
        /// Describes the IntegrationBaseEvent operation.
        /// </summary>
        public IntegrationBaseEvent(Guid id, DateTime createDate)
        {
            Id = id;
            CreationDate = createDate;
        }
        
        /// <summary>
        /// Gets or sets the Id value.
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Gets or sets the CreationDate value.
        /// </summary>
        public DateTime CreationDate { get; set; }
    }
}
