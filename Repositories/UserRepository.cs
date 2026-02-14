using System.Threading.Tasks;
using Cocktail.back.Data;
using Cocktail.back.Models;
using Microsoft.EntityFrameworkCore;

namespace Cocktail.back.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Correo == email);
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users.Include(u => u.Rol).FirstOrDefaultAsync(u => u.IdUsuario == id);
        }

        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Correo == email);
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users.Include(u => u.Rol).ToListAsync();
        }

        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
        }
    }
}
