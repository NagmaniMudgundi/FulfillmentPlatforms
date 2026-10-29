namespace FulfillmentPlatform.Application.Catalog;

// the PUBLIC product card: no SKU, no TenantId, no exact stock
public record CatalogProductResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string? ImageUrl { get; init; }

    public string CategoryName { get; init; } = string.Empty;
    public string CategorySlug { get; init; } = string.Empty;

    // "sold by …"
    public string ShopName { get; init; } = string.Empty;
    public string ShopSlug { get; init; } = string.Empty;
    public string? ShopBrandColor { get; init; }

    // stock as a hint, not the exact number
    public bool InStock { get; init; }
    public int? OnlyLeft { get; init; }        // e.g. 3 → "Only 3 left"; null = plenty
}