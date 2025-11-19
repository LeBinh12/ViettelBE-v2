using Application.DTOs;
using Share.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface ICategoryService
    {
        Task<IResult<IEnumerable<CategoryResponse>>> GetAllAsync();
        Task<IResult<CategoryResponse>> GetByIdAsync(Guid id);
        Task<IResult<CategoryResponse>> CreateAsync(CategoryRequest request);
        Task<IResult<CategoryResponse>> UpdateAsync(CategoryUpdateRequest request);
        Task<IResult<bool>> DeleteAsync(Guid id);
    }
}
