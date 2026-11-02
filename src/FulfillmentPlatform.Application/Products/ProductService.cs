using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Common.Exceptions;
using FulfillmentPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Application.Products
{
    public class ProductService : IProductService
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
            // only shop admins create products: the product belongs to THEIR shop
            var tenantId = _currentUser.RequireTenantId();

            // validation: does the category exist?
            if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            {
                throw new BadRequestException($"Category {request.CategoryId} does not exist.");
            }

            // conflict: is the SKU already used in this shop? (the query filter limits this to the admin's shop)
            var skuExists = await _db.Products.AnyAsync(p => p.Sku == request.Sku);
            if (skuExists)
            {
                throw new ConflictException($"The SKU '{request.Sku}' is already used in your shop.");
            }

            var product = new Product
            {
                TenantId = tenantId,
                Name = request.Name,
                Sku = request.Sku,
                Price = request.Price,
                StockQuantity = request.StockQuantity,
                CategoryId = request.CategoryId,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                IsActive = request.IsActive
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return ToResponse(product);
        }

        public async Task<ProductResponse> GetByIdAsync(int id)
        {
            // the query filter makes sure an admin only finds products of their own shop
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                throw new NotFoundException($"Product {id} not found.");
            }
            return ToResponse(product);
        }

        public async Task<IEnumerable<ProductResponse>> GetAllAsync()
        {
            var products = await _db.Products
                .OrderBy(p => p.Name)
                .ToListAsync();

            return products.Select(ToResponse);
        }
        public async Task<ProductResponse> UpdateAsync(int id, UpdateProductRequest request)
        {
            _currentUser.RequireTenantId();                           // shop admins only

            // the query filter → an admin only finds products of their OWN shop (otherwise 404)
            var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id);
            if (product is null)
            {
                throw new NotFoundException($"Product {id} not found.");
            }

            if (!await _db.Categories.AnyAsync(c => c.Id == request.CategoryId))
            {
                throw new BadRequestException($"Category {request.CategoryId} does not exist.");
            }

            product.Name = request.Name;
            product.Price = request.Price;
            product.StockQuantity = request.StockQuantity;
            product.CategoryId = request.CategoryId;
            product.Description = request.Description;
            product.ImageUrl = request.ImageUrl;
            product.IsActive = request.IsActive;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // RowVersion: a customer bought this product while the admin was editing
                throw new ConflictException("This product was changed in the meantime (e.g. sold). Please reload and try again.");
            }

            return ToResponse(product);
        }
        private static ProductResponse ToResponse(Product product) => new ProductResponse
        {
            Id = product.Id,
            TenantId = product.TenantId,
            Name = product.Name,
            Sku = product.Sku,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            Description = product.Description,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive
        };
    }
}