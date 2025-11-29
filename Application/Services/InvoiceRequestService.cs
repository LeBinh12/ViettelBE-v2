using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Domain.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Share;

public class InvoiceRequestService : IInvoiceRequestService
{
    private readonly IConfiguration _configuration;
    private readonly IInvoiceRepository _invoiceRepo;
    private readonly string _frontendUrl;
    private readonly IEmailService _emailService;
    private readonly ICustomerService _customerService;
    private readonly ICustomerRepository _customerRepo;
    private readonly IPaymentGateway _paymentGateway;
    private readonly InvoiceSnapshotService _snapshotService;
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceRequestService(IConfiguration configuration, IInvoiceRepository invoiceRepo,
                    IEmailService emailService, ICustomerService customerService, ICustomerRepository customerRepo,
                    IPaymentGateway paymentGateway,InvoiceSnapshotService snapshotService, IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _invoiceRepo = invoiceRepo;
        _frontendUrl = configuration["Frontend:ConfirmInvoiceUrl"] ?? "http://localhost:5175/confirm-payment";
        _emailService = emailService;
        _customerService = customerService;
        _customerRepo = customerRepo;
        _paymentGateway = paymentGateway;
        _snapshotService = snapshotService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InvoiceRequestCheckResultDto>> CreateInvoiceRequestTokenAsync(InvoiceRequestDto dto)
    {
        var result = new InvoiceRequestCheckResultDto();


            // Kiểm tra user theo email
            var existingUser = await _customerRepo.GetByEmailAsync(dto.Email);

            // Trường hợp 2: TÀI KHOẢN ĐÃ TỒN TẠI
            if (existingUser != null)
            {
                // Nếu tồn tại nhưng chưa có mật khẩu -> yêu cầu bổ sung mật khẩu
                if (string.IsNullOrEmpty(existingUser.PasswordHash))
                {
                    if (!dto.IsRegister || string.IsNullOrEmpty(dto.Password))
                    {
                        return await Result<InvoiceRequestCheckResultDto>.SuccessAsync(
                            new InvoiceRequestCheckResultDto
                            {
                                isPassword = true,
                                Token = ""
                            },
                            "Tài khoản của bạn chưa hoàn tất đăng ký. Vui lòng nhập mật khẩu để tiếp tục."
                        );
                    }

                    // Người dùng gửi password để hoàn tất đăng ký
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                    await _customerRepo.UpdateAsync(existingUser);
                }

                // Nếu tài khoản đã có password → cho phép tiếp tục
            }


            // Trường hợp 1 2: TÀI KHOẢN CHƯA TỒN TẠI 
            else
            {
                if (!dto.IsRegister || string.IsNullOrEmpty(dto.Password))
                {
                    return await Result<InvoiceRequestCheckResultDto>.SuccessAsync(
                        new InvoiceRequestCheckResultDto
                        {
                            isPassword = true,
                            Token = ""
                        },
                        "Email chưa có tài khoản. Vui lòng nhập mật khẩu để tạo tài khoản mới."
                    );
                }

                // Tạo mới user
                var newUser = new Customer
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Address = dto.Address,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
                };

                await _customerRepo.AddAsync(newUser);
            }


        // Tạo claims để lưu tất cả thông tin
        var claims = new[]
        {
            new Claim("email", dto.Email),
            new Claim("packageId", dto.PackageId.ToString()),
            new Claim("amount", dto.Amount.ToString()),
            new Claim("fullName", dto.FullName),
            new Claim("phone", dto.Phone),
            new Claim("address", dto.Address)

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
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        result.Token = tokenStr;
        result.isPassword = false;

        //  Gửi email kèm link xác nhận
        var confirmLink = $"{_frontendUrl}?token={tokenStr}";
        var subject = "Xác nhận hóa đơn của bạn";
        var html = $@"
        <p>Xin chào {dto.FullName},</p>
        <p>Bạn đã yêu cầu tạo hóa đơn với số tiền <strong>{dto.Amount}</strong>.</p>
        <p>Vui lòng bấm vào nút dưới đây để xác nhận:</p>
        <p><a href='{confirmLink}' style='padding:10px 20px; background-color:#4CAF50; color:white; text-decoration:none;'>Xác nhận hóa đơn</a></p>
        <p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
    ";
        BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(dto.Email, subject, html));

        // await _emailService.SendEmailAsync(dto.Email, subject, html);
        
        return await Result<InvoiceRequestCheckResultDto>.SuccessAsync(result, "Đã xử lý bạn cần check email");
    }

    public async Task<Result<string>> ConfirmInvoiceAsync(ConfirmInvoiceRequestDto dto)
    {
        try
        {
            //  Giải mã token
            var handler = new JwtSecurityTokenHandler();
            var tokenObj = handler.ReadJwtToken(dto.Token);

            var email = tokenObj.Claims.First(c => c.Type == "email").Value;
            var packageId = Guid.Parse(tokenObj.Claims.First(c => c.Type == "packageId").Value);
            var amount = decimal.Parse(tokenObj.Claims.First(c => c.Type == "amount").Value);
            var fullName = tokenObj.Claims.First(c => c.Type == "fullName").Value;
            var phone = tokenObj.Claims.First(c => c.Type == "phone").Value;
            var address = tokenObj.Claims.First(c => c.Type == "address").Value;


            // Kiểm tra user
            var customer = (await _customerRepo.GetAllAsync()).FirstOrDefault(c => c.Email == email);
            if (customer == null)
            {
                // Tạo user mới
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    FullName = fullName,
                    Phone = phone,
                    Address = address,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _customerRepo.AddAsync(customer);
            }
            else
            {
                // Cập nhật nếu có sự thay đổi
                var updated = false;
                if (customer.FullName != fullName)
                {
                    customer.FullName = fullName;
                    updated = true;
                }

                if (customer.Phone != phone)
                {
                    customer.Phone = phone;
                    updated = true;
                }

                if (customer.Address != address)
                {
                    customer.Address = address;
                    updated = true;
                }

                if (updated)
                {
                    customer.UpdatedAt = DateTime.UtcNow;
                    await _customerRepo.UpdateAsync(customer);
                }
            }

            // 3. Tạo hóa đơn
            var package = await _invoiceRepo.GetPackageByIdAsync(packageId);
            if (package == null)
                return Result<string>.Failure("Package not found");

            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                CustemerId = customer.Id,
                PackageId = packageId,
                Amount = amount,
                Status = InvoiceStatus.Pending,
                DueDate = DateTime.UtcNow.AddMonths(package.DurationMonths),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                BlockchainTxHash = "PENDING" // giá trị tạm

            };
            // await _snapshotService.SaveSnapshotAsync(invoice);

            await _invoiceRepo.AddInvoiceAsync(invoice);
            

            // Sinh QR có chứa invoiceId đầy đủ
            var qrUrl = _paymentGateway.GeneratePaymentLink(invoice.Id, invoice.Amount ?? 0);


            return await Result<string>.SuccessAsync(qrUrl,"Hóa đơn của bạn đã được tạo thành công!");
        }
        catch (Exception ex)
        {
            var error = ex.InnerException?.Message ?? ex.Message;
            return Result<string>.Failure("ERR: " + error);
        }
    }

    public async Task<Result<bool>> InvoiceCheckHistoryRequestTokenAsync(InvoicecCheckHistoryRequestDto dto)
    {
        var existingUser = (await _customerRepo.GetAllAsync())
            .FirstOrDefault(c => c.Email == dto.Email);
        if (existingUser == null)
            return await Result<bool>.FailureAsync("Email người dùng không tồn tại");

        if (existingUser.Phone != dto.Phone)
            return await Result<bool>.FailureAsync("Số điện thoại không hợp lệ");

        // Tạo claims để lưu tất cả thông tin
        var claims = new[]
        {
            new Claim("id", existingUser.Id.ToString()),
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
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );
        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        
        // Tạo link frontend
        var confirmLink = $"http://localhost:5175/InvoiceManagement?token={tokenStr}";
        var subject = "Xem lịch sử giao dịch của bạn";

        var html = $@"
        <p>Xin chào {existingUser.FullName},</p>
        <p>Bạn đã yêu cầu xem lịch sử giao dịch của mình.</p>
        <p>Vui lòng bấm vào nút dưới đây để xem tất cả hóa đơn của bạn (chưa thanh toán, đã thanh toán):</p>
        <p><a href='{confirmLink}' style='padding:10px 20px; background-color:#4CAF50; color:white; text-decoration:none;'>Xem lịch sử giao dịch</a></p>
        <p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
    ";

        BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(dto.Email, subject, html));

        // await _emailService.SendEmailAsync(dto.Email, subject, html);
        
        return await Result<bool>.SuccessAsync(true, "Đã xử lý bạn cần check email");
    }

    
    public async Task<Result<bool>> ReportInvoiceAsync(Guid invoiceId)
    {
        try
        {
            var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null)
                return await Result<bool>.FailureAsync("Hóa đơn không tồn tại.");

            if (invoice.IsReported)
                return await Result<bool>.FailureAsync("Hóa đơn này đã được báo cáo trước đó.");

            // Đánh dấu đã báo cáo
            invoice.IsReported = true;
            invoice.ReportedAt = DateTime.UtcNow;
            await _invoiceRepo.UpdateInvoiceAsync(invoice);
            await _unitOfWork.CommitAsync();

            // Gửi email cho Admin
            var adminEmail = _configuration["Admin:Email"];
            if (!string.IsNullOrWhiteSpace(adminEmail))
            {
                var subject = $"[Invoice Alert] Hóa đơn {invoice.Id} có dấu hiệu bị thay đổi";
                var body = $@"
                Hóa đơn {invoice.Id} do khách hàng {invoice.CustemerId} đã được phát hiện có thể bị thay đổi dữ liệu.<br/>
                Vui lòng kiểm tra và xử lý kịp thời.<br/>
                <b>Thời gian phát hiện:</b> {invoice.TamperedDetectedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")}
            ";
                BackgroundJob.Enqueue<IEmailService>(x => x.SendEmailAsync(adminEmail, subject, body));

                // await _emailService.SendEmailAsync(adminEmail, subject, body);
            }

            return await Result<bool>.SuccessAsync(true, "Báo cáo đã được gửi cho Admin.");
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
            return await Result<bool>.FailureAsync($"Lỗi khi báo cáo hóa đơn: {ex.Message}");
        }
    }

    
    private string ComputeHash(Invoice invoice)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(
            $"{invoice.Id}-{invoice.CustemerId}-{invoice.PackageId}-{invoice.Amount}-{invoice.Status}-{DateTime.UtcNow:O}");
        return Convert.ToHexString(sha.ComputeHash(bytes));
    }

}
