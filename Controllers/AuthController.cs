using System;
using System.Threading.Tasks;
using Cocktail.back.DTOs;
using Cocktail.back.Models;
using Cocktail.back.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cocktail.back.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var response = await _authService.LoginAsync(loginDto);
                if (response == null)
                {
                    return Unauthorized(new { message = "Credenciales inválidas" });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGIN ERROR] {ex.Message} \n {ex.StackTrace}");
                return StatusCode(500, new { message = "Error interno al procesar login", detail = ex.Message });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var user = await _authService.RegisterAsync(registerDto, "Invitado");
                return Ok(new { 
                    user.IdUsuario, 
                    user.Nombre, 
                    user.Correo, 
                    user.ImagenPerfilURL,
                    Rol = user.Rol?.NombreRol 
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
