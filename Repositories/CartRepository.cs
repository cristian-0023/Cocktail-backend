using Microsoft.EntityFrameworkCore;
using Cocktail.back.Data;
using Cocktail.back.Models;
using System.Threading.Tasks;
using System.Linq;

namespace Cocktail.back.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _context;

        public CartRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetByUserIdAsync(int userId)
        {
            return await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.IdUsuario == userId);
        }

        public async Task<Cart> SaveAsync(Cart cart)
        {
            var entry = _context.Entry(cart);
            if (entry.State == EntityState.Detached)
            {
                _context.Carts.Update(cart);
            }
            
            // Aseguramos que los productos NO sean marcados como modificados accidentalmente
            foreach (var item in cart.Items)
            {
                if (item.Product != null)
                {
                    _context.Entry(item.Product).State = EntityState.Unchanged;
                }
            }

            await _context.SaveChangesAsync();
            return cart;
        }

        public async Task<bool> DeleteItemAsync(int cartItemId)
        {
            var item = await _context.CartItems.FindAsync(cartItemId);
            if (item == null) return false;

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CartItem?> GetItemByIdAsync(int cartItemId)
        {
            return await _context.CartItems.FindAsync(cartItemId);
        }
    }
}
