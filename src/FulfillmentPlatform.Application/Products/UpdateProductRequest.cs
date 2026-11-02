using System.ComponentModel.DataAnnotations;

namespace FulfillmentPlatform.Application.Products;

// what an admin may change; the SKU stays fixed (it's the product's identifier in the shop)
public record UpdateProductRequest
{
    [Required, MaxLength(200)]
    public string Name { get; init; } = string.Empty;

    [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0.")]
    public decimal Price { get; init; }

    [Range(0, 100000)]
    public int StockQuantity { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
    public int CategoryId { get; init; }

    [MaxLength(1000)]
    public string? Description { get; init; }

    [MaxLength(500), Url]
    public string? ImageUrl { get; init; }

    public bool IsActive { get; init; } = true;
}