using Application.DTOs;
using Share;
using Share.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IServicePackageService
    {
        Task<IResult<IEnumerable<ServicePackageResponse>>> GetAllAsync();
        Task<IResult<ServicePackageResponse>> GetByIdAsync(Guid id);
        Task<IResult<IEnumerable<ServicePackageResponse>>> SearchAsync(string? keyword);
        Task<IResult<ServicePackageResponse>> CreateAsync(ServicePackageRequest request);
        Task<IResult<ServicePackageResponse>> UpdateAsync(ServicePackageUpdateRequest request);
        Task<IResult<bool>> DeleteAsync(Guid id);
        Task<IResult<IEnumerable<ServicePackageResponse>>> GetByCategoryAsync(Guid categoryId);
    }
}
