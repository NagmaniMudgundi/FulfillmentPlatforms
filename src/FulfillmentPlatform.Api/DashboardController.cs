using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentPlatform.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = Roles.TenantAdmin)]                 // shop admins only
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard)
    {
        _dashboard = dashboard;
    }

    [HttpGet]                                          // GET /api/admin/dashboard
    public async Task<ActionResult<DashboardResponse>> Get()
        => Ok(await _dashboard.GetAsync());
}