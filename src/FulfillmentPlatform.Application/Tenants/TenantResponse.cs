using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Tenants
{
    public record TenantResponse(int Id, string Name, DateTime CreatedAt);

}
