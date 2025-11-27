using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Share;

namespace Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;
        private readonly IEmailService _emailService;
        private readonly string _frontendUrl;
        private readonly IConfiguration _configuration;

        public CustomerService(ICustomerRepository repo, IConfiguration configuration, IEmailService emailService)
        {
            _repo = repo;
            _frontendUrl = configuration["Frontend:ConfirmInvoiceUrl"] ?? "http://localhost:5175/login";
            _emailService = emailService;
            _configuration = configuration;

        }

        // Authentication JWT + Email Magic
        public async Task<Result<string>> LoginByEmailMagicLinkAsync(string email)
        {
            // Kiểm tra user theo email
            var user = await _repo.GetByEmailAsync(email);
            if (user == null)
            {
                // Tạo user tạm nếu chưa tồn tại
                user = new Customer
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _repo.AddAsync(user);
            }

            // Tạo claims cho JWT
            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("fullName", user.FullName ?? "")

            };

            var secret = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("JWT Key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                throw new InvalidOperationException("JWT Issuer or Audience is missing.");

            // Tạo JWT
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            // Gửi link đăng nhập qua email
            var loginLink = $"{_frontendUrl}?token={tokenStr}";
            var subject = "Đăng nhập bằng email";
            var html = $@"
        <p>Xin chào,</p>
        <p>Bạn đã yêu cầu đăng nhập. Vui lòng click vào link bên dưới để đăng nhập:</p>
        <p><a href='{loginLink}' style='padding:10px 20px; background-color:#4CAF50; color:white; text-decoration:none;'>Đăng nhập</a></p>
        <p>Link chỉ có hiệu lực trong 30 phút.</p>
    ";

            await _emailService.SendEmailAsync(email, subject, html);

            return await Result<string>.SuccessAsync("Check email", "Link đăng nhập đã được gửi vào email của bạn.");
        }

        // Authentication Thuần 
        public async Task<Result<string>> LoginByEmailPasswordAsync(string email, string password)
        {
            if (!IsEmailFormatValid(email))
                return await Result<string>.FailureAsync("Email không hợp lệ.");

            var user = await _repo.GetByEmailAsync(email);
            if (user == null)
                return await Result<string>.FailureAsync("Email không tồn tại trong hệ thống.");

            if (string.IsNullOrEmpty(user.PasswordHash))
                return await Result<string>.FailureAsync(
                    "Tài khoản này chưa thiết lập mật khẩu. Vui lòng dùng magic link để đăng nhập.");

            // Kiểm tra mật khẩu
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return await Result<string>.FailureAsync("Mật khẩu không đúng.");

            // Tạo claims
            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("fullName", user.FullName ?? "")
            };

            var secret = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(secret))
                throw new InvalidOperationException("JWT Key is not configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            if (string.IsNullOrEmpty(issuer) || string.IsNullOrEmpty(audience))
                throw new InvalidOperationException("JWT Issuer or Audience is missing.");

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), // token hợp lệ 2h
                signingCredentials: creds
            );

            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            return await Result<string>.SuccessAsync(tokenStr, "Đăng nhập thành công.");
        }
        
        public async Task<Result<CustomerDTO.CustomerResponseDto>> GetByIdAsync(Guid id)
        {
            var customer = await _repo.GetByIdAsync(id);
            if (customer == null) return Result<CustomerDTO.CustomerResponseDto>.Failure("Không tìm thấy user.");

            var response = new CustomerDTO.CustomerResponseDto
            {
                Id = customer.Id,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                CreatedAt = customer.CreatedAt
            };

            return await Result<CustomerDTO.CustomerResponseDto>.SuccessAsync(response);
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

            return Result<List<CustomerDTO.CustomerResponseDto>>.Success(result,
                "Lấy danh saách người dùng thành công");
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

        public async Task<Result<CustomerDTO.CustomerResponseDto>> UpdateAsync(Guid id,
            CustomerDTO.CustomerRequestDto dto)
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


        public async Task<Result<PagedResult<CustomerDTO.CustomerResponseDto>>> GetPagedAsync(
            CustomerPagingRequestDto request)
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
