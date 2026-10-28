using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;
using FulfillmentPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;
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
        // entity → DTO, used by Create, GetAll and GetById
        private static TenantResponse ToResponse(Tenant tenant) => new TenantResponse
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Slug = tenant.Slug,
            CreatedAt = tenant.CreatedAt
        };
        public async Task<TenantResponse> CreateAsync(CreateTenantRequest request)
        {
            // ① normalise
            var slug = ShopSlug.Normalize(request.Slug);

            // ② friendly check
            if (await _db.Tenants.AnyAsync(t => t.Slug == slug))
            {
                throw new ConflictException($"The shop link '{slug}' is already taken.");
            }

            // ③ create with all values
            var tenant = new Tenant
            {
                Name = request.Name,          // ⚠️ use YOUR property name (Name or CompanyName)
                Slug = slug,
                Description = request.Description,
                City = request.City,
                BrandColor = request.BrandColor
            };
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();

            return ToResponse(tenant);        // or however your method builds the response today
        }

        public async Task<TenantResponse?> GetByIdAsync(int id)
        {
            var tenant = await _db.Tenants.FindAsync(id);
            if (tenant == null)
            {
                return null;
            }
            return ToResponse(tenant);          
            
        }

        // tenant (database) → ShopResponse (public card)
        private static ShopResponse ToShopResponse(Tenant t) => new ShopResponse
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            Description = t.Description,
            City = t.City,
            BrandColor = t.BrandColor
        };

        // the board: all shops, A → Z
        public async Task<List<ShopResponse>> GetShopsAsync()
        {
            var tenants = await _db.Tenants
                .OrderBy(t => t.Name)
                .ToListAsync();

            return tenants.Select(ToShopResponse).ToList();
        }

        // the info desk: one shop by its link name
        public async Task<ShopResponse> GetShopBySlugAsync(string slug)
        {
            var normalized = ShopSlug.Normalize(slug);       // "SchuhWerk" → "schuhwerk"

            var tenant = await _db.Tenants
                .FirstOrDefaultAsync(t => t.Slug == normalized);

            if (tenant is null)
            {
                throw new NotFoundException($"Shop '{normalized}' not found.");
            }

            return ToShopResponse(tenant);
        }
    }
}
