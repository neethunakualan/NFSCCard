using System.Data;
using Dapper;
using NFSCCard.DTOs.Customer;

namespace NFSCCard.Repositories
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IDbConnection _db;
        public CustomerRepository(IDbConnection db) => _db = db;

        public async Task<IEnumerable<CustomerDto>> GetAllAsync()
        {
            return await _db.QueryAsync<CustomerDto>("SP_Customer_List", commandType: CommandType.StoredProcedure);
        }

        public async Task<CustomerDto?> GetByIdAsync(int customerId)
        {
            var p = new DynamicParameters();
            p.Add("@CustomerId", customerId);
            return await _db.QueryFirstOrDefaultAsync<CustomerDto>("SP_Customer_GetById", p, commandType: CommandType.StoredProcedure);
        }

        public async Task<CustomerDto?> GetByUserIdAsync(int userId)
        {
            var p = new DynamicParameters();
            p.Add("@UserId", userId);
            return await _db.QueryFirstOrDefaultAsync<CustomerDto>("SELECT TOP 1 * FROM Customers WHERE UserId = @UserId AND IsActive = 1", p, commandType: CommandType.Text);
        }

        public async Task<CustomerDto?> GetByNfscCodeAsync(string nfscCode)
        {
            var p = new DynamicParameters();
            p.Add("@NFCCodeUniqueId", nfscCode);
            return await _db.QueryFirstOrDefaultAsync<CustomerDto>("SP_Customer_GetByNFSCCode", p, commandType: CommandType.StoredProcedure);
        }

        public async Task SaveAsync(DynamicParameters parameters)
        {
            await _db.ExecuteAsync("SP_Customer_Save", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteAsync(int customerId)
        {
            var p = new DynamicParameters();
            p.Add("@CustomerId", customerId);
            await _db.ExecuteAsync("SP_Customer_Delete", p, commandType: CommandType.StoredProcedure);
        }
    }
}
