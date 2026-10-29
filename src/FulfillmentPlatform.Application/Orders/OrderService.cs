using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;
using FulfillmentPlatform.Application.Common.Messaging;
using FulfillmentPlatform.Application.Orders.Events;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Domain.Enums;
using FulfillmentPlatform.Domain.Rules;
using Microsoft.EntityFrameworkCore;
using System.Text;

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

    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request)
    {
        // ① who is ordering?
        var userId = _currentUser.UserId;

        // ② no duplicate products in one cart
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        if (productIds.Distinct().Count() != productIds.Count)
        {
            throw new BadRequestException("Each product may appear only once in the cart.");
        }

        // ③ load all products in ONE query, with their shop (customers see all shops)
        var products = await _db.Products
            .Include(p => p.Tenant)
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToDictionaryAsync(p => p.Id);

        if (products.Count != productIds.Count)
        {
            throw new NotFoundException("One or more products were not found or are no longer available.");
        }

        // ④ one checkout id for all orders of this cart
        var checkoutId = Guid.NewGuid();
        var orders = new List<Order>();

        // ⑤ SPLIT: group the cart items by shop → one order per shop
        var itemsByShop = request.Items.GroupBy(i => products[i.ProductId].TenantId);

        foreach (var shopItems in itemsByShop)
        {
            var order = new Order
            {
                TenantId = shopItems.Key,                       // the shop comes from the PRODUCT
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

                product.StockQuantity -= item.Quantity;          // reduce stock (RowVersion protects it)

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

        // ⑥ save ALL orders at once: one SaveChanges = one transaction (all or nothing)
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("The stock of a product changed while you were ordering. Please try again.");
        }

        // ⑦ tell each shop about ITS order (status: nothing → Pending)
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

        // ⑧ the receipt
        return new CheckoutResponse
        {
            CheckoutId = checkoutId,
            Orders = orders.Select(ToResponse).ToList(),
            GrandTotal = orders.Sum(o => o.TotalAmount)
        };
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
        Status = order.Status.ToString(),
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
        CheckoutId = order.CheckoutId,
        ShopName = order.Tenant.Name,
        ShopSlug = order.Tenant.Slug,
        // TODO 8: Items = order.OrderItems.Select(i => new OrderItemResponse { … ProductName = i.Product.Name … }).ToList(),
        Items = order.OrderItems.Select(i => new OrderItemResponse
        {
            ProductId = i.ProductId,
            ProductName = i.Product.Name,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList(),
        // TODO 9: StatusHistory = order.StatusHistory.Select(h => new OrderStatusHistoryResponse { … }).ToList()
        StatusHistory = order.StatusHistory
        .OrderBy(h => h.ChangedAt)
        .Select(h => new OrderStatusHistoryResponse
        {
            Status = h.Status.ToString(),
            ChangedAt = h.ChangedAt,
            Note = h.Note
        }).ToList()

    };

    // Shared by manual cancel and automatic cancel
    private static void CancelOrder(Order order, string note)
    {
        // TODO 1: return the stock for each item (you wrote this loop in CancelAsync)
        foreach (var item in order.OrderItems)
        {
            item.Product.StockQuantity += item.Quantity;
        }
        // TODO 2: set the status to Cancelled
        order.Status = OrderStatus.Cancelled;
        // TODO 3: add the history row with the note
        OrderStatusHistory history = new OrderStatusHistory
        {
            Status = OrderStatus.Cancelled,
            Note = note
        };
        order.StatusHistory.Add(history);
    }
    // Orders the current caller may see, with everything needed for the response
    private IQueryable<Order> VisibleOrders()
    {
        IQueryable<Order> query = _db.Orders
        .Include(o => o.Tenant)                                   // ⬅️ NEW: for ShopName
        .Include(o => o.OrderItems).ThenInclude(i => i.Product)
        .Include(o => o.StatusHistory);
        //In OrderService.VisibleOrders(), don’t delete the customer check (o.UserId == userId). It now does the same as the filter. That’s on purpose: two layers of protection (defence in depth). If someone breaks the filter one day, the service still protects the data.
        // Customers only see their own orders (TenantAdmins see all of their tenant)
        if (_currentUser.IsInRole(Roles.Customer))
        {
            var userId = _currentUser.UserId;
            query = query.Where(o => o.UserId == userId);
        }

        return query;
    }
    //GetAllAsync	VisibleOrders() → newest first: .OrderByDescending(o => o.CreatedAt) → .ToListAsync() → map each with ToResponse → return the list
    public async Task<List<OrderResponse>> GetAllAsync()
    {
       var orderresponse= await VisibleOrders().
            OrderByDescending(o => o.CreatedAt).
            ToListAsync();
        //The fix: Select + your existing ToResponse
        //You already have a method that converts one order:
        //ToResponse(order).Select runs it for each order in the list:
        return orderresponse.Select(ToResponse).ToList();

    }
    public async Task<OrderResponse> GetByIdAsync(int id)
    {
        var order = await VisibleOrders().FirstOrDefaultAsync(o => o.Id == id);
        if(order is null)
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

        var oldStatus = order.Status;                          // ① NEW: note down the old status ("Processing")

        order.Status = request.NewStatus;                      // ② change it ("Shipped")
        order.StatusHistory.Add(new OrderStatusHistory
        {
            Status = request.NewStatus,
            Note = request.Note
        });

        await _db.SaveChangesAsync();                          // ③ save to the database

        await _eventPublisher.PublishAsync(                    // ④ NEW: tell RabbitMQ
            CreateStatusChangedEvent(order, oldStatus, request.Note));

        return ToResponse(order);
    }

    public async Task<OrderResponse> CancelAsync(int id, CancelOrderRequest request)
    {
        // load the order (customers only see their own; other tenants are filtered)
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

        // is cancelling allowed from the current status?
        if (!OrderStatusTransitions.CanTransition(order.Status, OrderStatus.Cancelled))
        {
            throw new ConflictException($"An order with status {order.Status} cannot be cancelled.");
        }

        var note = request.Reason ?? (isCustomer ? "Cancelled by customer" : "Cancelled by shop");

        var oldStatus = order.Status;                              // ← NEW ① remember the old status

        CancelOrder(order, note);                                  // ② stock back, Cancelled, history

        await _db.SaveChangesAsync();                              // ③ save

        await _eventPublisher.PublishAsync(                        // ← NEW ④ tell RabbitMQ
            CreateStatusChangedEvent(order, oldStatus, note));

        return ToResponse(order);
    }
    public async Task CancelStaleOrdersAsync(int olderThanDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);      // e.g. "3 days ago"
        var note = $"Automatically cancelled: not processed within {olderThanDays} days.";

        var staleOrders = await _db.Orders
            .IgnoreQueryFilters()                                  // a job has no tenant: look at ALL tenants
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Include(o => o.StatusHistory)
            .Where(o => o.Status == OrderStatus.Pending && o.CreatedAt < cutoff)
            .ToListAsync();

        foreach (var order in staleOrders)
        {
            CancelOrder(order, note);                              // ② stock back, Cancelled, history
        }

        await _db.SaveChangesAsync();                              // ③ save all at once

        foreach (var order in staleOrders)                         // ← NEW ④ tell RabbitMQ, one message per order
        {
            await _eventPublisher.PublishAsync(
                CreateStatusChangedEvent(order, OrderStatus.Pending, note));
        }
    }
}