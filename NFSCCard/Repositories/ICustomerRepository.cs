using NFSCCard.DTOs.Customer;
using Dapper;

namespace NFSCCard.Repositories
{
    public interface ICustomerRepository
    {
        Task<IEnumerable<CustomerDto>> GetAllAsync();
        Task<CustomerDto?> GetByIdAsync(int customerId);
        Task<CustomerDto?> GetByUserIdAsync(int userId);
        Task<CustomerDto?> GetByNfscCodeAsync(string nfscCode);
        Task SaveAsync(DynamicParameters parameters);
        Task DeleteAsync(int customerId);
    }
}
