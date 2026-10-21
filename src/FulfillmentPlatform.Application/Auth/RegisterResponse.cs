using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Auth
{
    public record RegisterResponse
    {
        public string UserId { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}
