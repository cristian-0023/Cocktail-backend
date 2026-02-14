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
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst("userId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                Console.WriteLine("DEBUG: No userId or NameIdentifier claim found in token.");
                return 0;
            }
            
            if (int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            Console.WriteLine($"DEBUG: Could not parse userId claim: {userIdClaim.Value}");
            return 0;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userId = GetUserId();
                var order = await _orderService.CheckoutAsync(userId);
                return Ok(order);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetOrderHistory()
        {
            var userId = GetUserId();
            var orders = await _orderService.GetUserOrdersAsync(userId);
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }

        [HttpGet("invoice/order/{orderId}")]
        public async Task<IActionResult> GetInvoiceByOrderId(int orderId)
        {
            var invoice = await _orderService.GetInvoiceByOrderIdAsync(orderId);
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }

        [HttpGet("invoice/{id}")]
        public async Task<IActionResult> GetInvoiceById(int id)
        {
            var invoice = await _orderService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }
    }
}
