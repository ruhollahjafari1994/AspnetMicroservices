using Basket.API.Controllers;
using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using Basket.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Wolverine;
using Xunit;

namespace Basket.UnitTests;

/// <summary>
/// Covers validation and server-authoritative basket pricing.
/// </summary>
public sealed class BasketPricingTests
{
    private const string ProductId = "0123456789abcdef01234567";

    /// <summary>
    /// Ensures submitted names and prices are replaced by trusted catalog pricing.
    /// </summary>
    [Fact]
    public async Task UpdateBasket_ReplacesTamperedProductNameAndPrice()
    {
        var repository = new Mock<IBasketRepository>();
        var pricing = new Mock<IProductPricingService>();
        var saved = new ShoppingCart("customer");
        pricing.Setup(service => service.GetPriceAsync(ProductId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductPrice { ProductId = ProductId, ProductName = "Catalog Phone", UnitPrice = 49m, IsAvailable = true });
        repository.Setup(repo => repo.UpdateBasket(It.IsAny<ShoppingCart>(), 0))
            .Callback<ShoppingCart, long>((cart, _) => saved = cart).ReturnsAsync(true);
        repository.Setup(repo => repo.GetBasket("customer")).ReturnsAsync(() => saved);
        var controller = CreateController(repository, pricing);

        var result = await controller.UpdateBasket(new ShoppingCart("customer")
        {
            Items = { new ShoppingCartItem { ProductId = ProductId, ProductName = "Forged", Price = 0.01m, Quantity = 2 } }
        });

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal("Catalog Phone", saved.Items[0].ProductName);
        Assert.Equal(49m, saved.Items[0].Price);
    }

    /// <summary>
    /// Ensures invalid quantities and malformed or unknown products are rejected before persistence.
    /// </summary>
    [Theory]
    [InlineData("0123456789abcdef01234567", 0, false)]
    [InlineData("not-a-product", 1, false)]
    [InlineData("0123456789abcdef01234567", 1, true)]
    public async Task UpdateBasket_RejectsInvalidQuantityOrProduct(string id, int quantity, bool unavailable)
    {
        var repository = new Mock<IBasketRepository>();
        var pricing = new Mock<IProductPricingService>();
        pricing.Setup(service => service.GetPriceAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(unavailable ? new ProductPrice { ProductId = id, IsAvailable = false } : null);
        var controller = CreateController(repository, pricing);

        var result = await controller.UpdateBasket(new ShoppingCart("customer")
        {
            Items = { new ShoppingCartItem { ProductId = id, Quantity = quantity, Price = 1m } }
        });

        if (unavailable) Assert.IsType<UnprocessableEntityObjectResult>(result.Result);
        else Assert.IsType<BadRequestObjectResult>(result.Result);
        repository.Verify(repo => repo.UpdateBasket(It.IsAny<ShoppingCart>(), It.IsAny<long>()), Times.Never);
    }

    /// <summary>
    /// Ensures checkout requests without a stable non-empty idempotency key are rejected.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Checkout_RejectsMissingOrEmptyCheckoutId(string checkoutId)
    {
        var repository = new Mock<IBasketRepository>();
        var pricing = new Mock<IProductPricingService>();
        var controller = CreateController(repository, pricing);
        var model = new BasketCheckout { UserName = "customer", CheckoutId = checkoutId == null ? null : System.Guid.Parse(checkoutId) };

        var result = await controller.Checkout(model);

        Assert.IsType<BadRequestObjectResult>(result);
        repository.Verify(repo => repo.ReserveBasket(It.IsAny<string>(), It.IsAny<System.Guid>()), Times.Never);
    }

    private static BasketController CreateController(Mock<IBasketRepository> repository, Mock<IProductPricingService> pricing)
    {
        var bus = new Mock<IMessageBus>();
        var controller = new BasketController(repository.Object, pricing.Object, bus.Object);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "customer") }, "test"));
        controller.ControllerContext = new ControllerContext { HttpContext = context };
        return controller;
    }
}
