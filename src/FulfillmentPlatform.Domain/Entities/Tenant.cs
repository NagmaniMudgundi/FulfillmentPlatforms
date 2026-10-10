using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Domain.Entities
{
    public class Tenant
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Product> Products { get; set; } = new ();
        public List<Order> Orders { get; set; } = new();
    }   
}
