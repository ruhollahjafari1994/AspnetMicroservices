using System;

namespace Ordering.Application.Exceptions
{
    /// <summary>
    /// Defines the NotFoundException type.
    /// </summary>
    public class NotFoundException : ApplicationException
    {
        /// <summary>
        /// Describes the NotFoundException operation.
        /// </summary>
        public NotFoundException(string name, object key)
            : base($"Entity \"{name}\" ({key}) was not found.")
        {
        }
    }
}
