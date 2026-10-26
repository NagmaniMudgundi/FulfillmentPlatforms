using FulfillmentPlatform.Domain.Enums;
using FulfillmentPlatform.Domain.Rules;

namespace FulfillmentPlatform.UnitTests.Domain;

public class OrderStatusTransitionsTests
{
    // ── 1. A single test ─────────────────────────────────────────────
    [Fact]
    public void CanTransition_PendingToProcessing_ReturnsTrue()
    {
        // Arrange
        var from = OrderStatus.Pending;
        var to = OrderStatus.Processing;

        // Act
        var result = OrderStatusTransitions.CanTransition(from, to);

        // Assert
        Assert.True(result);
    }

    // ── 2. ALL allowed transitions, one Theory ───────────────────────
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Processing)]
    [InlineData(OrderStatus.Processing, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Processing, OrderStatus.Cancelled)]
    public void CanTransition_AllowedTransition_ReturnsTrue(OrderStatus from, OrderStatus to)
    {
        // Act
        var result = OrderStatusTransitions.CanTransition(from, to);

        // Assert
        Assert.True(result);
    }

    // ── 3. Forbidden transitions, one Theory ─────────────────────────
    [Theory]
    [InlineData(OrderStatus.Delivered, OrderStatus.Pending)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Processing)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    public void CanTransition_ForbiddenTransition_ReturnsFalse(OrderStatus from, OrderStatus to)
    {
        // Act
        var result = OrderStatusTransitions.CanTransition(from, to);

        // Assert
        Assert.False(result);
    }
}