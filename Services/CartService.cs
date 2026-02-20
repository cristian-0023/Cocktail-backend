using System.Threading.Tasks;
using System.Linq;
using Cocktail.back.Models;
using Cocktail.back.Repositories;

namespace Cocktail.back.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly IProductRepository _productRepository;

        public CartService(ICartRepository cartRepository, IProductRepository productRepository)
        {
            _cartRepository = cartRepository;
            _productRepository = productRepository;
        }

        public async Task<Cart> GetCartByUserIdAsync(int userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null)
            {
                // Return a transient cart for READ ONLY (no SaveChanges here)
                return new Cart { IdUsuario = userId, Items = new System.Collections.Generic.List<CartItem>() };
            }
            return cart;
        }

        public async Task<Cart> AddToCartAsync(int userId, int productId, int quantity)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            var product = await _productRepository.GetByIdAsync(productId);

            if (product == null) throw new System.Exception($"Product with ID {productId} not found.");

            if (cart == null) 
            {
                cart = new Cart { IdUsuario = userId, Items = new System.Collections.Generic.List<CartItem>() };
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.IdProducto == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    IdCarrito = cart.IdCarrito,
                    IdProducto = productId,
                    Quantity = quantity,
                    UnitPrice = product.Precio
                });
            }

            return await _cartRepository.SaveAsync(cart);
        }

        public async Task<Cart> UpdateQuantityAsync(int userId, int cartItemId, int quantity)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.IdCartItem == cartItemId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.Items.Remove(item);
                }
                else
                {
                    item.Quantity = quantity;
                }
                await _cartRepository.SaveAsync(cart);
            }

            return cart;
        }

        public async Task<bool> RemoveFromCartAsync(int userId, int cartItemId)
        {
            return await _cartRepository.DeleteItemAsync(cartItemId);
        }

        public async Task<bool> ClearCartAsync(int userId)
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            cart.Items.Clear();
            await _cartRepository.SaveAsync(cart);
            return true;
        }
    }
}
