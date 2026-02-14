using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cocktail.back.Models;
using Cocktail.back.Repositories;
using Cocktail.back.DTOs;
using System.Security.Claims;

namespace Cocktail.back.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository _userRepository;

        public UserController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userRepository.GetAllAsync();
            var userDtos = users.Select(u => new UserDto
            {
                IdUsuario = u.IdUsuario,
                Nombre = u.Nombre,
                Correo = u.Correo,
                ImagenPerfilURL = u.ImagenPerfilURL,
                Rol = u.Rol?.NombreRol ?? "Invitado",
                Estado = u.Estado
            });
            return Ok(userDtos);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RegisterDto dto)
        {
            if (await _userRepository.UserExistsAsync(dto.Correo))
                return BadRequest("El correo ya está registrado.");

            var user = new User
            {
                Nombre = dto.Nombre,
                Correo = dto.Correo,
                Contrasena = BCrypt.Net.BCrypt.HashPassword(dto.Contrasena),
                IdRol = 2, // Default to Invitado, can be improved to accept role
                Estado = true
            };

            await _userRepository.CreateAsync(user);
            return Ok(new { message = "Usuario creado exitosamente" });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();

            user.Nombre = dto.Nombre;
            user.Correo = dto.Correo;
            user.Estado = dto.Estado;
            // Update role if needed (mapping Rol string to IdRol)
            
            await _userRepository.UpdateAsync(user);
            return Ok(new { message = "Usuario actualizado" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Prevent self-deletion
            var currentUserIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (currentUserIdClaim != null && int.TryParse(currentUserIdClaim.Value, out int currentId))
            {
                if (currentId == id)
                    return BadRequest("No puedes eliminar tu propia cuenta de administrador.");
            }

            await _userRepository.DeleteAsync(id);
            return Ok(new { message = "Usuario eliminado" });
        }
    }
}
