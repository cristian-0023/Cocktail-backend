using System.Threading.Tasks;
using Cocktail.back.Models;

namespace Cocktail.back.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task<IEnumerable<User>> GetAllAsync();
        Task<User> UpdateAsync(User user);
        Task DeleteAsync(int id);
        Task<bool> UserExistsAsync(string email);
    }
}
