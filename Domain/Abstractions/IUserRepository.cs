using Domain.Entities;

namespace Domain.Abstractions
{
    public interface IUserRepository
    {
        Task<IEnumerable<UserAccount>> GetAll();
        Task<UserAccount?> GetByUsernameAsync(string username);
        Task<UserAccount?> GetByEmailAsync(string email);
        Task AddAsync(UserAccount user);

        Task<(IEnumerable<UserAccount> users, int totalCount)> GetPagedUsersAsync(int pageNumber, int pageSize, string? keyword);
    }
}
