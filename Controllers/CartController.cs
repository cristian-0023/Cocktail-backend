using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Cocktail.back.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cocktail.back.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private int GetUserId()
        {
            // Intentamos obtener el ID desde NameIdentifier (Estándar) o userId (Personalizado)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("userId");
            
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int id)) 
            {
                 Console.WriteLine($"[CART ERROR] No se encontró un claim de ID válido en el token.");
                 return 0;
            }
            return id;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try 
            {
                var userId = GetUserId();
                if (userId <= 0) 
                {
                    Console.WriteLine("[CART ERROR] Intento de acceso sin ID de usuario válido.");
                    return Unauthorized(new { message = "Sesión inválida o expirada." });
                }
                
                var cart = await _cartService.GetCartByUserIdAsync(userId);
                
                // Si por alguna razón el servicio devolviera null (no debería pasar con el repo actual),
                // manejamos graciosamente en lugar de que rompa el JSON serializer o el frontend
                if (cart == null)
                {
                    return Ok(new { idCarrito = 0, idUsuario = userId, items = new Array[] { } });
                }

                return Ok(cart);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CART CRITICAL ERROR] GET /api/Cart: {ex.Message}");
                if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
                
                return StatusCode(500, new { 
                    error = "Ocurrió un error al obtener el carrito.",
                    details = ex.Message 
                });
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartReq req)
        {
            try 
            {
                if (req == null || req.ProductId <= 0) return BadRequest("Datos de producto inválidos.");
                if (req.Quantity <= 0) req.Quantity = 1;

                var userId = GetUserId();
                if (userId == 0) return Unauthorized();

                var cart = await _cartService.AddToCartAsync(userId, req.ProductId, req.Quantity);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en AddToCart: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQtyReq req)
        {
            try 
            {
                if (req == null || req.CartItemId <= 0) return BadRequest("ID de item inválido.");
                
                var userId = GetUserId();
                if (userId == 0) return Unauthorized();

                var cart = await _cartService.UpdateQuantityAsync(userId, req.CartItemId, req.Quantity);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en UpdateQuantity: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("item/{id}")]
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            try 
            {
                var userId = GetUserId();
                if (userId == 0) return Unauthorized();

                var result = await _cartService.RemoveFromCartAsync(userId, id);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en RemoveFromCart: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }

        [HttpDelete("clear")]
        public async Task<IActionResult> ClearCart()
        {
            try 
            {
                var userId = GetUserId();
                if (userId == 0) return Unauthorized();

                await _cartService.ClearCartAsync(userId);
                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en ClearCart: {ex.Message}");
                return StatusCode(500, ex.Message);
            }
        }
    }

    public class AddToCartReq { public int ProductId { get; set; } public int Quantity { get; set; } }
    public class UpdateQtyReq { public int CartItemId { get; set; } public int Quantity { get; set; } }
}
