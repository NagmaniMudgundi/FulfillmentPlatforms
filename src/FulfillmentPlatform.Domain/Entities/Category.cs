using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;   // "Fashion"
    public string Slug { get; set; } = string.Empty;   // "fashion"  → used in URLs: ?category=fashion
    public List<Product> Products { get; set; } = new();
}
