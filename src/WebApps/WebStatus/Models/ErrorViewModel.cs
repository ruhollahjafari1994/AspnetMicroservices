using System;

namespace WebStatus.Models
{
    /// <summary>
    /// Defines the ErrorViewModel type.
    /// </summary>
    public class ErrorViewModel
    {
        /// <summary>
        /// Gets or sets the RequestId value.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// Describes the IsNullOrEmpty operation.
        /// </summary>
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
