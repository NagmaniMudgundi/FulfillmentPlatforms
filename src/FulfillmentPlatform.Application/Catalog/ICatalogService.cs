using FulfillmentPlatform.Application.Common;

namespace FulfillmentPlatform.Application.Catalog;

public interface ICatalogService
{
    Task<PagedResult<CatalogProductResponse>> SearchAsync(CatalogQuery query);
    Task<CatalogProductResponse> GetProductAsync(int id);
    Task<List<CategoryResponse>> GetCategoriesAsync();
}