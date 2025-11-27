using Domain.Entities;

namespace Domain.Abstractions
{
    public interface IServicePackageRepository
    {
        Task<IEnumerable<ServicePackage>> GetAllAsync();
        Task<IEnumerable<ServicePackage>> SearchAsync(string? keyword);
        Task<ServicePackage?> GetByIdAsync(Guid id);
        Task AddAsync(ServicePackage servicePackage);
        Task UpdateAsync(ServicePackage servicePackage);
        Task DeleteAsync(ServicePackage servicePackage);
        Task<IEnumerable<ServicePackage>> GetByCategoryAsync(Guid categoryId);
    }
}
