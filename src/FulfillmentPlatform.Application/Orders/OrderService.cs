using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;
using FulfillmentPlatform.Domain.Rules;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace FulfillmentPlatform.Application.Orders;

public class OrderService : IOrderService
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OrderService(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<OrderResponse> PlaceOrderAsync(CreateOrderRequest request)
    {
        // ① who is ordering?
        var tenantId = _currentUser.TenantId;
        var userId = _currentUser.UserId;

        // ② no duplicate products in one order
        var productIds = request.Items.Select(i => i.ProductId).ToList();
        if (productIds.Distinct().Count() != productIds.Count)
        {
            throw new BadRequestException("Each product may appear only once per order.");
        }

        // ③ load all products in ONE query (tenant filter is applied automatically)
        // TODO 1: var products = await _db.Products. ... .ToListAsync();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();
        // ④ were all products found?
        // TODO 2: if products.Count != productIds.Count → throw NotFoundException("One or more products were not found.")
        if (products.Count != productIds.Count)
        {
            throw new NotFoundException("One or more products were not found.");
        }

        // ⑤ build the order
        var order = new Order
        {
            TenantId = tenantId,
            UserId = userId,
            Status = OrderStatus.Pending
        };
        decimal total = 0;

        foreach (var item in request.Items)
        {
            var product = products.First(p => p.Id == item.ProductId);

            // TODO 3: if product.StockQuantity < item.Quantity
            //         → throw ConflictException($"Not enough stock for '{product.Name}'. Available: …, requested: ….")
            if (product.StockQuantity < item.Quantity)
            {
                throw new ConflictException($"Not enough stock for '{product.Name}'. Available: {product.StockQuantity}, requested: {item.Quantity}.");
            }
            // TODO 4: reduce the stock
            product.StockQuantity -= item.Quantity;
            // TODO 5: add a new OrderItem to order.OrderItems:
            //         ProductId, Product (the navigation!), Quantity, UnitPrice = product.Price  ← the snapshot
            var orderItem = new OrderItem
            {
                ProductId = product.Id,
                Product = product,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            };
            order.OrderItems.Add(orderItem);
            // TODO 6: total += item.Quantity * product.Price;
            total += item.Quantity * product.Price;
        }

        // ⑥ total + first history row
        order.TotalAmount = total;
        // TODO 7: add a new OrderStatusHistory to order.StatusHistory with Status = OrderStatus.Pending
        var orderStatusHistory = new OrderStatusHistory
        {

            Status = OrderStatus.Pending
        };
        order.StatusHistory.Add(orderStatusHistory);
        // ⑦ save EVERYTHING at once (one SaveChanges = one transaction)
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // ⑧ the receipt
        return ToResponse(order);
    }

    private static OrderResponse ToResponse(Order order) => new OrderResponse
    {
        Id = order.Id,
        Status = order.Status.ToString(),
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
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
    // Orders the current caller may see, with everything needed for the response
    private IQueryable<Order> VisibleOrders()
    {
        IQueryable<Order> query = _db.Orders
            .Include(o => o.OrderItems).ThenInclude(i => i.Product)
            .Include(o => o.StatusHistory);

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
        // cancelling has its own endpoint (it must return the stock)
        if (request.NewStatus == OrderStatus.Cancelled)
        {
            throw new BadRequestException("Use the cancel endpoint to cancel an order.");
        }

        // TODO 1: load the order with VisibleOrders() + FirstOrDefaultAsync
        //         not found → NotFoundException
        var order = await VisibleOrders().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            throw new NotFoundException($"Order {id} not found.");
        }

        // TODO 2: is the change allowed?
        //         if (!OrderStatusTransitions.CanTransition(order.Status, request.NewStatus))
        //         → ConflictException($"Cannot change status from {order.Status} to {request.NewStatus}.")
        if (!OrderStatusTransitions.CanTransition(order.Status, request.NewStatus))
        {
            throw new ConflictException($"Cannot change status from {order.Status} to {request.NewStatus}.");
        }
        // TODO 3: set the new status on the order
        order.Status = request.NewStatus;
        // TODO 4: add a new OrderStatusHistory to order.StatusHistory
        //         (Status = request.NewStatus, Note = request.Note)
        OrderStatusHistory history = new OrderStatusHistory
        {
            Status = request.NewStatus,
            Note = request.Note
        };
        order.StatusHistory.Add(history);
        await _db.SaveChangesAsync();
        return ToResponse(order);
    }

    public async Task<OrderResponse> CancelAsync(int id, CancelOrderRequest request)
    {
        // ① load the order (customers only see their own; other tenants are filtered)
        // TODO 1: VisibleOrders() + FirstOrDefaultAsync → null → NotFoundException
        var order = await VisibleOrders().FirstOrDefaultAsync(o => o.Id == id);
        if (order is null)
        {
            throw new NotFoundException($"Order {id} not found.");
        }
        var isCustomer = _currentUser.IsInRole(Roles.Customer);

        // ② customers may only cancel while Pending
        if (isCustomer && order.Status != OrderStatus.Pending)
        {
            throw new ConflictException("You can only cancel orders that are still pending. Please contact the shop.");
        }

        // ③ is cancelling allowed from the current status?
        // TODO 2: if (!OrderStatusTransitions.CanTransition(order.Status, OrderStatus.Cancelled))
        //         → ConflictException($"An order with status {order.Status} cannot be cancelled.")
        if(!OrderStatusTransitions.CanTransition(order.Status, OrderStatus.Cancelled))
        {
            throw new ConflictException($"An order with status {order.Status} cannot be cancelled.");
        }
        // ④ return the stock
        // TODO 3: foreach item in order.OrderItems → item.Product.StockQuantity += item.Quantity;
        foreach(var item in order.OrderItems)
        {
            item.Product.StockQuantity += item.Quantity;
        }
        // ⑤ new status
        // TODO 4: order.Status = OrderStatus.Cancelled;
        order.Status = OrderStatus.Cancelled;

        // ⑥ history row: use the reason if given, otherwise say who cancelled
        var note = request.Reason ?? (isCustomer ? "Cancelled by customer" : "Cancelled by shop");
        // TODO 5: add a new OrderStatusHistory (Status = Cancelled, Note = note) to order.StatusHistory
        OrderStatusHistory history = new OrderStatusHistory
        {
            Status = OrderStatus.Cancelled,
            Note = note
        };
        order.StatusHistory.Add(history);
        // ⑦ save stock + status + history together
        await _db.SaveChangesAsync();
        return ToResponse(order);
    }
}