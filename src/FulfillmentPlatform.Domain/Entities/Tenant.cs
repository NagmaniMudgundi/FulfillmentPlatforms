using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FulfillmentPlatform.Domain.Entities
{
    public class Tenant
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Product> Products { get; set; } = new ();
        public List<Order> Orders { get; set; } = new();
        // Slug(string, required) → the shop’s link name
        //Description(string?, optional) → e.g. “Fashion”
        //City(string?) → “Reutlingen”
        //BrandColor(string?) → “
        //#0F766E”
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? City { get; set; }
        public string? BrandColor { get; set; }

    }
}
