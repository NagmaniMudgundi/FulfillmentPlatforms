namespace FulfillmentPlatform.Application.Tenants;

public static class ShopSlug
{
    // " ModeHaus " → "modehaus"
    public static string Normalize(string slug) => slug.Trim().ToLowerInvariant();
}