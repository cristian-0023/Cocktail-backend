using System.ComponentModel.DataAnnotations;

namespace Cocktail.back.DTOs
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public string Contrasena { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        [Required]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public string Contrasena { get; set; } = string.Empty;
    }

    public class ProductDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public string ImagenURL { get; set; } = string.Empty;
        public bool Disponibilidad { get; set; } = true;
    }
}
