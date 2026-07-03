using System.Data;
using Dapper;
using NFSCCard.Models;

namespace NFSCCard.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly IDbConnection _db;
        public AuthRepository(IDbConnection db) => _db = db;

        public async Task<User?> GetByEmailAsync(string email)
        {
            var p = new DynamicParameters();
            p.Add("@Email", email);
            var user = await _db.QueryFirstOrDefaultAsync<User>("SP_LoginUser", p, commandType: CommandType.StoredProcedure);
            return user;
        }

        public async Task<int> CreateUserAndCustomerAsync(DynamicParameters parameters)
        {
            await _db.ExecuteAsync("SP_Customer_Save", parameters, commandType: CommandType.StoredProcedure);
            return 1;
        }

        public async Task<int> UpdatePasswordHashAsync(int userId, string passwordHash)
        {
            var p = new DynamicParameters();
            p.Add("@UserId", userId);
            p.Add("@PasswordHash", passwordHash);
            return await _db.ExecuteAsync("UPDATE Users SET PasswordHash = @PasswordHash WHERE UserId = @UserId", p, commandType: CommandType.Text);
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            var p = new DynamicParameters();
            p.Add("@UserId", userId);
            var user = await _db.QueryFirstOrDefaultAsync<User>(
                "SELECT UserId, Email, PasswordHash, Role, IsActive FROM Users WHERE UserId = @UserId",
                p,
                commandType: CommandType.Text);
            return user;
        }
    }
}
