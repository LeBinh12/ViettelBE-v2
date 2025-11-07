using Domain.Entities;

namespace Domain.Abstractions;

public interface IUserRepository
{
    Task<IEnumerable<UserAccount>> GetAll();

    Task<UserAccount?> GetByUsernameAsync(string username);
    Task<UserAccount?> GetByEmailAsync(string email);
    Task AddAsync(UserAccount user);
}
