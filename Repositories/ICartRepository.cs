using System.Threading.Tasks;
using Cocktail.back.Models;

namespace Cocktail.back.Repositories
{
    public interface ICartRepository
    {
        Task<Cart?> GetByUserIdAsync(int userId);
        Task<Cart> SaveAsync(Cart cart);
        Task<bool> DeleteItemAsync(int cartItemId);
        Task<CartItem?> GetItemByIdAsync(int cartItemId);
    }
}
