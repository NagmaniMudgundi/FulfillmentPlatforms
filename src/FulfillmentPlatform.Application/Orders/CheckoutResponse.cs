namespace FulfillmentPlatform.Application.Orders;

// the receipt: one checkout, one order per shop
public record CheckoutResponse
{
    public Guid CheckoutId { get; init; }
    public List<OrderResponse> Orders { get; init; } = new();
    public decimal GrandTotal { get; init; }
}