using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cocktail.back.Models
{
    public class OrderItem
    {
        [Key]
        public int IdOrderItem { get; set; }

        [Required]
        public int IdOrder { get; set; }

        [Required]
        public int IdProducto { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [ForeignKey("IdProducto")]
        public Product? Product { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
