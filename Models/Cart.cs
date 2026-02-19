using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cocktail.back.Models
{
    public class Cart
    {
        [Key]
        public int IdCarrito { get; set; }

        [Required]
        public int IdUsuario { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
