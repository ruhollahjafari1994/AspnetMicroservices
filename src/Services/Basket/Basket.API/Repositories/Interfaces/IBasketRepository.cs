using Basket.API.Entities;
using System;
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
        Task<bool> UpdateBasket(ShoppingCart basket, long expectedVersion);
        /// <summary>
        /// Describes the DeleteBasket contract member.
        /// </summary>
        Task DeleteBasket(string userName);

        /// <summary>
        /// Atomically reserves the current basket snapshot for a checkout ID.
        /// </summary>
        Task<BasketCheckoutReservation> ReserveBasket(string userName, Guid checkoutId);

        /// <summary>
        /// Loads a prior reservation without changing the active basket.
        /// </summary>
        Task<BasketCheckoutReservation> GetCheckoutReservation(string userName, Guid checkoutId);

        /// <summary>
        /// Marks a reserved checkout committed and releases its basket lock.
        /// </summary>
        Task<bool> CompleteCheckout(string userName, Guid checkoutId);

        /// <summary>
        /// Idempotently merges the reserved snapshot back into the active basket.
        /// </summary>
        Task<bool> RestoreCheckout(string userName, Guid checkoutId);
    }
}
