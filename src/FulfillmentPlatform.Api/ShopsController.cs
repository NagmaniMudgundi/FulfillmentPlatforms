using FulfillmentPlatform.Application.Tenants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentPlatform.Api.Controllers;

[ApiController]
[Route("api/shops")]
[AllowAnonymous]                      // public: visitors don't need to log in
public class ShopsController : ControllerBase
{
    private readonly ITenantService _tenantService;

    public ShopsController(ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    [HttpGet]                         // GET /api/shops
    public async Task<IActionResult> GetAll()
        => Ok(await _tenantService.GetShopsAsync());

    [HttpGet("{slug}")]               // GET /api/shops/schuhwerk
    public async Task<IActionResult> GetBySlug(string slug)
        => Ok(await _tenantService.GetShopBySlugAsync(slug));
}