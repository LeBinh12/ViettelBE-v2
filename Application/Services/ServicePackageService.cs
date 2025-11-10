using Application.DTOs;
using Application.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Share;
using Share.Interfaces;

namespace Application.Services
{
    public class ServicePackageService : IServicePackageService
    {
        private readonly IServicePackageRepository _repository;
        private readonly ICategoryRepository _categoryRepository;

        private ServicePackageResponse MapToResponse(ServicePackage package)
        {
            return new ServicePackageResponse
            {
                Id = package.Id,
                PackageName = package.PackageName,
                Price = package.Price,
                Description = package.Description,
                DurationMonths = package.DurationMonths,
                CategoryName = package.Category?.Name ?? string.Empty
            };
        }

        public ServicePackageService(IServicePackageRepository repository, ICategoryRepository categoryRepository)
        {
            _repository = repository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IResult<IEnumerable<ServicePackageResponse>>> GetAllAsync()
        {
            var packages = await _repository.GetAllAsync();

            var response = packages.Select(MapToResponse);

            return Result<IEnumerable<ServicePackageResponse>>.Success(response, "Lấy danh sách gói dịch vụ thành công");
        }

        public async Task<IResult<ServicePackageResponse>> GetByIdAsync(Guid id)
        {
            var package = await _repository.GetByIdAsync(id);
            if (package == null)
                return Result<ServicePackageResponse>.Failure("Không tìm thấy gói dịch vụ");

            var response = MapToResponse(package);

            return Result<ServicePackageResponse>.Success(response, "Lấy chi tiết gói dịch vụ thành công");
        }

        public async Task<IResult<ServicePackageResponse>> CreateAsync(ServicePackageRequest request)
        {
            if (request.CategoryId != Guid.Empty)
            {
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
                if (category == null)
                    return Result<ServicePackageResponse>.Failure("Loại dịch vụ không tồn tại");
            }

            var entity = new ServicePackage
            {
                PackageName = request.PackageName,
                Price = request.Price,
                Description = request.Description,
                DurationMonths = request.DurationMonths,
                CategoryId = request.CategoryId
            };

            await _repository.AddAsync(entity);

            var response = MapToResponse(entity);

            return Result<ServicePackageResponse>.Success(response, "Tạo gói dịch vụ thành công");
        }


        public async Task<IResult<ServicePackageResponse>> UpdateAsync(ServicePackageUpdateRequest request)
        {
            var package = await _repository.GetByIdAsync(request.Id);
            if (package == null)
                return Result<ServicePackageResponse>.Failure("Không tìm thấy gói dịch vụ");

            if (request.CategoryId != Guid.Empty)
            {
                var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
                if (category == null)
                    return Result<ServicePackageResponse>.Failure("Loại dịch vụ không tồn tại");
            }

            package.PackageName = request.PackageName;
            package.Price = request.Price;
            package.Description = request.Description;
            package.DurationMonths = request.DurationMonths;
            package.CategoryId = request.CategoryId;
            package.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(package);

            var response = MapToResponse(package);

            return Result<ServicePackageResponse>.Success(response, "Cập nhật gói dịch vụ thành công");
        }


        public async Task<IResult<bool>> DeleteAsync(Guid id)
        {
            var package = await _repository.GetByIdAsync(id);
            if (package == null)
                return Result<bool>.Failure("Không tìm thấy gói dịch vụ");

            await _repository.DeleteAsync(package);
            return Result<bool>.Success(true, "Xóa gói dịch vụ thành công");
        }
    }
}
