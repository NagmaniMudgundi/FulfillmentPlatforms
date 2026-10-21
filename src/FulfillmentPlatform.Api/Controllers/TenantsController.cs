using FulfillmentPlatform.Application.Tenants;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class TenantsController : ControllerBase
    {
        //private readonly AppDbContext _db;

        private readonly ITenantService _tenantService;
        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }
        //[HttpPost]
        //public async Task<ActionResult<TenantResponse>> Create(CreateTenantRequest request)
        //{
        //    var result = await _tenantService.CreateAsync(request);

        //    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        //}

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TenantResponse>> GetById(int id)
        {
            var result = await _tenantService.GetByIdAsync(id);
            if (result is null)
            {
              return NotFound();
            }
            return Ok(result);
        }
    }
}
