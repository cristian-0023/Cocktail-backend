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
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null) 
            {
                 // Try NameIdentifier as fallback for compatibility
                 userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            }

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int id)) 
            {
                 Console.WriteLine($"Warning: Valid userId claim not found in token. Value: {userIdClaim?.Value}");
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
                if (userId == 0) return Unauthorized("No se pudo identificar al usuario.");
                
                var cart = await _cartService.GetCartByUserIdAsync(userId);
                return Ok(cart);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en GetCart: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
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
