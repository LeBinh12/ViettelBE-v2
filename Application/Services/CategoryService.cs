using Application.DTOs;
using Application.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using Share;
using Share.Interfaces;

namespace Application.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;
        
        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        public async Task<IResult<IEnumerable<CategoryResponse>>> GetAllAsync()
        {
            var categories = await _repository.GetAllAsync();
            var response = categories.Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description ?? " "
            });

            return await Result<IEnumerable<CategoryResponse>>.SuccessAsync(response, "Lấy danh sách danh mục thành công");
        }

        public async Task<IResult<CategoryResponse>> GetByIdAsync(Guid id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null)
                return await Result<CategoryResponse>.FailureAsync("Không tìm thấy danh mục");

            var response = new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description ?? " "
            };

            return await Result<CategoryResponse>.SuccessAsync(response, "Lấy chi tiết danh mục thành công");
        }

        public async Task<IResult<CategoryResponse>> CreateAsync(CategoryRequest request)
        {
            var category = new Category
            {
                Name = request.Name,
                Description = request.Description,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(category);

            var response = new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description ?? " "
            };

            return await Result<CategoryResponse>.SuccessAsync(response, "Tạo danh mục thành công");
        }

        public async Task<IResult<CategoryResponse>> UpdateAsync(CategoryUpdateRequest request)
        {
            var category = await _repository.GetByIdAsync(request.Id);
            if (category == null)
                return Result<CategoryResponse>.Failure("Không tìm thấy danh mục");

            category.Name = request.Name;
            category.Description = request.Description;
            category.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(category);

            var response = new CategoryResponse
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description ?? " "
            };

            return await Result<CategoryResponse>.SuccessAsync(response, "Cập nhật danh mục thành công");
        }

        public async Task<IResult<bool>> DeleteAsync(Guid id)
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null)
                return await Result<bool>.FailureAsync("Không tìm thấy danh mục");

            await _repository.DeleteAsync(category);
            return await Result<bool>.SuccessAsync(true, "Xóa danh mục thành công");
        }

     
        //public async Task<IEnumerable<string>> GetAllCategoryNamesAsync()
        //{
        //    var categories = await _repository.GetAllAsync();
        //    return categories.Select(c => c.Name);
        //}

    }
}
