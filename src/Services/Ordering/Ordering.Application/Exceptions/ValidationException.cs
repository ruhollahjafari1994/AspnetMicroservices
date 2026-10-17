using FluentValidation.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ordering.Application.Exceptions
{
    /// <summary>
    /// Defines the ValidationException type.
    /// </summary>
    public class ValidationException : ApplicationException
    {
        /// <summary>
        /// Describes the ValidationException operation.
        /// </summary>
        public ValidationException()
            : base("One or more validation failures have occurred.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        /// <summary>
        /// Describes the ValidationException operation.
        /// </summary>
        public ValidationException(IEnumerable<ValidationFailure> failures)
            : this()
        {
            Errors = failures
                .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
                .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
        }

        /// <summary>
        /// Gets the Errors value.
        /// </summary>
        public IDictionary<string, string[]> Errors { get; }
    }
}
