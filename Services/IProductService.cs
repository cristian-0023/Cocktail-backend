using System.Threading.Tasks;
using System.Collections.Generic;
using Cocktail.back.DTOs;
using Cocktail.back.Models;

namespace Cocktail.back.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product?> GetProductByIdAsync(int id);
        Task<Product> CreateProductAsync(ProductDto productDto);
        Task<Product?> UpdateProductAsync(int id, ProductDto productDto);
        Task<bool> DeleteProductAsync(int id);
    }
}
