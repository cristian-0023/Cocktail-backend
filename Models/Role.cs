using System.ComponentModel.DataAnnotations;

namespace Cocktail.back.Models
{
    public class Role
    {
        [Key]
        public int IdRol { get; set; }
        
        [Required]
        [MaxLength(50)]
        public string NombreRol { get; set; } = string.Empty;
    }
}
