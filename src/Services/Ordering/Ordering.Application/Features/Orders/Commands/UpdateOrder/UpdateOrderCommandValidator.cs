using FluentValidation;

namespace Ordering.Application.Features.Orders.Commands.UpdateOrder
{
    /// <summary>
    /// Defines the UpdateOrderCommandValidator type.
    /// </summary>
    public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
    {
        /// <summary>
        /// Describes the UpdateOrderCommandValidator operation.
        /// </summary>
        public UpdateOrderCommandValidator()
        {
            RuleFor(p => p.UserName)
                .NotEmpty().WithMessage("{UserName} is required.")
                .NotNull()
                .MaximumLength(50).WithMessage("{UserName} must not exceed 50 characters.");

            RuleFor(p => p.EmailAddress)
               .NotEmpty().WithMessage("{EmailAddress} is required.");

            RuleFor(p => p.TotalPrice)
                .NotEmpty().WithMessage("{TotalPrice} is required.")
                .GreaterThan(0).WithMessage("{TotalPrice} should be greater than zero.");
        }
    }
}
