using Microsoft.EntityFrameworkCore;
using Ordering.Application.Contracts.Persistence;
using Ordering.Domain.Entities;
using Ordering.Infrastructure.Persistence;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ordering.Infrastructure.Repositories
{
    /// <summary>
    /// Defines the OrderRepository type.
    /// </summary>
    public class OrderRepository : RepositoryBase<Order>, IOrderRepository
    {
        /// <summary>
        /// Executes the OrderRepository operation.
        /// </summary>
        public OrderRepository(OrderContext dbContext) : base(dbContext)
        {
        }

        /// <summary>
        /// Describes the GetOrdersByUserName operation.
        /// </summary>
        public async Task<IEnumerable<Order>> GetOrdersByUserName(string userName)
        {
            var orderList = await _dbContext.Orders
                                .Where(o => o.UserName == userName)
                                .ToListAsync();
            return orderList;
        }

    }
}
