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
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression("^[a-z0-9]+(-[a-z0-9]+)*$",
    ErrorMessage = "Use only lowercase letters, numbers and single hyphens, e.g. 'modehaus'.")]
        public string Slug { get; set; } = default!;

        [StringLength(100)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Use a hex colour like #0F766E.")]
        public string? BrandColor { get; set; }

    }
        
        


}


