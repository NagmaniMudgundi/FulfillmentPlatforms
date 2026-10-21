using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Auth
{
    public record RegisterRequest
    {
        [Required,EmailAddress]   
        public string Email { get; init; } = string.Empty;
        [Required]
        public string Password { get; init; } = string.Empty;
        [Range(1, int.MaxValue)]
        public int TenantId { get; init; } 
    }
}
