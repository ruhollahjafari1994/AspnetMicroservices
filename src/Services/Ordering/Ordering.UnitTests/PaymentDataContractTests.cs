using System;
using System.Linq;
using EventBus.Messages.Events;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Domain.Entities;
using Xunit;

namespace Ordering.UnitTests
{
    /// <summary>
    /// Defines the PaymentDataContractTests type.
    /// </summary>
    public class PaymentDataContractTests
    {
        /// <summary>
        /// Describes the Contracts_never_expose_raw_card_data operation.
        /// </summary>
        [Theory]
        [InlineData(typeof(Order))]
        [InlineData(typeof(BasketCheckoutEvent))]
        [InlineData(typeof(CheckoutOrderCommand))]
        public void Contracts_never_expose_raw_card_data(Type contractType)
        {
            var propertyNames = contractType.GetProperties().Select(property => property.Name).ToArray();

            Assert.DoesNotContain("CardNumber", propertyNames);
            Assert.DoesNotContain("CVV", propertyNames);
            Assert.DoesNotContain("Expiration", propertyNames);
            Assert.DoesNotContain("CardName", propertyNames);
        }
    }
}
