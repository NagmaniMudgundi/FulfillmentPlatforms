using FulfillmentPlatform.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Infrastructure.Identity
{
    public class ApplicationUser: IdentityUser
    {
        public int? TenantId { get; set; }          // null = customer (platform), value = admin of that shop
        public Tenant? Tenant { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}
