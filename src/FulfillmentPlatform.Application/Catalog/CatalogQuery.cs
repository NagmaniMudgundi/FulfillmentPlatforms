using System.ComponentModel.DataAnnotations;

namespace FulfillmentPlatform.Application.Catalog;

// what a customer may ask the shop window for (comes from the URL: ?search=boot&page=2)
public class CatalogQuery
{
    [MaxLength(100)]
    public string? Search { get; set; }        // "boot"

    [MaxLength(50)]
    public string? Category { get; set; }      // category slug: "shoes"

    [MaxLength(50)]
    public string? Shop { get; set; }          // shop slug: "modehaus"

    [Range(0, 1000000)]
    public decimal? MinPrice { get; set; }

    [Range(0, 1000000)]
    public decimal? MaxPrice { get; set; }

    public string? Sort { get; set; }          // "newest" (default), "price-asc", "price-desc", "name"

    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 50)]
    public int PageSize { get; set; } = 20;    // max 50 protects the database
}