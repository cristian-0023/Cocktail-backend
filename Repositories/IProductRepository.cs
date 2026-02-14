using System.Threading.Tasks;
using System.Collections.Generic;
using Cocktail.back.Models;

namespace Cocktail.back.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(int id);
        Task<Product> CreateAsync(Product product);
        Task<Product?> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
    }
}
