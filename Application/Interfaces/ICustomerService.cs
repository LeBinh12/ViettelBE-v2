using Application.DTOs;
using Domain.Entities;
using Share;

namespace Application.Interfaces
{
    public interface ICustomerService
    {
        Task<Result<List<CustomerDTO.CustomerResponseDto>>> GetAllAsync();
        Task<Result<CustomerDTO.CustomerResponseDto>> AddAsync(CustomerDTO.CustomerRequestDto dto);
        Task<Result<CustomerDTO.CustomerResponseDto>> UpdateAsync(Guid id, CustomerDTO.CustomerRequestDto dto);
        Task<Result<bool>> DeleteAsync(Guid id);
        Task<Result<bool>> IsEmailValidAsync(string email);

        Task<Result<PagedResult<CustomerDTO.CustomerResponseDto>>> GetPagedAsync(CustomerPagingRequestDto request);

        Task<Result<string>> LoginByEmailMagicLinkAsync(string email);
        Task<Result<CustomerDTO.CustomerResponseDto>> GetByIdAsync(Guid id);
        Task<Result<string>> LoginByEmailPasswordAsync(string email, string password);

    }
}

