using AspnetRunBasics.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspnetRunBasics.Services
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
        /// <summary>
        /// Describes the CancelOrder contract member.
        /// </summary>
        Task CancelOrder(int orderId);
    }
}
