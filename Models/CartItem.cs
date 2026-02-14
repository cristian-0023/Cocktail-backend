using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cocktail.back.Models
{
    public class CartItem
    {
        [Key]
        public int IdCartItem { get; set; }

        [Required]
        public int IdCarrito { get; set; }

        [Required]
        public int IdProducto { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [ForeignKey("IdProducto")]
        public Product? Product { get; set; }
    }
}
