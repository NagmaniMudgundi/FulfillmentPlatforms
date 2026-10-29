using System.Linq.Expressions;
using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;
using FulfillmentPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Application.Catalog;

public class CatalogService : ICatalogService
{
    private const int LowStockLimit = 5;
    private readonly IAppDbContext _db;

    public CatalogService(IAppDbContext db)
    {
        _db = db;
    }

    // the shop window: active products of ALL shops
    private IQueryable<Product> PublicProducts() =>
        _db.Products
            .IgnoreQueryFilters()          // public by design: the same result for everyone
            .AsNoTracking()                // read-only: faster
            .Where(p => p.IsActive);       // hidden products never appear

    public async Task<PagedResult<CatalogProductResponse>> SearchAsync(CatalogQuery q)
    {
        var query = PublicProducts();

        // ① FILTER: only the filters the customer used
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim();
            query = query.Where(p => p.Name.Contains(term) ||
                                     (p.Description != null && p.Description.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(q.Category))
        {
            var category = q.Category.Trim().ToLowerInvariant();
            query = query.Where(p => p.Category.Slug == category);
        }
        if (!string.IsNullOrWhiteSpace(q.Shop))
        {
            var shop = q.Shop.Trim().ToLowerInvariant();
            query = query.Where(p => p.Tenant.Slug == shop);
        }
        if (q.MinPrice.HasValue) query = query.Where(p => p.Price >= q.MinPrice.Value);
        if (q.MaxPrice.HasValue) query = query.Where(p => p.Price <= q.MaxPrice.Value);

        // ② COUNT: for "Page 2 of 7"
        var totalCount = await query.CountAsync();

        // ③ SORT
        query = q.Sort switch
        {
            "price-asc" => query.OrderBy(p => p.Price),
            "price-desc" => query.OrderByDescending(p => p.Price),
            "name" => query.OrderBy(p => p.Name),
            _ => query.OrderByDescending(p => p.Id)      // "newest" = default
        };

        // ④ PAGE + ⑤ SHAPE
        var items = await query
            .Skip((q.Page - 1) * q.PageSize)
            .Take(q.PageSize)
            .Select(ToCard)
            .ToListAsync();

        return new PagedResult<CatalogProductResponse>
        {
            Items = items,
            Page = q.Page,
            PageSize = q.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task<CatalogProductResponse> GetProductAsync(int id)
    {
        var product = await PublicProducts()
            .Where(p => p.Id == id)
            .Select(ToCard)
            .FirstOrDefaultAsync();

        return product ?? throw new NotFoundException($"Product {id} not found.");
    }

    public async Task<List<CategoryResponse>> GetCategoriesAsync()
    {
        return await _db.Categories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync();
    }

    // product → public card. An "expression", so EF turns it into SQL and loads only these columns
    private static readonly Expression<Func<Product, CatalogProductResponse>> ToCard =
        p => new CatalogProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            CategoryName = p.Category.Name,
            CategorySlug = p.Category.Slug,
            ShopName = p.Tenant.Name,
            ShopSlug = p.Tenant.Slug,
            ShopBrandColor = p.Tenant.BrandColor,
            InStock = p.StockQuantity > 0,
            OnlyLeft = p.StockQuantity > 0 && p.StockQuantity <= LowStockLimit
                ? p.StockQuantity
                : (int?)null
        };
}