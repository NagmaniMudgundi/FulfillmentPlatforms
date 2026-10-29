using System.ComponentModel.DataAnnotations;

namespace FulfillmentPlatform.Application.Orders;

// what React sends when Lisa clicks "Place order"
public class CheckoutRequest
{
    [Required]
    [MinLength(1, ErrorMessage = "Your cart is empty.")]
    [MaxLength(50, ErrorMessage = "A cart may contain at most 50 different products.")]
    public List<CheckoutItemRequest> Items { get; set; } = new();

    [Required, StringLength(200)]
    public string Street { get; set; } = default!;          // "Lindachstraße 26"

    [Required]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Please enter a 5-digit German postal code.")]
    public string PostalCode { get; set; } = default!;      // "72764"

    [Required, StringLength(100)]
    public string City { get; set; } = default!;            // "Reutlingen"
}

public class CheckoutItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100.")]
    public int Quantity { get; set; }
}