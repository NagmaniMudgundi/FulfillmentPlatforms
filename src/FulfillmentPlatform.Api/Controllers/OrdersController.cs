using FulfillmentPlatform.Application.Common;
using FulfillmentPlatform.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FulfillmentPlatform.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }
        [Authorize(Roles = Roles.Customer)]
        [HttpPost]
        public async Task<ActionResult<OrderResponse>> PlaceOrder(CreateOrderRequest request)
        {
            var orderResponse = await _orderService.PlaceOrderAsync(request);
            return CreatedAtAction(nameof(GetOrderById), new { id = orderResponse.Id }, orderResponse);
        }
        [HttpGet("{id:int}")]
        public async Task<ActionResult<OrderResponse>> GetOrderById(int id)
        {
            var orderResponse = await _orderService.GetByIdAsync(id);
            return Ok(orderResponse);
        }
        [HttpGet]
        public async Task<ActionResult<List<OrderResponse>>> GetAllOrders()
        {
            var orders = await _orderService.GetAllAsync();
            return Ok(orders);
        }
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = Roles.TenantAdmin)]
        public async Task<ActionResult<OrderResponse>> ChangeStatus(int id, ChangeOrderStatusRequest request)
        {
            var orderResponse = await _orderService.ChangeStatusAsync(id, request);
            return Ok(orderResponse);
        }
        [HttpPost("{id:int}/cancel")]
        public async Task<ActionResult<OrderResponse>> CancelOrder(int id, CancelOrderRequest request)
        {
            var orderResponse = await _orderService.CancelAsync(id, request);
            return Ok(orderResponse);
        }

    }
}
