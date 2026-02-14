using Cocktail.back.Models;
using System.Threading.Tasks;

namespace Cocktail.back.Services
{
    public interface ICartService
    {
        Task<Cart> GetCartByUserIdAsync(int userId);
        Task<Cart> AddToCartAsync(int userId, int productId, int quantity);
        Task<Cart> UpdateQuantityAsync(int userId, int cartItemId, int quantity);
        Task<bool> RemoveFromCartAsync(int userId, int cartItemId);
        Task<bool> ClearCartAsync(int userId);
    }
}
