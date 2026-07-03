using Dapper;
using NFSCCard.DTOs.Customer;

namespace NFSCCard.Services
{
    public interface ICustomerService
    {
        Task<IEnumerable<CustomerDto>> GetAllAsync();
        Task<CustomerDto?> GetByIdAsync(int customerId);
        Task<CustomerDto?> GetByUserIdAsync(int userId);
        Task<CustomerDto?> GetByNfscAsync(string nfscCode);
        Task SaveAsync(DynamicParameters parameters);
        Task DeleteAsync(int customerId);
    }
}
