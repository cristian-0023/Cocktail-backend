using Cocktail.back.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Cocktail.back.Services
{
    public interface IOrderService
    {
        Task<Order> CheckoutAsync(int userId);
        Task<Order?> GetOrderByIdAsync(int orderId);
        Task<IEnumerable<Order>> GetUserOrdersAsync(int userId);
        Task<Invoice?> GetInvoiceByOrderIdAsync(int orderId);
        Task<Invoice?> GetInvoiceByIdAsync(int invoiceId);
    }
}
