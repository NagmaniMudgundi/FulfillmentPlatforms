using FulfillmentPlatform.Application.Tenants;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    
    public class TenantsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public TenantsController(AppDbContext db)
        {
            _db = db;
        }
        [HttpPost]
        public async Task<ActionResult<TenantResponse>> Create(CreateTenantRequest request)
        {
            var tenant = new Tenant { Name = request.Name };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
            //SQL Server sends the new Id back, and EF Core writes it into your tenant object
            //It copies three values from the entity into the DTO, in the same order as your record

            var response = new TenantResponse(tenant.Id, tenant.Name, tenant.CreatedAt);

            return CreatedAtAction(nameof(GetById), new { id = tenant.Id }, response);

        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<TenantResponse>> GetById(int id)
        {
            var tenant = await _db.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }
            var response = new TenantResponse(tenant.Id, tenant.Name, tenant.CreatedAt);
            return Ok(response);
        }
    }
}
