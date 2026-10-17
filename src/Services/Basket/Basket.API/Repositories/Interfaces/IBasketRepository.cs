using Basket.API.Entities;
using System.Threading.Tasks;

namespace Basket.API.Repositories.Interfaces
{
    /// <summary>
    /// Defines the IBasketRepository type.
    /// </summary>
    public interface IBasketRepository
    {
        /// <summary>
        /// Describes the GetBasket contract member.
        /// </summary>
        Task<ShoppingCart> GetBasket(string userName);
        /// <summary>
        /// Describes the UpdateBasket contract member.
        /// </summary>
        Task<ShoppingCart> UpdateBasket(ShoppingCart basket);
        /// <summary>
        /// Describes the DeleteBasket contract member.
        /// </summary>
        Task DeleteBasket(string userName);
    }
}
