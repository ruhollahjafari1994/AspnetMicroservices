using Shopping.Aggregator.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shopping.Aggregator.Services
{
    /// <summary>
    /// Defines the IOrderService type.
    /// </summary>
    public interface IOrderService
    {
        /// <summary>
        /// Describes the GetOrdersByUserName contract member.
        /// </summary>
        Task<IEnumerable<OrderResponseModel>> GetOrdersByUserName(string userName);
    }
}
