using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Tenants
{
    public record TenantResponse{
        public int Id { get; init; }
        public string Name { get; init; }= string.Empty;
        public DateTime CreatedAt { get; init; }
        public string Slug { get; init; } = string.Empty;
    }

}
