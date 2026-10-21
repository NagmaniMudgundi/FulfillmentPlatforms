using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Application.Common.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Products
{
    public class ProductService: IProductService
    {
        private readonly IAppDbContext _db;
        private readonly ICurrentUserService _currentUser;
        public ProductService(IAppDbContext db, ICurrentUserService currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }
        public async Task<ProductResponse> CreateAsync(CreateProductRequest request)
        {
            var tenantId = _currentUser.TenantId;
          
            // Check 2: is the SKU already used by this tenant?
            var SkuExists = await _db.Products.AnyAsync(s => s.Sku == request.Sku );
            {
                if (SkuExists)
                {
                    throw new ConflictException($"The SKU '{request.Sku}' is already used by this tenant.");
                }
            }
            var product = new Product
            {
                Name = request.Name,
                TenantId = tenantId,
                Sku = request.Sku,
                Price = request.Price,
                StockQuantity = request.StockQuantity
            };
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return ToResponse(product);
        }
        public async Task<ProductResponse> GetByIdAsync( int id)
        {
        
            //FindAsync(5) finds product 5 of any tenant. If product 5 belongs to TechShop and someone calls /api/tenants/1/products/5, ModeHaus would see TechShop’s product. ❌ So we search by both id and tenantId.
            // var product = await _db.Products.FindAsync(id);
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id );
            if (product == null)
            {
                throw new NotFoundException($"Product {id} not found.");
            }
            return ToResponse(product);
        }
        public async Task<IEnumerable<ProductResponse>> GetAllByTenantIdAsync()
        {
           
            return await _db.Products
                .Select(p => ToResponse(p))
                .ToListAsync();
        }
        private static ProductResponse ToResponse(Product product) => new ProductResponse
        {
            Id = product.Id,
            TenantId = product.TenantId,
            Name = product.Name,
            Sku = product.Sku,
            Price = product.Price,
            StockQuantity = product.StockQuantity
        };
    }
}
