using Microsoft.EntityFrameworkCore;
using Cocktail.back.Data;
using Cocktail.back.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Cocktail.back.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.IdOrder == orderId);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.IdUsuario == userId)
                .ToListAsync();
        }

        public async Task<Invoice> CreateInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task<Invoice?> GetInvoiceByOrderIdAsync(int orderId)
        {
            return await _context.Invoices
                .FirstOrDefaultAsync(i => i.IdOrder == orderId);
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.Order)
                .ThenInclude(o => o.OrderItems)
                .FirstOrDefaultAsync(i => i.IdInvoice == invoiceId);
        }
    }
}
