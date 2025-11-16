using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Text.RegularExpressions;
using Share;

namespace Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;

        public CustomerService(ICustomerRepository repo)
        {
            _repo = repo;
        }

        private bool IsEmailFormatValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }

        public async Task<Result<List<CustomerDTO.CustomerResponseDto>>> GetAllAsync()
        {
            var customers = await _repo.GetAllAsync();
            var result = customers.Select(c => new CustomerDTO.CustomerResponseDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CreatedAt = c.CreatedAt,
            }).ToList();

            return Result<List<CustomerDTO.CustomerResponseDto>>.Success(result,"Lấy danh saách người dùng thành công");
        }

        public async Task<Result<CustomerDTO.CustomerResponseDto>> AddAsync(CustomerDTO.CustomerRequestDto dto)
        {
            if (!IsEmailFormatValid(dto.Email))
                return Result<CustomerDTO.CustomerResponseDto>.Failure("Email không hợp lệ.");

            var exists = await _repo.GetByEmailAsync(dto.Email);
            if (exists != null)
                return Result<CustomerDTO.CustomerResponseDto>.Failure("Email đã tồn tại trong hệ thống.");

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                isDeleted = false
            };

            await _repo.AddAsync(customer);

            var response = new CustomerDTO.CustomerResponseDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
            };

            return Result<CustomerDTO.CustomerResponseDto>.Success(response, "Thêm khách hàng thành công.");
        }

        public async Task<Result<CustomerDTO.CustomerResponseDto>> UpdateAsync(Guid id, CustomerDTO.CustomerRequestDto dto)
        {
            var customer = await _repo.GetByIdAsync(id);
            if (customer == null)
                return Result<CustomerDTO.CustomerResponseDto>.Failure("Không tìm thấy khách hàng.");

            if (!IsEmailFormatValid(dto.Email))
                return Result<CustomerDTO.CustomerResponseDto>.Failure("Email không hợp lệ.");

            var duplicate = await _repo.GetByEmailAsync(dto.Email);
            if (duplicate != null && duplicate.Id != id)
                return Result<CustomerDTO.CustomerResponseDto>.Failure("Email đã được sử dụng bởi khách hàng khác.");

            customer.FullName = dto.FullName;
            customer.Email = dto.Email;
            customer.Phone = dto.Phone;
            customer.Address = dto.Address;
            customer.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(customer);

            var response = new CustomerDTO.CustomerResponseDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                CreatedAt = customer.CreatedAt,
            };

            return Result<CustomerDTO.CustomerResponseDto>.Success(response, "Cập nhật khách hàng thành công.");
        }

        public async Task<Result<bool>> DeleteAsync(Guid id)
        {
            var customer = await _repo.GetByIdAsync(id);
            if (customer == null)
                return Result<bool>.Failure("Không tìm thấy khách hàng để xóa.");

            customer.isDeleted = true;
            customer.UpdatedAt = DateTime.UtcNow;
            await _repo.UpdateAsync(customer);

            return Result<bool>.Success(true, "Xóa khách hàng thành công.");
        }

        public async Task<Result<bool>> IsEmailValidAsync(string email)
        {
            if (!IsEmailFormatValid(email))
                return Result<bool>.Failure("Email không hợp lệ.");

            var exists = await _repo.GetByEmailAsync(email);
            return Result<bool>.Success(exists == null, exists == null ? "Email hợp lệ." : "Email đã tồn tại.");
        }


        public async Task<Result<PagedResult<CustomerDTO.CustomerResponseDto>>> GetPagedAsync(CustomerPagingRequestDto request)
        {
            var (items, totalItems) = await _repo.GetPagedAsync(request.Search, request.PageNumber, request.PageSize);

            var resultItems = items.Select(c => new CustomerDTO.CustomerResponseDto
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone,
                Address = c.Address,
                CreatedAt = c.CreatedAt
            }).ToList();

            var pagedResult = new PagedResult<CustomerDTO.CustomerResponseDto>
            {
                Items = resultItems,
                TotalItems = totalItems,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize)
            };

            return Result<PagedResult<CustomerDTO.CustomerResponseDto>>.Success(
                pagedResult, "Lấy danh sách phân trang thành công."
            );
        }

    }
}
