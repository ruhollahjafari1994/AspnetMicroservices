using AspnetRunBasics.Models;
using System.Threading.Tasks;

namespace AspnetRunBasics.Services
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
        /// <summary>
        /// Describes the UpdateBasket contract member.
        /// </summary>
        Task<BasketModel> UpdateBasket(BasketModel model);
        /// <summary>
        /// Describes the CheckoutBasket contract member.
        /// </summary>
        Task CheckoutBasket(BasketCheckoutModel model);
    }
}
