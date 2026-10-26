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

    // xUnit creates a NEW instance of this class for EVERY test
    // → every test gets a fresh, empty database (tests can't influence each other)
    public OrderServiceTests()
    {
        // ARRANGE (shared): the fake logged-in user is Anna, a Customer of tenant 1
        _currentUser.Setup(u => u.TenantId).Returns(1);
        _currentUser.Setup(u => u.UserId).Returns("anna-id");
        _currentUser.Setup(u => u.IsInRole(Roles.Customer)).Returns(true);

        // a brand-new in-memory database (unique name per test)
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options, _currentUser.Object);

        // test data: one shop, one product (price 500, stock 10)
        _db.Tenants.Add(new Tenant { Id = 1, Name = "ModeHaus" });
        _db.Products.Add(new Product { Id = 1, TenantId = 1, Name = "TShirt", Sku = "TS-1", Price = 500, StockQuantity = 10 });
        _db.SaveChanges();

        // the class we test, with fakes instead of the real things
        _service = new OrderService(_db, _currentUser.Object, _eventPublisher.Object);
    }

    // ── Test 1 (complete example) ────────────────────────────────────
    [Fact]
    public async Task PlaceOrderAsync_ValidOrder_ReducesStockAndCalculatesTotal()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            Items = new() { new CreateOrderItemRequest { ProductId = 1, Quantity = 2 } }
        };

        // Act
        var result = await _service.PlaceOrderAsync(request);

        // Assert
        Assert.Equal(1000, result.TotalAmount);                       // 2 × 500
        Assert.Equal(500, result.Items.Single().UnitPrice);           // price snapshot
        Assert.Equal("Pending", result.Status);
        Assert.Single(result.StatusHistory);                          // first history row

        var product = await _db.Products.FindAsync(1);
        Assert.Equal(8, product!.StockQuantity);                      // 10 − 2
    }

    // ── Test 2 (you write) ───────────────────────────────────────────
    [Fact]
    public async Task PlaceOrderAsync_NotEnoughStock_ThrowsConflictAndKeepsStock()
    {
        // TODO Arrange: order 11 pieces of product 1 (only 10 in stock)
        var request = new CreateOrderRequest
        {
            Items = new() { new CreateOrderItemRequest { ProductId = 1, Quantity = 11 } }
        };

        // TODO Act + Assert:
        //   await Assert.ThrowsAsync<ConflictException>(() => _service.PlaceOrderAsync(request));
        await Assert.ThrowsAsync<ConflictException>(async () => await _service.PlaceOrderAsync(request));

        // TODO Assert: the product's stock is STILL 10
        var product = await _db.Products.FindAsync(1);
        Assert.Equal(10, product!.StockQuantity);
    }

    // ── Test 3 (you write) ───────────────────────────────────────────
    [Fact]
    public async Task PlaceOrderAsync_DuplicateProduct_ThrowsBadRequest()
    {
        // TODO Arrange: the same ProductId twice in Items
        var request = new CreateOrderRequest
        {
            Items = new()
            {
                new CreateOrderItemRequest { ProductId = 1, Quantity = 1 },
                new CreateOrderItemRequest { ProductId = 1, Quantity = 2 }
            }
        };
        // TODO Act + Assert: ThrowsAsync<BadRequestException>
        await Assert.ThrowsAsync<BadRequestException>(async () => await _service.PlaceOrderAsync(request));
    }
    [Fact]
    public async Task ChangeStatusAsync_ValidTransition_PublishesEventOnce()
    {
        // Arrange: place an order as Anna, then act as an admin
        var order = await _service.PlaceOrderAsync(new CreateOrderRequest
        {
            Items = new() { new CreateOrderItemRequest { ProductId = 1, Quantity = 1 } }
        });

        _currentUser.Setup(u => u.IsInRole(Roles.Customer)).Returns(false);      // now: not a customer
        _currentUser.Setup(u => u.IsInRole(Roles.TenantAdmin)).Returns(true);    // but an admin

        // Act
        await _service.ChangeStatusAsync(order.Id, new ChangeOrderStatusRequest { NewStatus = OrderStatus.Processing });

        // Assert: exactly ONE event, from Pending to Processing
        _eventPublisher.Verify(p => p.PublishAsync(It.Is<OrderStatusChangedEvent>(e =>
            e.OldStatus == "Pending" && e.NewStatus == "Processing")), Times.Once);
    }
}