using Domain.Entities;

namespace Application.Interfaces
{
    public interface ICustomerService
    {
        Task<List<Customer>> GetAllAsync();
        Task AddAsync(Customer customer);
        Task UpdateAsync(Customer customer);
        Task DeleteAsync(Guid id);
        Task<bool> IsEmailValidAsync(string email);
    }
}
