using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Tenants
{
    public record CreateTenantRequest
    {

        [Required, MaxLength(200)]
        public string Name { get; init; } = string.Empty;

    }
        
        


}


