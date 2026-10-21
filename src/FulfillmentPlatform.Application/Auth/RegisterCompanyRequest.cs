using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Auth
{
    public class RegisterCompanyRequest
    {
        

        [Required, MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;
        [Required, EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;
        [Required]
        public string AdminPassword { get; set; } = string.Empty;
    }
}
