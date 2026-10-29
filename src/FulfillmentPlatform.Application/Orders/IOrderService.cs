using FulfillmentPlatform.Application.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Orders
{
    public interface IOrderService
    {
        Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request);
        Task<PagedResult<OrderResponse>> GetAllAsync(OrderQuery query);
        Task<OrderResponse> GetByIdAsync(int id);
        Task<OrderResponse> ChangeStatusAsync(int id, ChangeOrderStatusRequest request);
        Task<OrderResponse> CancelAsync(int id, CancelOrderRequest request);
        Task CancelStaleOrdersAsync(int olderThanDays);
    }
}
