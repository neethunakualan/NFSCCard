using NFSCCard.Models;
using Dapper;

namespace NFSCCard.Repositories
{
    public interface IAuthRepository
    {
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIdAsync(int userId);
        Task<int> CreateUserAndCustomerAsync(DynamicParameters parameters);
        Task<int> UpdatePasswordHashAsync(int userId, string passwordHash);
    }
}
