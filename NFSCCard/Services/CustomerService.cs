using Dapper;
using NFSCCard.DTOs.Customer;
using NFSCCard.Repositories;

namespace NFSCCard.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;

        public CustomerService(ICustomerRepository repo)
        {
            _repo = repo;
        }

        public async Task<IEnumerable<CustomerDto>> GetAllAsync()
        {
            return await _repo.GetAllAsync();
        }

        public async Task<CustomerDto?> GetByIdAsync(int customerId)
        {
            return await _repo.GetByIdAsync(customerId);
        }

        public async Task<CustomerDto?> GetByUserIdAsync(int userId)
        {
            return await _repo.GetByUserIdAsync(userId);
        }

        public async Task<CustomerDto?> GetByNfscAsync(string nfscCode)
        {
            return await _repo.GetByNfscCodeAsync(nfscCode);
        }

        public async Task SaveAsync(DynamicParameters parameters)
        {
            await _repo.SaveAsync(parameters);
        }

        public async Task DeleteAsync(int customerId)
        {
            await _repo.DeleteAsync(customerId);
        }
    }
}
