using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.DTOs;
using Application.Interfaces;
using Domain.Abstractions;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Share;

namespace Application.Services
{
    public class UserAccountService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IConfiguration _config;

        public UserAccountService(IUserRepository repository, IConfiguration config)
        {
            _repository = repository;
            _config = config;
        }

        public Task<Result<IEnumerable<UserDto>>> GetAllUsers()
        {
            throw new NotImplementedException();
        }

        public async Task<Result<string>> RegisterAsync(string username, string email, string password)
        {
            var existingUser = await _repository.GetByUsernameAsync(username);
            if (existingUser != null)
                return Result<string>.Failure("Username đã tồn tại");

            var existingEmail = await _repository.GetByEmailAsync(email);
            if (existingEmail != null)
                return Result<string>.Failure("Email đã tồn tại");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var newUser = new UserAccount()
            {
                UserName = username,  
                Email = email,
                PasswordHash = passwordHash,
                Role = "User"
            };

            await _repository.AddAsync(newUser);
            return Result<string>.Success("Đăng ký thành công");
        }

        public async Task<Result<string>> LoginAsync(string username, string password)
        {
            var existingUser = await _repository.GetByUsernameAsync(username);
            if (existingUser == null)
                return Result<string>.Failure("Username không tồn tại");

            bool validPassword = BCrypt.Net.BCrypt.Verify(password, existingUser.PasswordHash);

            if (!validPassword)
                return Result<string>.Failure("Sai username hoặc password");

            string token = GenerateJwtToken(existingUser);

            return Result<string>.Success(token, "Đăng nhập thành công");
        }

        public async Task<Result<ValidateTokenResponse>> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            try
            {
                // Xác thực token
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _config["Jwt:Issuer"],
                    ValidAudience = _config["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                // Giải mã token để lấy thông tin
                var jwtToken = (JwtSecurityToken)validatedToken;
                var username = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Sub).Value;
                var email = jwtToken.Claims.First(x => x.Type == JwtRegisteredClaimNames.Email).Value;
                var role = jwtToken.Claims.First(x => x.Type == ClaimTypes.Role).Value;
                var id = jwtToken.Claims.First(x => x.Type == "Id").Value;

                var response = new ValidateTokenResponse
                {
                    Username = username,
                    Email = email,
                    Role = role,
                    Id = Guid.TryParse(id, out var guidValue) ? guidValue : null
                };

                return Result<ValidateTokenResponse>.Success(response, "Token hợp lệ");
            }
            catch (SecurityTokenExpiredException)
            {
                return Result<ValidateTokenResponse>.Failure("Token đã hết hạn");
            }
            catch (Exception)
            {
                return Result<ValidateTokenResponse>.Failure("Token không hợp lệ");
            }
        }

        private string GenerateJwtToken(UserAccount user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName), 
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("Id", user.Id.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // phân trang
        public async Task<Result<PagedResult<UserDto>>> GetPagedUsersAsync(int pageNumber, int pageSize)
        {
           // Lấy danh sách user từ repository
            var (users, totalCount) = await _repository.GetPagedUsersAsync(pageNumber, pageSize, null);

            var dtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.UserName,
                Role = u.Role
            });

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var result = new PagedResult<UserDto>
            {
                Items = dtos,
                TotalItems = totalCount,
                TotalPages = totalPages,
                CurrentPage = pageNumber,
                PageSize = pageSize
            };

            return Result<PagedResult<UserDto>>.Success(result, "Danh sách người dùng đã được tải");
        }

    }
}
