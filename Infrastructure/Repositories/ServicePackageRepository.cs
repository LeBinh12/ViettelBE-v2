using Domain.Abstractions;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ServicePackageRepository : IServicePackageRepository
    {
        private readonly ApplicationDbContext _context;

        public ServicePackageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ServicePackage>> GetAllAsync()
        {
            return await _context.ServicePackages
                .Include(p => p.Category)
                .Where(p => !p.isDeleted)
                .ToListAsync();
        }

        public async Task<ServicePackage?> GetByIdAsync(Guid id)
        {
            return await _context.ServicePackages
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && !p.isDeleted);
        }

        public async Task AddAsync(ServicePackage servicePackage)
        {
            await _context.ServicePackages.AddAsync(servicePackage);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ServicePackage servicePackage)
        {
            _context.ServicePackages.Update(servicePackage);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(ServicePackage servicePackage)
        {
            servicePackage.isDeleted = true;
            _context.ServicePackages.Update(servicePackage);
            await _context.SaveChangesAsync();
        }
        
        public async Task<IEnumerable<ServicePackage>> GetByCategoryAsync(Guid categoryId)
        {
            return await _context.ServicePackages
                .Include(p => p.Category)
                .Where(p => !p.isDeleted && p.CategoryId == categoryId)
                .ToListAsync();
        }

    }
}
