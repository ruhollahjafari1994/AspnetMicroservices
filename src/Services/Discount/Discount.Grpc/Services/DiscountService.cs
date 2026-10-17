using Discount.Grpc.Entities;
using Discount.Grpc.Protos;
using Discount.Grpc.Repositories.Interfaces;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Discount.Grpc.Services
{
    /// <summary>
    /// Defines the DiscountService type.
    /// </summary>
    public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
    {
        private readonly IDiscountRepository _repository;
        private readonly ILogger<DiscountService> _logger;

        /// <summary>
        /// Describes the DiscountService operation.
        /// </summary>
        public DiscountService(IDiscountRepository repository, ILogger<DiscountService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Describes the GetDiscount operation.
        /// </summary>
        public override async Task<CouponModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
        {
            var coupon = await _repository.GetDiscount(request.ProductName);
            if (coupon == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, $"Discount with ProductName={request.ProductName} is not found."));
            }
            _logger.LogInformation("Discount is retrieved for ProductName : {productName}, Amount : {amount}", coupon.ProductName, coupon.Amount);

            return ToCouponModel(coupon);
        }

        /// <summary>
        /// Describes the CreateDiscount operation.
        /// </summary>
        public override async Task<CouponModel> CreateDiscount(CreateDiscountRequest request, ServerCallContext context)
        {
            var coupon = ToCoupon(request.Coupon);

            await _repository.CreateDiscount(coupon);
            _logger.LogInformation("Discount is successfully created. ProductName : {ProductName}", coupon.ProductName);

            return ToCouponModel(coupon);
        }

        /// <summary>
        /// Describes the UpdateDiscount operation.
        /// </summary>
        public override async Task<CouponModel> UpdateDiscount(UpdateDiscountRequest request, ServerCallContext context)
        {
            var coupon = ToCoupon(request.Coupon);

            await _repository.UpdateDiscount(coupon);
            _logger.LogInformation("Discount is successfully updated. ProductName : {ProductName}", coupon.ProductName);

            return ToCouponModel(coupon);
        }

        /// <summary>
        /// Describes the DeleteDiscount operation.
        /// </summary>
        public override async Task<DeleteDiscountResponse> DeleteDiscount(DeleteDiscountRequest request, ServerCallContext context)
        {
            var deleted = await _repository.DeleteDiscount(request.ProductName);
            var response = new DeleteDiscountResponse
            {
                Success = deleted
            };

            return response;
        }

        /// <summary>
        /// Describes the ToCoupon operation.
        /// </summary>
        private static Coupon ToCoupon(CouponModel model)
        {
            return new Coupon
            {
                Id = model.Id,
                ProductName = model.ProductName,
                Description = model.Description,
                Amount = model.Amount
            };
        }

        /// <summary>
        /// Describes the ToCouponModel operation.
        /// </summary>
        private static CouponModel ToCouponModel(Coupon coupon)
        {
            return new CouponModel
            {
                Id = coupon.Id,
                ProductName = coupon.ProductName,
                Description = coupon.Description,
                Amount = coupon.Amount
            };
        }
    }
}
