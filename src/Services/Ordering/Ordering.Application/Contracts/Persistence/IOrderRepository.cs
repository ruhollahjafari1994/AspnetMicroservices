using Ordering.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ordering.Application.Contracts.Persistence
{
    /// <summary>
    /// Defines the IOrderRepository type.
    /// </summary>
    public interface IOrderRepository : IAsyncRepository<Order>
    {
        /// <summary>
        /// Describes the GetOrdersByUserName contract member.
        /// </summary>
        Task<IEnumerable<Order>> GetOrdersByUserName(string userName);
    }
}
