using Ordering.Application.Models;
using System.Threading.Tasks;

namespace Ordering.Application.Contracts.Infrastructure
{
    /// <summary>
    /// Defines the IEmailService type.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Describes the SendEmail contract member.
        /// </summary>
        Task<bool> SendEmail(Email email);
    }
}
