using Domain.Entities;

namespace Application.Interfaces
{
    public interface ICustomerRepository
    {
        Task<List<Customer>> GetAllAsync();
        Task<Customer?> GetByEmailAsync(string email);
        Task<Customer?> GetByIdAsync(Guid id);
        Task AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(Guid id);
    }
}
