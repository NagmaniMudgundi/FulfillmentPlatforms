using FulfillmentPlatform.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Orders
{
    public record ChangeOrderStatusRequest
    {//NewStatus	OrderStatus	[EnumDataType(typeof(OrderStatus))]
        //Note string?[MaxLength(500)] (matches the database column)
        [EnumDataType(typeof(OrderStatus))]
        public OrderStatus NewStatus { get; init; }
        public string? Note {get; init;}
    }
}
