using System.Threading.Tasks;
using Cocktail.back.Models;
using System.Collections.Generic;

namespace Cocktail.back.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> CreateOrderAsync(Order order);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetOrdersByUserIdAsync(int userId);
        Task<Invoice> CreateInvoiceAsync(Invoice invoice);
        Task<Invoice?> GetInvoiceByOrderIdAsync(int orderId);
        Task<Invoice?> GetInvoiceByIdAsync(int invoiceId);
    }
}
