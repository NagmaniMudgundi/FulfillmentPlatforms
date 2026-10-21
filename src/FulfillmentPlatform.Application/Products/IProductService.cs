using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Products
{
    public interface IProductService
    {
        //Task<ProductResponse> CreateAsync(int tenantId,CreateProductRequest request);
        //Task<ProductResponse> GetByIdAsync(int tenantId,int id);
        //Task<IEnumerable<ProductResponse>> GetAllByTenantIdAsync(int tenantId);
        Task<ProductResponse> CreateAsync(CreateProductRequest request);
        Task<ProductResponse> GetByIdAsync(int id);
        Task<IEnumerable<ProductResponse>> GetAllByTenantIdAsync();
    }
}
