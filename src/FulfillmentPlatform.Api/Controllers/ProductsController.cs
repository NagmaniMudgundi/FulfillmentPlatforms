using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


namespace FulfillmentPlatform.Api.Controllers
{
    [Route("api/products")]
    [ApiController]
    [Authorize(Roles = Roles.TenantAdmin)]
    public class ProductsController : ControllerBase
    {
        //private readonly AppDbContext _db;
        private readonly IProductService _productService;
        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }
        [Authorize(Roles = Roles.TenantAdmin)]
        [HttpPost]
        public async Task<ActionResult<ProductResponse>> Create( CreateProductRequest createProductRequest)
        {
            var result=await _productService.CreateAsync(createProductRequest);
            return CreatedAtAction(nameof(GetByProductId), new { id = result.Id }, result);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProductResponse>> GetByProductId( int id)
        {
            var result = await _productService.GetByIdAsync( id);
            return Ok(result);
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductResponse>>> GetAllProducts()
        {
            var result = await _productService.GetAllAsync();
            return Ok(result);
        }

        [HttpPut("{id:int}")]                                      // PUT /api/products/12
        public async Task<ActionResult<ProductResponse>> Update(int id, UpdateProductRequest request)
    => Ok(await _productService.UpdateAsync(id, request));
    }
}
