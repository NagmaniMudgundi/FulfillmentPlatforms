using System.ComponentModel.DataAnnotations;
using FulfillmentPlatform.Domain.Enums;

namespace FulfillmentPlatform.Application.Orders;

// GET /api/orders?status=Pending&page=1&pageSize=20
public class OrderQuery
{
    public OrderStatus? Status { get; set; }       // empty = all statuses

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}