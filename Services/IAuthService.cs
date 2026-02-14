using System.Threading.Tasks;
using Cocktail.back.DTOs;
using Cocktail.back.Models;

namespace Cocktail.back.Services
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> LoginAsync(LoginDto loginDto);
        Task<User> RegisterAsync(RegisterDto registerDto, string roleName);
    }
}
