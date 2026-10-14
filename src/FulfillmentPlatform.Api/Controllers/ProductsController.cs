using FulfillmentPlatform.Application.Products;
using FulfillmentPlatform.Domain.Entities;
using FulfillmentPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FulfillmentPlatform.Api.Controllers
{
    [Route("api/tenants/{tenantId:int}/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public ProductsController(AppDbContext db)
        {
            _db = db;
        }
        [HttpPost]
        public async Task<ActionResult<ProductResponse>> Create(int tenantId, CreateProductRequest createProductRequest)
        {
            //AnyAsync answers one yes/no question: “Is there at least one row that matches this condition?” It returns true or false.
            // Check 1: does the tenant exist?
            var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == tenantId);
            if (!tenantExists)
            {
                return NotFound();
            }
            // Check 2: is the SKU already used by this tenant?
            var SkuExists=await _db.Products.AnyAsync(s=> s.Sku == createProductRequest.Sku && s.TenantId == tenantId);
            {
                if (SkuExists)
                {
                    return Conflict($"The SKU '{createProductRequest.Sku}' is already used by this tenant.");
                }
            }
            var product = new Product
            {
                TenantId = tenantId,
                Name = createProductRequest.Name,
                Sku = createProductRequest.Sku,
                Price = createProductRequest.Price,
                StockQuantity = createProductRequest.StockQuantity

            };
             _db.Products.Add(product);
            await _db.SaveChangesAsync();
            var productResponse = new ProductResponse
            {
                Id = product.Id,
                TenantId = product.TenantId,
                Name = product.Name,
                Sku = product.Sku,
                Price = product.Price,
                StockQuantity = product.StockQuantity
            };
            return(CreatedAtAction(nameof(GetByProductId), new { tenantId = product.TenantId, id = product.Id }, productResponse));

        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductResponse>> GetByProductId(int tenantId, int id)
        {
            //FindAsync(5) finds product 5 of any tenant. If product 5 belongs to TechShop and someone calls /api/tenants/1/products/5, ModeHaus would see TechShop’s product. ❌ So we search by both id and tenantId.
           // var product = await _db.Products.FindAsync(id);
           var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);
            if (product == null)
            {
                return NotFound();
            }
            var productResponse = new ProductResponse
            {
                Id = product.Id,
                TenantId = product.TenantId,
                Name = product.Name,
                Sku = product.Sku,
                Price = product.Price,
                StockQuantity = product.StockQuantity
            };
            return Ok(productResponse);
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAllProducts(int tenantId)
        {
            var tenantExists = await _db.Tenants.AnyAsync(t => t.Id == tenantId);
            if (!tenantExists)
            {
                return NotFound();
            }
            var products = await _db.Products
                .Where(p => p.TenantId == tenantId)
                .Select(p => new ProductResponse
                {
                    Id = p.Id,
                    TenantId = p.TenantId,
                    Name = p.Name,
                    Sku = p.Sku,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity
                })
                .ToListAsync();
            return Ok(products);
        }
    }
}
