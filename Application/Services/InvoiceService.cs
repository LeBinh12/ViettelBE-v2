using System.IdentityModel.Tokens.Jwt;
using Application.Interfaces;
using Domain.Entities;
using Share;
using System.Security.Cryptography;
using System.Text;
using Application.DTOs;
using Domain.Abstractions;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Nethereum.Hex.HexConvertors.Extensions;

namespace Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IBlockchainService _blockchainService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;

        public InvoiceService(IConfiguration configuration, IInvoiceRepository invoiceRepo, IPaymentGateway paymentGateway, IBlockchainService blockchainService, IUnitOfWork unitOfWork)
        {
            _invoiceRepo = invoiceRepo;
            _paymentGateway = paymentGateway;
            _blockchainService = blockchainService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }

        public async Task<Result<string>> CreateInvoiceAndGetPaymentLinkAsync(Guid customerId, Guid packageId)
        {
            try
            {
                //  Lấy gói dịch vụ từ DB
                var package = await _invoiceRepo.GetPackageByIdAsync(packageId);
                if (package == null)
                    return await Result<string>.FailureAsync("Package not found");

                // Tạo hóa đơn
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    CustemerId = customerId,
                    PackageId = packageId,
                    Amount = package.Price,
                    Status = InvoiceStatus.Pending,
                    DueDate = DateTime.UtcNow.AddMonths(package.DurationMonths),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    BlockchainTxHash = "PENDING" // giá trị tạm
                };
                
                
                // // Tính public hash và push lên blockchain
                // var publicHash = ComputePublicHash(invoice); // có thể SHA256 toàn bộ JSON
                // var txHash = await _blockchainService.PushInvoiceHashAsync(invoice.Id, publicHash);
                // invoice.AddPublicBlock(publicHash, txHash, "Polygon Testnet");
                
                await _invoiceRepo.AddInvoiceAsync(invoice);

                // Sinh QR có chứa invoiceId đầy đủ
                var qrUrl = _paymentGateway.GeneratePaymentLink(invoice.Id, invoice.Amount ?? 0);

                return await Result<string>.SuccessAsync(qrUrl, "Tạo hóa đơn và QR thanh toán thành công");
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return await Result<string>.FailureAsync($"Error: {inner}");
            }

        }

        public async Task<Result<bool>> HandlePaymentCallbackAsync(Guid invoiceId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Lấy invoice từ DB
                var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
                if (invoice == null)
                    return await Result<bool>.FailureAsync( "Invoice not found");

                // Nếu đã thanh toán rồi → return luôn
                if (invoice.Status == InvoiceStatus.Paid)
                    return await Result<bool>.SuccessAsync( true,"Invoice already paid");

                // Cập nhật trạng thái hóa đơn -> Paid
                invoice.Status = InvoiceStatus.Paid;
                invoice.LastModified = DateTime.UtcNow;
                
                // Tạo public hash cho blockchain
                string paymentHash = ComputeHash(invoice);

                // Push hash mới lên blockchain
                var txHash = await _blockchainService.PushInvoiceHashAsync(invoice.Id, paymentHash);

                invoice.BlockchainTxHash = txHash;
                invoice.BlockchainHash = paymentHash;
                invoice.BlockchainRecordedAt = DateTime.UtcNow;

                // Cập nhật hóa đơn 
                await _invoiceRepo.UpdateInvoiceAsync(invoice);
                
                await _unitOfWork.CommitAsync();

                return await Result<bool>.SuccessAsync(true,"Invoice marked as paid and blockchain updated");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return await Result<bool>.FailureAsync( $"Callback error: {ex.Message}");
            }
        }
        public async Task<Result<InvoiceWithBlockchainStatus>> GetInvoiceByIdAsync(Guid invoiceId)
        {
            try
            {
                // 1. Lấy hóa đơn từ DB
                var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
                if (invoice == null)
                    return await Result<InvoiceWithBlockchainStatus>.FailureAsync("Invoice not found");

                // Tầng 1 Kiểm tra dưới DB
                var invoiceHash = ComputeHash(invoice);
                
                if(invoice.BlockchainHash != invoiceHash) 
                    return await Result<InvoiceWithBlockchainStatus>.FailureAsync("Dữ liệu của bạn đã bị thay đổi dưới phát hiện ở DB");

                string? blockchainLatestHash = null;
                bool isMatched = false;
                
                // 2. Nếu hóa đơn đã có hash (đã push blockchain)
                if (!string.IsNullOrEmpty(invoice.BlockchainHash))
                {
                    blockchainLatestHash = await _blockchainService.GetInvoiceHashAsync(invoice.Id);

                    // 3. So sánh hash DB với blockchain
                    if (!string.IsNullOrEmpty(blockchainLatestHash))
                    {
                        isMatched = blockchainLatestHash.Equals(invoice.BlockchainHash, StringComparison.OrdinalIgnoreCase);
                        if (!isMatched) 
                            return await Result<InvoiceWithBlockchainStatus>.FailureAsync("Dữ liệu của bạn đã bị thay đổi dưới phát hiện ở Blockchain");
                    }
                }

                // 4. Trả kết quả dưới dạng DTO
                var result = new InvoiceWithBlockchainStatus
                {
                    Invoice = invoice,
                    BlockchainLatestHashOnChain = blockchainLatestHash,
                    IsBlockchainMatched = isMatched
                };

                return await Result<InvoiceWithBlockchainStatus>.SuccessAsync(result);
            }
            catch (Exception ex)
            {
                return await Result<InvoiceWithBlockchainStatus>.FailureAsync(ex.Message);
            }
        }

        public async Task<Result<List<InvoiceWithBlockchainStatus>>> GetInvoicesByCustomerAsync(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                    return await Result<List<InvoiceWithBlockchainStatus>>.FailureAsync("Token không được để trống");

                // Giải mã token
                var handler = new JwtSecurityTokenHandler();
                var tokenObj = handler.ReadJwtToken(token);

                // Lấy id từ token
                var userIdClaim = tokenObj.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                    return await Result<List<InvoiceWithBlockchainStatus>>.FailureAsync("Token không hợp lệ");

                // Lấy hóa đơn theo userId
                var invoices = await _invoiceRepo.GetInvoicesByCustomerIdAsync(userId);

                var resultList = new List<InvoiceWithBlockchainStatus>();
                var invalidDetails = new List<string>();

                foreach (var invoice in invoices)
                {
                    var item = new InvoiceWithBlockchainStatus
                    {
                        Invoice = invoice,
                        BlockchainLatestHashOnChain = null,
                        IsBlockchainMatched = false
                    };

                    // Kiểm tra blockchain nếu có
                    if (!string.IsNullOrEmpty(invoice.BlockchainHash) ||
                        !string.IsNullOrEmpty(invoice.BlockchainTxHash))
                    {
                        var currentHash = ComputeHash(invoice);

                        if (!string.Equals(currentHash, invoice.BlockchainHash, StringComparison.OrdinalIgnoreCase))
                        {
                            invalidDetails.Add($"Hóa đơn {invoice.Id} không trùng khớp với hash lưu trong Database.");
                            resultList.Add(item);
                            continue;
                        }

                        var blockchainLatestHash = await _blockchainService.GetInvoiceHashAsync(invoice.Id);
                        item.BlockchainLatestHashOnChain = blockchainLatestHash;
                        item.IsBlockchainMatched = string.Equals(blockchainLatestHash, invoice.BlockchainHash,
                            StringComparison.OrdinalIgnoreCase);

                        if (!item.IsBlockchainMatched)
                            invalidDetails.Add(
                                $"Hóa đơn {invoice.Id} không trùng khớp với hash mới nhất trên blockchain.");
                    }

                    resultList.Add(item);
                }

                return await Result<List<InvoiceWithBlockchainStatus>>.SuccessAsync(resultList,
                    "Lấy danh sách hóa đơn thành công");
            }
            catch (Exception ex)
            {
                return await Result<List<InvoiceWithBlockchainStatus>>.FailureAsync(ex.Message);
            }
        }


        private string ComputeHash(Invoice invoice)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes($"{invoice.Id}-{invoice.CustemerId}-{invoice.PackageId}-{invoice.Amount}-{invoice.Note}");
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
        
        private string ComputePublicHash(Invoice invoice)
        {
            using var sha = SHA256.Create();

            // Lấy dữ liệu cần hash (deterministic)
            var invoiceData = new
            {
                invoice.Id,
                invoice.CustemerId,
                invoice.PackageId,
                invoice.Amount,
                invoice.Status,
                invoice.DueDate,
                invoice.Note
            };

            // Serialize sang JSON (cố định thứ tự property)
            var json = System.Text.Json.JsonSerializer.Serialize(invoiceData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
        
        

        public Guid ExtractInvoiceId(string raw)
        {
            // Bỏ prefix THANHTOAN
            var idPart = raw.Replace("THANHTOAN", "");

            // Thêm dấu '-' để thành UUID hợp lệ
            string formatted = idPart.Insert(8, "-")
                .Insert(13, "-")
                .Insert(18, "-")
                .Insert(23, "-");

            return Guid.Parse(formatted);
        }
        
        private async Task AddPaymentBlockAsync(Invoice invoice)
        {
            string newHash = ComputeHash(invoice);

            // Push hash lên blockchain
            var txHash = await _blockchainService.PushInvoiceHashAsync(invoice.Id, newHash);

            

            invoice.LastModified = DateTime.UtcNow;
            invoice.Status = InvoiceStatus.Paid;
        }
        

        public Guid GetUserIdFromToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("Token không được để trống");

            var secret = _configuration["Jwt:Key"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var userIdClaim = principal.FindFirst("id")?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
                throw new SecurityTokenException("Token không hợp lệ");

            return userId;
        }

        
        
    }
}
