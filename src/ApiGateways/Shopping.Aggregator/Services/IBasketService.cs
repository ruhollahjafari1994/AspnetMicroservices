using Shopping.Aggregator.Models;
using System.Threading.Tasks;

namespace Shopping.Aggregator.Services
{
    /// <summary>
    /// Defines the IBasketService type.
    /// </summary>
    public interface IBasketService
    {
        /// <summary>
        /// Describes the GetBasket contract member.
        /// </summary>
        Task<BasketModel> GetBasket(string userName);                
    }
}
