using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;
using FulfillmentPlatform.Application.Common.Messaging;
using FulfillmentPlatform.Application.Orders;
using FulfillmentPlatform.Application.Orders.Events;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Domain.Enums;
using FulfillmentPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FulfillmentPlatform.UnitTests.Orders;

public class OrderServiceTests
{
    private readonly Mock<ICurrentUserService> _currentUser = new();
    private readonly Mock<IEventPublisher> _eventPublisher = new();
    private readonly AppDbContext _db;
    private readonly OrderService _service;

    // xUnit creates a NEW instance for EVERY test → every test gets a fresh database
    public OrderServiceTests()
    {
        // the logged-in user is Anna: a customer WITHOUT a shop (platform account)
        _currentUser.Setup(u => u.TenantId).Returns((int?)null);
        _currentUser.Setup(u => u.UserId).Returns("anna-id");
        _currentUser.Setup(u => u.UserIdOrNull).Returns("anna-id");
        _currentUser.Setup(u => u.IsInRole(Roles.Customer)).Returns(true);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options, _currentUser.Object);

        // two shops, one product each
        _db.Tenants.Add(new Tenant { Id = 1, Name = "ModeHaus", Slug = "modehaus" });
        _db.Tenants.Add(new Tenant { Id = 2, Name = "SchuhWerk", Slug = "schuhwerk" });
        _db.Products.Add(new Product { Id = 1, TenantId = 1, Name = "TShirt", Sku = "TS-1", Price = 500, StockQuantity = 10, CategoryId = 1, IsActive = true });
        _db.Products.Add(new Product { Id = 2, TenantId = 2, Name = "Sneaker", Sku = "SN-1", Price = 20, StockQuantity = 5, CategoryId = 2, IsActive = true });
        _db.Products.Add(new Product { Id = 3, TenantId = 1, Name = "Old Sandal", Sku = "OLD-1", Price = 10, StockQuantity = 5, CategoryId = 2, IsActive = false });
        _db.SaveChanges();

        _service = new OrderService(_db, _currentUser.Object, _eventPublisher.Object);
    }

    // helper: a valid checkout request with the given (productId, quantity) pairs
    private static CheckoutRequest Cart(params (int ProductId, int Quantity)[] items) => new()
    {
        Items = items.Select(i => new CheckoutItemRequest { ProductId = i.ProductId, Quantity = i.Quantity }).ToList(),
        Street = "Lindachstraße 26",
        PostalCode = "72764",
        City = "Reutlingen"
    };

    // reads the REAL stored value (not the object EF is currently tracking in memory)
    private int StoredStock(int productId) =>
        _db.Products.AsNoTracking().IgnoreQueryFilters().Single(p => p.Id == productId).StockQuantity;

    [Fact]
    public async Task CheckoutAsync_OneShop_ReducesStockAndCalculatesTotal()
    {
        var result = await _service.CheckoutAsync(Cart((1, 2)));

        var order = Assert.Single(result.Orders);
        Assert.Equal(1000, order.TotalAmount);                  // 2 × 500
        Assert.Equal(500, order.Items.Single().UnitPrice);      // price snapshot
        Assert.Equal("Pending", order.Status);
        Assert.Equal("ModeHaus", order.ShopName);
        Assert.Single(order.StatusHistory);
        Assert.Equal(8, StoredStock(1));                        // 10 − 2
    }

    [Fact]
    public async Task CheckoutAsync_TwoShops_CreatesOneOrderPerShopWithSameCheckoutId()
    {
        var result = await _service.CheckoutAsync(Cart((1, 1), (2, 2)));

        Assert.Equal(2, result.Orders.Count);                                   // split per shop
        Assert.All(result.Orders, o => Assert.Equal(result.CheckoutId, o.CheckoutId));
        Assert.Contains(result.Orders, o => o.ShopName == "ModeHaus" && o.TotalAmount == 500);
        Assert.Contains(result.Orders, o => o.ShopName == "SchuhWerk" && o.TotalAmount == 40);
        Assert.Equal(540, result.GrandTotal);
    }

    [Fact]
    public async Task CheckoutAsync_NotEnoughStockInOneShop_SavesNothing()
    {
        // product 1 is fine, product 2 has only 5 → the whole checkout must fail
        await Assert.ThrowsAsync<ConflictException>(() => _service.CheckoutAsync(Cart((1, 1), (2, 6))));

        Assert.Equal(10, StoredStock(1));                                       // all or nothing
        Assert.Equal(5, StoredStock(2));
        Assert.Empty(_db.Orders.IgnoreQueryFilters());
        _eventPublisher.Verify(p => p.PublishAsync(It.IsAny<OrderStatusChangedEvent>()), Times.Never);
    }

    [Fact]
    public async Task CheckoutAsync_DuplicateProduct_ThrowsBadRequest()
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CheckoutAsync(Cart((1, 1), (1, 2))));
    }

    [Fact]
    public async Task CheckoutAsync_InactiveProduct_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CheckoutAsync(Cart((3, 1))));
    }

    [Fact]
    public async Task CheckoutAsync_TwoShops_PublishesOneNewOrderEventPerShop()
    {
        await _service.CheckoutAsync(Cart((1, 1), (2, 1)));

        _eventPublisher.Verify(p => p.PublishAsync(It.Is<OrderStatusChangedEvent>(e =>
            e.OldStatus == "None" && e.NewStatus == "Pending")), Times.Exactly(2));
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<OrderStatusChangedEvent>(e => e.TenantId == 1)), Times.Once);
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<OrderStatusChangedEvent>(e => e.TenantId == 2)), Times.Once);
    }

    [Fact]
    public async Task ChangeStatusAsync_ValidTransition_PublishesEventOnce()
    {
        // Arrange: Anna buys at ModeHaus …
        var checkout = await _service.CheckoutAsync(Cart((1, 1)));
        var orderId = checkout.Orders.Single().Id;

        // … then the ModeHaus admin takes over
        _currentUser.Setup(u => u.TenantId).Returns(1);
        _currentUser.Setup(u => u.IsInRole(Roles.Customer)).Returns(false);
        _currentUser.Setup(u => u.IsInRole(Roles.TenantAdmin)).Returns(true);

        // Act
        await _service.ChangeStatusAsync(orderId, new ChangeOrderStatusRequest { NewStatus = OrderStatus.Processing });

        // Assert: exactly ONE event from Pending to Processing
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<OrderStatusChangedEvent>(e =>
            e.OldStatus == "Pending" && e.NewStatus == "Processing")), Times.Once);
    }
}