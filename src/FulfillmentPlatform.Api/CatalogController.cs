using FulfillmentPlatform.Application.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentPlatform.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[AllowAnonymous]                                   // the shop window is public
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalog;

    public CatalogController(ICatalogService catalog)
    {
        _catalog = catalog;
    }

    [HttpGet("products")]                          // GET /api/catalog/products?search=boot&page=1
    public async Task<IActionResult> Search([FromQuery] CatalogQuery query)
        => Ok(await _catalog.SearchAsync(query));

    [HttpGet("products/{id:int}")]                 // GET /api/catalog/products/12
    public async Task<IActionResult> GetProduct(int id)
        => Ok(await _catalog.GetProductAsync(id));

    [HttpGet("categories")]                        // GET /api/catalog/categories
    public async Task<IActionResult> GetCategories()
        => Ok(await _catalog.GetCategoriesAsync());
}