using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Orders
{
    public interface IOrderService
    {
        Task<OrderResponse> PlaceOrderAsync(CreateOrderRequest request);
        Task<List<OrderResponse>> GetAllAsync();
        Task<OrderResponse> GetByIdAsync(int id);
        Task<OrderResponse> ChangeStatusAsync(int id, ChangeOrderStatusRequest request);
        Task<OrderResponse> CancelAsync(int id, CancelOrderRequest request);
    }
}
