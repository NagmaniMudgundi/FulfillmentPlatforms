using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;
using FulfillmentPlatform.Application.Common.Messaging;
using FulfillmentPlatform.Application.Orders.Events;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Domain.Enums;
using FulfillmentPlatform.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Application.Orders;

public class OrderService : IOrderService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IEventPublisher _eventPublisher;

    public OrderService(IAppDbContext db, ICurrentUserService currentUser, IEventPublisher eventPublisher)
    {
        _db = db;
        _currentUser = currentUser;
        _eventPublisher = eventPublisher;
    }

    // One cart → one order per shop, saved in one transaction
    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request)
    {
        var userId = _currentUser.UserId;

        // no duplicate products in one cart
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        if (productIds.Distinct().Count() != productIds.Count)
        {
            throw new BadRequestException("Each product may appear only once in the cart.");
        }

        // load all products in one query, with their shop
        var products = await _db.Products
            .Include(p => p.Tenant)
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
        {
            throw new NotFoundException("One or more products were not found or are no longer available.");
        }

        var checkoutId = Guid.NewGuid();
        var orders = new List<Order>();

        // split: group the cart items by shop
        var itemsByShop = request.Items.GroupBy(i => products[i.ProductId].TenantId);

        foreach (var shopItems in itemsByShop)
        {
            var order = new Order
            {
                TenantId = shopItems.Key,                       // the shop comes from the product
                Tenant = products[shopItems.First().ProductId].Tenant,
                UserId = userId,
                CheckoutId = checkoutId,
                Status = OrderStatus.Pending,
                ShippingStreet = request.Street.Trim(),
                ShippingPostalCode = request.PostalCode,
                ShippingCity = request.City.Trim()
            };

            decimal total = 0;
            foreach (var item in shopItems)
            {
                var product = products[item.ProductId];

                if (product.StockQuantity < item.Quantity)
                {
                    throw new ConflictException(
                        $"Not enough stock for '{product.Name}'. Available: {product.StockQuantity}, requested: {item.Quantity}.");
                }

                product.StockQuantity -= item.Quantity;          // protected by RowVersion

                order.OrderItems.Add(new OrderItem
                {
                    ProductId = product.Id,
                    Product = product,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price                     // price snapshot
                });
                total += item.Quantity * product.Price;
            }

            order.TotalAmount = total;
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = OrderStatus.Pending,
                Note = "Order placed"
            });

            orders.Add(order);
            _db.Orders.Add(order);
        }

        // one SaveChanges = one transaction: all orders or none
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The stock of a product changed while you were ordering. Please try again.");
        }

        // notify each shop about its own order (status: none → Pending)
        foreach (var order in orders)
        {
            await _eventPublisher.PublishAsync(new OrderStatusChangedEvent
            {
                OrderId = order.Id,
                TenantId = order.TenantId,
                UserId = order.UserId,
                OldStatus = "None",
                NewStatus = order.Status.ToString(),
                Note = "Order placed",
                ChangedAt = DateTime.UtcNow
            });
        }

        return new CheckoutResponse
        {
            CheckoutId = checkoutId,
            Orders = orders.Select(ToResponse).ToList(),
            GrandTotal = orders.Sum(o => o.TotalAmount)
        };
    }

    // One page of orders the caller may see (admin: their shop · customer: their own)
    public async Task<PagedResult<OrderResponse>> GetAllAsync(OrderQuery q)
    {
        var query = VisibleOrders();

        if (q.Status.HasValue)
        {
            query = query.Where(o => o.Status == q.Status.Value);
        }

        var totalCount = await query.CountAsync();

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .ToListAsync();

        return new PagedResult<OrderResponse>
        {
            Items = orders.Select(ToResponse).ToList(),
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<OrderResponse> GetByIdAsync(int id)
    {
        var order = await VisibleOrders().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            throw new NotFoundException($"Order {id} not found.");
        }
        return ToResponse(order);
    }

    public async Task<OrderResponse> ChangeStatusAsync(int id, ChangeOrderStatusRequest request)
    {
        if (request.NewStatus == OrderStatus.Cancelled)
        {
            throw new BadRequestException("Use the cancel endpoint to cancel an order.");
        }

        var order = await VisibleOrders().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            throw new NotFoundException($"Order {id} not found.");
        }

        if (!OrderStatusTransitions.CanTransition(order.Status, request.NewStatus))
        {
            throw new ConflictException($"Cannot change status from {order.Status} to {request.NewStatus}.");
        }

        var oldStatus = order.Status;

        order.Status = request.NewStatus;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = request.NewStatus,
            Note = request.Note
        });

        await _db.SaveChangesAsync();

        await _eventPublisher.PublishAsync(CreateStatusChangedEvent(order, oldStatus, request.Note));

        return ToResponse(order);
    }

    public async Task<OrderResponse> CancelAsync(int id, CancelOrderRequest request)
    {
        // customers only find their own orders; admins only their shop's
        var order = await VisibleOrders().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            throw new NotFoundException($"Order {id} not found.");
        }

        var isCustomer = _currentUser.IsInRole(Roles.Customer);

        // customers may only cancel while Pending
        if (isCustomer && order.Status != OrderStatus.Pending)
        {
            throw new ConflictException("You can only cancel orders that are still pending. Please contact the shop.");
        }

        if (!OrderStatusTransitions.CanTransition(order.Status, OrderStatus.Cancelled))
        {
            throw new ConflictException($"An order with status {order.Status} cannot be cancelled.");
        }

        var note = request.Reason ?? (isCustomer ? "Cancelled by customer" : "Cancelled by shop");
        var oldStatus = order.Status;

        CancelOrder(order, note);
        await _db.SaveChangesAsync();

        await _eventPublisher.PublishAsync(CreateStatusChangedEvent(order, oldStatus, note));

        return ToResponse(order);
    }

    // Hangfire job: cancel orders that stayed Pending too long (all shops)
    public async Task CancelStaleOrdersAsync(int olderThanDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        var note = $"Automatically cancelled: not processed within {olderThanDays} days.";

        var staleOrders = await _db.Orders
            .IgnoreQueryFilters()                                  // a job has no user: look at all shops
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Include(o => o.StatusHistory)
            .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < cutoff)
            .ToListAsync();

        foreach (var order in staleOrders)
        {
            CancelOrder(order, note);
        }

        await _db.SaveChangesAsync();

        foreach (var order in staleOrders)
        {
            await _eventPublisher.PublishAsync(CreateStatusChangedEvent(order, OrderStatus.Pending, note));
        }
    }

    // Orders the current caller may see, with everything needed for the response
    private IQueryable<Order> VisibleOrders()
    {
        IQueryable<Order> query = _db.Orders
            .Include(o => o.Tenant)
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Include(o => o.StatusHistory);

        // second safety net: customers only see their own orders (the query filter does this too)
        if (_currentUser.IsInRole(Roles.Customer))
        {
            var userId = _currentUser.UserId;
            query = query.Where(o => o.UserId == userId);
        }

        return query;
    }

    // Shared by manual cancel and automatic cancel: stock back, status, history
    private static void CancelOrder(Order order, string note)
    {
        foreach (var item in order.OrderItems)
        {
            item.Product.StockQuantity += item.Quantity;
        }

        order.Status = OrderStatus.Cancelled;
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = OrderStatus.Cancelled,
            Note = note
        });
    }

    private static OrderStatusChangedEvent CreateStatusChangedEvent(Order order, OrderStatus oldStatus, string? note) => new()
    {
        OrderId = order.Id,
        TenantId = order.TenantId,
        UserId = order.UserId,
        OldStatus = oldStatus.ToString(),
        NewStatus = order.Status.ToString(),
        Note = note,
        ChangedAt = DateTime.UtcNow
    };

    private static OrderResponse ToResponse(Order order) => new OrderResponse
    {
        Id = order.Id,
        CheckoutId = order.CheckoutId,
        ShopName = order.Tenant.Name,
        ShopSlug = order.Tenant.Slug,
        Status = order.Status.ToString(),
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
        Items = order.OrderItems.Select(i => new OrderItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        StatusHistory = order.StatusHistory
            .OrderBy(h => h.ChangedAt)
            .Select(h => new OrderStatusHistoryResponse
            {
                Status = h.Status.ToString(),
                ChangedAt = h.ChangedAt,
                Note = h.Note
            }).ToList()
    };
}