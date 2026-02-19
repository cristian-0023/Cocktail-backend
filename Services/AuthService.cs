using System.Threading.Tasks;
using Cocktail.back.Data;
using Cocktail.back.DTOs;
using Cocktail.back.Models;
using Cocktail.back.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Linq;

namespace Cocktail.back.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ApplicationDbContext _context; // Direct access for Roles simple check, or add RoleRepo
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, ApplicationDbContext context, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _context = context;
            _configuration = configuration;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginDto loginDto)
        {
            Console.WriteLine($"[AUTH] Login attempt for: {loginDto.Correo}");
            
            var user = await _userRepository.GetByEmailAsync(loginDto.Correo);
            
            if (user == null)
            {
                Console.WriteLine($"[AUTH] ❌ User not found: {loginDto.Correo}");
                return null;
            }
            
            Console.WriteLine($"[AUTH] ✅ User found: {user.Nombre} (ID: {user.IdUsuario})");
            
            bool passwordValid = BCrypt.Net.BCrypt.Verify(loginDto.Contrasena, user.Contrasena);
            Console.WriteLine($"[AUTH] Password verification: {(passwordValid ? "✅ VALID" : "❌ INVALID")}");
            
            if (!passwordValid)
            {
                Console.WriteLine($"[AUTH] ❌ Invalid password for: {loginDto.Correo}");
                return null;
            }

            var token = GenerateJwtToken(user);
            Console.WriteLine($"[AUTH] ✅ Login successful for: {user.Correo}");
            
            return new LoginResponseDto
            {
                Token = token,
                User = new UserDto
                {
                    IdUsuario = user.IdUsuario,
                    Nombre = user.Nombre,
                    Correo = user.Correo,
                    ImagenPerfilURL = user.ImagenPerfilURL,
                    Rol = user.Rol?.NombreRol ?? "Invitado",
                    Estado = user.Estado
                }
            };
        }

        public async Task<User> RegisterAsync(RegisterDto registerDto, string roleName)
        {
            if (await _userRepository.UserExistsAsync(registerDto.Correo))
            {
                throw new Exception("El usuario ya existe.");
            }

            var role = _context.Roles.FirstOrDefault(r => r.NombreRol == roleName) 
                       ?? _context.Roles.First(r => r.NombreRol == "Invitado");

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Contrasena);

            var user = new User
            {
                Nombre = registerDto.Nombre,
                Correo = registerDto.Correo,
                Contrasena = passwordHash,
                IdRol = role.IdRol,
                Rol = role, 
                Estado = true
            };

            return await _userRepository.CreateAsync(user);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtKey = _configuration["Jwt:Key"] ?? "fallback_secret_key_at_least_32_characters_long";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Correo),
                new Claim("userId", user.IdUsuario.ToString()),
                new Claim(ClaimTypes.Role, user.Rol?.NombreRol ?? "Invitado")
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
