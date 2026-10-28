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
      
        [Required, StringLength(100)]
        public string FirstName { get; set; } = default!;

        [Required, StringLength(100)]
        public string LastName { get; set; } = default!;
    }
}
