

using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nethereum.Contracts.QueryHandlers.MultiCall;
using Share;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Bảo vệ toàn bộ controller bằng JWT
public class ProfileController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public ProfileController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet("me")]
    public async Task<Result<CustomerDTO.CustomerResponseDto>> GetProfile()
    {
        // Lấy userId từ token
        var userIdClaim = User.FindFirst("id")?.Value;
        if (userIdClaim == null) return await Result<CustomerDTO.CustomerResponseDto>.FailureAsync("Token không hợp lệ");

        var userId = Guid.Parse(userIdClaim);

        var result = await _customerService.GetByIdAsync(userId);
        if (!result.Succeeded) return await Result<CustomerDTO.CustomerResponseDto>.FailureAsync(result.Message);

        return await Result<CustomerDTO.CustomerResponseDto>.SuccessAsync(result.Data,"Lấy dữ liệu thành công");
    }
}