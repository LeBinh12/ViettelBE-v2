using Domain.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<UserAccount>> GetAll()
    {
        return await _db.UserAccounts
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<UserAccount?> GetByUsernameAsync(string username)
    {
        return await _db.UserAccounts.FirstOrDefaultAsync(u => u.Username == username);   
    }

    public async Task<UserAccount?> GetByEmailAsync(string email)
    {
        return await _db.UserAccounts.FirstOrDefaultAsync(u => u.Email == email);   
    }

    public async Task AddAsync(UserAccount user)
    {
        await _db.UserAccounts.AddAsync(user);
        await _db.SaveChangesAsync();
    }
}