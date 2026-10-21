using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Tenants
{
    public class TenantService : ITenantService
    {
        private readonly IAppDbContext _db;
        public TenantService(IAppDbContext db)
        {
            _db = db;
        }
        public async Task<TenantResponse> CreateAsync(CreateTenantRequest request)
        {
            var tenant= new Tenant
            {
                Name = request.Name
            };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
            var tenantResponse = new TenantResponse
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt
            };
            return tenantResponse;
        }

        public async Task<TenantResponse?> GetByIdAsync(int id)
        {
            var tenant = await _db.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return null;
            }
            var tenantResponse = new TenantResponse
            {
                Id = tenant.Id,
                Name = tenant.Name,
                CreatedAt = tenant.CreatedAt
            };
            return tenantResponse;
        }
    }
}
