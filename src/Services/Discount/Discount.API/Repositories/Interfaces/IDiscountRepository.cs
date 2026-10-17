using Discount.API.Entities;
using System.Threading.Tasks;

namespace Discount.API.Repositories.Interfaces
{
    /// <summary>
    /// Defines the IDiscountRepository type.
    /// </summary>
    public interface IDiscountRepository
    {
        /// <summary>
        /// Describes the GetDiscount contract member.
        /// </summary>
        Task<Coupon> GetDiscount(string productName);

        /// <summary>
        /// Describes the CreateDiscount contract member.
        /// </summary>
        Task<bool> CreateDiscount(Coupon coupon);
        /// <summary>
        /// Describes the UpdateDiscount contract member.
        /// </summary>
        Task<bool> UpdateDiscount(Coupon coupon);
        /// <summary>
        /// Describes the DeleteDiscount contract member.
        /// </summary>
        Task<bool> DeleteDiscount(string productName);
    }
}
