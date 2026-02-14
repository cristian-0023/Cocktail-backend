using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cocktail.back.Models
{
    public class User
    {
        [Key]
        public int IdUsuario { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public string Contrasena { get; set; } = string.Empty; // Encrypted

        public int IdRol { get; set; }
        
        [ForeignKey("IdRol")]
        public Role? Rol { get; set; }

        public bool Estado { get; set; } = true;

        public string? ImagenPerfilURL { get; set; } = "https://cdn-icons-png.flaticon.com/512/149/149071.png";
    }
}
