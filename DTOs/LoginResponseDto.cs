namespace Cocktail.back.DTOs
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new UserDto();
    }

    public class UserDto
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string? ImagenPerfilURL { get; set; }
        public string Rol { get; set; } = string.Empty;
        public bool Estado { get; set; }
    }
}
