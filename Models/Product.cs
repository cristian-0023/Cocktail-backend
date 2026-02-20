using System;
using System.ComponentModel.DataAnnotations;

namespace Cocktail.back.Models
{
    public class Product
    {
        [Key]
        public int IdProducto { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 1000000)]
        public decimal Precio { get; set; }

        public string ImagenURL { get; set; } = string.Empty;

        public bool Disponibilidad { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
