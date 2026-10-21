using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Auth
{
    public class LoginResponse
    {
        public string Token { get; init; } = string.Empty;
        public DateTime ExpiresAt { get; init; }
    }
}
