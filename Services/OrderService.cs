using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Cocktail.back.Models;
using Cocktail.back.Repositories;

namespace Cocktail.back.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ICartRepository _cartRepository;
        private readonly IUserRepository _userRepository;

        public OrderService(IOrderRepository orderRepository, ICartRepository cartRepository, IUserRepository userRepository)
        {
            _orderRepository = orderRepository;
            _cartRepository = cartRepository;
            _userRepository = userRepository;
        }

        public async Task<Order> CheckoutAsync(int userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null || !cart.Items.Any()) 
                throw new Exception("El carrito está vacío o no se pudo encontrar.");

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            var order = new Order
            {
                IdUsuario = userId,
                OrderDate = DateTime.UtcNow,
                TotalAmount = cart.Items.Sum(i => i.Quantity * i.UnitPrice),
                PaymentStatus = "Paid", // Simulated payment
                OrderItems = cart.Items.Select(i => new OrderItem
                {
                    IdProducto = i.IdProducto,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            var createdOrder = await _orderRepository.CreateOrderAsync(order);

            // Generate Invoice
            var invoice = new Invoice
            {
                IdOrder = createdOrder.IdOrder,
                InvoiceDate = DateTime.UtcNow,
                CustomerName = user.Nombre,
                TotalAmount = createdOrder.TotalAmount
            };
            await _orderRepository.CreateInvoiceAsync(invoice);

            // Clear Cart
            cart.Items.Clear();
            await _cartRepository.SaveAsync(cart);

            return createdOrder;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _orderRepository.GetOrderByIdAsync(orderId);
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(int userId)
        {
            return await _orderRepository.GetOrdersByUserIdAsync(userId);
        }

        public async Task<Invoice?> GetInvoiceByOrderIdAsync(int orderId)
        {
            return await _orderRepository.GetInvoiceByOrderIdAsync(orderId);
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId)
        {
            return await _orderRepository.GetInvoiceByIdAsync(invoiceId);
        }
    }
}
