using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Tenants
{
    public interface ITenantService
    {
        Task<TenantResponse> CreateAsync(CreateTenantRequest request);
        Task<TenantResponse?> GetByIdAsync(int id);
        Task<List<ShopResponse>> GetShopsAsync();
        Task<ShopResponse> GetShopBySlugAsync(string slug);


    }
}
