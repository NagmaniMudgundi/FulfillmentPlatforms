namespace FulfillmentPlatform.Application.Tenants;

public record ShopResponse
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? City { get; init; }
    public string? BrandColor { get; init; }
}