using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FulfillmentPlatform.Application.Products
{
    public record CreateProductRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; init; } = string.Empty;
        [Required, MaxLength(100)]
        public string Sku { get; init; } = string.Empty;
       
        [Range(0.01, 1000000, ErrorMessage = "Price must be greater than 0.")]
        public decimal Price { get; init; }
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative.")]   
        public int StockQuantity { get; init; }
        [Range(1, int.MaxValue, ErrorMessage = "Please choose a category.")]
        public int CategoryId { get; init; }

        [MaxLength(1000)]
        public string? Description { get; init; }

        [MaxLength(500), Url]
        public string? ImageUrl { get; init; }

        public bool IsActive { get; init; } = true;
    }
}
