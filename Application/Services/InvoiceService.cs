using System.IdentityModel.Tokens.Jwt;
using Application.Interfaces;
using Domain.Entities;
using Share;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Application.DTOs;
using ClosedXML.Excel;
using Domain.Abstractions;
using Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;


namespace Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPaymentGateway _paymentGateway;
        private readonly IBlockchainService _blockchainService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        private readonly InvoiceSnapshotService _snapshotService;

        public InvoiceService(IConfiguration configuration, IInvoiceRepository invoiceRepo,
            IPaymentGateway paymentGateway, IBlockchainService blockchainService, IUnitOfWork unitOfWork,
            InvoiceSnapshotService snapshotService)
        {
            _invoiceRepo = invoiceRepo;
            _paymentGateway = paymentGateway;
            _blockchainService = blockchainService;
            _unitOfWork = unitOfWork;
            _configuration = configuration;
            _snapshotService = snapshotService;
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
                await _snapshotService.SaveSnapshotAsync(invoice);

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
                    return await Result<bool>.FailureAsync("Invoice not found");

                // Nếu đã thanh toán rồi → return luôn
                if (invoice.Status == InvoiceStatus.Paid)
                    return await Result<bool>.SuccessAsync(true, "Invoice already paid");

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
                // Lưu vào snapshot để xử lý backup dữ liệu
                await _snapshotService.SaveSnapshotAsync(invoice);


                return await Result<bool>.SuccessAsync(true, "Invoice marked as paid and blockchain updated");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return await Result<bool>.FailureAsync($"Callback error: {ex.Message}");
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
                
                string? blockchainLatestHash = null;
                bool isMatched = false;
                
                if (invoice.BlockchainHash != null)
                {
                    // Tầng 1 Kiểm tra dưới DB
                    var invoiceHash = ComputeHash(invoice);

                    if (invoice.BlockchainHash != invoiceHash)
                    {
                        invoice.IsTampered = true;
                        invoice.TamperedDetectedAt = DateTime.UtcNow;
                        await _invoiceRepo.UpdateInvoiceAsync(invoice);
                        await _unitOfWork.CommitAsync();
                        var response = new InvoiceWithBlockchainStatus
                        {
                            Invoice = invoice,
                            BlockchainLatestHashOnChain = "",
                            IsBlockchainMatched = true,
                        };
                        return await Result<InvoiceWithBlockchainStatus>.SuccessAsync(response,
                            "Hóa đơn của bạn có tể đang bị thay đổi, cần báo ngay cho quản trị hệ thống để sớm khôi phục!");
                    }

                    // 2. Nếu hóa đơn đã có hash (đã push blockchain)
                    if (!string.IsNullOrEmpty(invoice.BlockchainHash))
                    {
                        blockchainLatestHash = await _blockchainService.GetInvoiceHashAsync(invoice.Id);

                        // 3. So sánh hash DB với blockchain
                        if (!string.IsNullOrEmpty(blockchainLatestHash))
                        {
                            isMatched = blockchainLatestHash.Equals(invoice.BlockchainHash,
                                StringComparison.OrdinalIgnoreCase);
                            if (!isMatched)
                            {

                                invoice.IsTampered = true;
                                invoice.TamperedDetectedAt = DateTime.UtcNow;
                                await _invoiceRepo.UpdateInvoiceAsync(invoice);
                                await _unitOfWork.CommitAsync();

                                var response = new InvoiceWithBlockchainStatus
                                {
                                    Invoice = invoice,
                                    BlockchainLatestHashOnChain = "",
                                    IsBlockchainMatched = true,
                                };
                                return await Result<InvoiceWithBlockchainStatus>.SuccessAsync(response,
                                    "Hóa đơn của bạn có tể đang bị thay đổi, cần báo ngay cho quản trị hệ thống để sớm khôi phục!");
                            }
                        }
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


        public async Task<Result<List<InvoiceWithBlockchainStatus>>> GetInvoicesByCustomerIdAsync(Guid customerId)
        {
            try
            {

                // Lấy hóa đơn theo userId
                var invoices = await _invoiceRepo.GetInvoicesByCustomerIdAsync(customerId);

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

        public async Task<Result<bool>> BackupInvoiceAsync(Guid invoiceId)
        {
            try
            {
                // Lấy snapshot
                var snapshot = await _snapshotService.LoadSnapshotAsync(invoiceId);
                if (snapshot == null)
                    return await Result<bool>.FailureAsync("Snapshot của hóa đơn không tồn tại.");

                // Lấy hóa đơn hiện tại trong DB
                var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
                if (invoice == null)
                    return await Result<bool>.FailureAsync("Hóa đơn không tồn tại.");

                // Restore dữ liệu từ snapshot
                invoice.Amount = snapshot.Amount;
                invoice.Status = snapshot.Status;
                invoice.DueDate = snapshot.DueDate;
                invoice.Note = snapshot.Note;
                invoice.PackageId = snapshot.PackageId;
                invoice.BlockchainHash = snapshot.BlockchainHash;
                invoice.BlockchainTxHash = snapshot.BlockchainTxHash;
                invoice.BlockchainRecordedAt = snapshot.BlockchainRecordedAt;
                invoice.LastModified = DateTime.UtcNow;
                invoice.IsTampered = false;
                invoice.TamperedDetectedAt = null;
                invoice.IsReported = false;

                await _invoiceRepo.UpdateInvoiceAsync(invoice);
                await _unitOfWork.CommitAsync();

                return await Result<bool>.SuccessAsync(true, "Backup hóa đơn thành công.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackAsync();
                return await Result<bool>.FailureAsync($"Lỗi khi backup hóa đơn: {ex.Message}");
            }
        }

        public async Task<Invoice?> RestoreInvoiceFromSnapshotAsync(Guid invoiceId)
        {
            var path = Path.Combine("invoice_snapshots", $"{invoiceId}.json");
            if (!File.Exists(path)) return null;

            var json = await File.ReadAllTextAsync(path);
            var originalInvoice = JsonSerializer.Deserialize<Invoice>(json);

            if (originalInvoice == null) return null;

            await _invoiceRepo.UpdateInvoiceAsync(originalInvoice);

            return originalInvoice;
        }

        public async Task<Result<List<InvoiceResponseFilterDto>>> GetAllInvoicesAsync(InvoiceFilterDto? filter = null)
        {
            try
            {
                // Lấy tất cả hóa đơn kèm Customer & Package
                var invoices = await _invoiceRepo.GetAllInvoicesAsync();

                if (filter != null)
                {
                    if (filter.InvoiceId.HasValue)
                        invoices = invoices.Where(i => i.Id == filter.InvoiceId.Value).ToList();

                    if (filter.CustomerName.HasValue)
                        invoices = invoices.Where(i => i.CustemerId == filter.CustomerName.Value).ToList();

                    if (!string.IsNullOrWhiteSpace(filter.Email))
                        invoices = invoices.Where(i =>
                            i.Customer != null &&
                            i.Customer.Email.Contains(filter.Email, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (!string.IsNullOrWhiteSpace(filter.Phone))
                        invoices = invoices.Where(i =>
                            i.Customer != null && i.Customer.Phone != null &&
                            i.Customer.Phone.Contains(filter.Phone, StringComparison.OrdinalIgnoreCase)).ToList();

                    if (filter.PackageName.HasValue)
                        invoices = invoices.Where(i => i.PackageId == filter.PackageName.Value).ToList();

                    if (filter.Status.HasValue)
                        invoices = invoices.Where(i => i.Status == filter.Status.Value).ToList();
                }

                // Mapping sang DTO
                var result = invoices.Select(i => new InvoiceResponseFilterDto
                {
                    InvoiceId = i.Id,
                    CustomerName = i.Customer?.FullName,
                    Amount = i.Amount,
                    Email = i.Customer?.Email,
                    Phone = i.Customer?.Phone,
                    PackageName = i.Package?.PackageName,
                    Status = i.Status,
                    IsTampered = i.IsTampered,
                    CreatedAt = i.CreatedAt
                }).ToList();

                return await Result<List<InvoiceResponseFilterDto>>.SuccessAsync(result,
                    "Lấy danh sách hóa đơn thành công");
            }
            catch (Exception ex)
            {
                return await Result<List<InvoiceResponseFilterDto>>.FailureAsync(ex.Message);
            }
        }

        
        public async Task<Result<DailyInvoiceSummaryDto>> GetDailyInvoiceSummaryAsync(DateTime? date = null)
        {
            try
            {
                // Nếu không truyền ngày thì lấy ngày hiện tại
                var targetDate = date?.Date ?? DateTime.UtcNow.Date;

                // Lấy tất cả hóa đơn trong ngày
                var invoices = await _invoiceRepo.GetAllInvoicesAsync();
                var dailyInvoices = invoices.Where(i =>
                    i.CreatedAt.Date == targetDate
                ).ToList();

                var totalAmount = dailyInvoices.Sum(i => i.Amount ?? 0);
                var totalInvoices = dailyInvoices.Count;

                var summary = new DailyInvoiceSummaryDto
                {
                    Date = targetDate,
                    TotalInvoices = totalInvoices,
                    TotalAmount = totalAmount
                };

                return await Result<DailyInvoiceSummaryDto>.SuccessAsync(summary, "Lấy tống kê hoóa đơn thành công!");
            }
            catch (Exception ex)
            {
                return await Result<DailyInvoiceSummaryDto>.FailureAsync($"Lỗi khi thống kê hóa đơn: {ex.Message}");
            }
        }


        /// <summary>
        ///  XUất hóa đơn
        /// </summary>

        public async Task<Result<byte[]>> ExportInvoiceToExcelAsync(Guid invoiceId)
{
    try
    {
        // Lấy hóa đơn chi tiết kèm Customer + Package
        var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
        if (invoice == null)
            return await Result<byte[]>.FailureAsync("Không tìm thấy hóa đơn.");

        // Tạo workbook mới
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Hóa Đơn");

        // Thiết lập style đẹp
        worksheet.PageSetup.Margins.Top = 0.5;
        worksheet.PageSetup.Margins.Bottom = 0.5;
        worksheet.PageSetup.Margins.Left = 0.5;
        worksheet.PageSetup.Margins.Right = 0.5;
        worksheet.PageSetup.PaperSize = XLPaperSize.A4Paper;
// Thay thế cho SetScaleToFit() - Fit toàn bộ nội dung vào 1 trang
        worksheet.PageSetup.PagesTall = 1;
        worksheet.PageSetup.PagesWide = 1;
        
        // Tiêu đề công ty
        worksheet.Cell(1, 1).Value = "Công ty 5 Thành Viên VIETDEV";
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.Red;
        worksheet.Cell(2, 1).Value = "Địa chỉ: 783 Phạm Hữu Lầu, Phường 6, Cao Lãnh, Đồng Tháp";
        worksheet.Cell(3, 1).Value = "Hotline: 0328075014 | Email: lephuocbinh.2000@gmail.com";
        worksheet.Cell(4, 1).Value = "Website: https://cnkt.dthu.edu.vn/";

        // Tiêu đề hóa đơn
        worksheet.Cell(6, 1).Value = "HÓA ĐƠN DỊCH VỤ INTERNET & TRUYỀN HÌNH";
        worksheet.Cell(6, 1).Style.Font.Bold = true;
        worksheet.Cell(6, 1).Style.Font.FontSize = 20;
        worksheet.Cell(6, 1).Style.Font.FontColor = XLColor.DarkBlue;
        worksheet.Cell(6, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range(6, 1, 6, 6).Merge();

        // Thông tin hóa đơn
        int row = 8;
        worksheet.Cell(row, 1).Value = "Mã hóa đơn:";
        worksheet.Cell(row, 2).Value = invoice.Id.ToString();
        worksheet.Cell(row, 2).Style.Font.Bold = true;

        row++;
        worksheet.Cell(row, 1).Value = "Ngày tạo:";
        worksheet.Cell(row, 2).Value = invoice.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss");
        worksheet.Cell(row, 2).Style.Font.Bold = true;

        row++;
        worksheet.Cell(row, 1).Value = "Hạn thanh toán:";
        worksheet.Cell(row, 2).Value = invoice.DueDate.ToString("dd/MM/yyyy");
        worksheet.Cell(row, 2).Style.Font.Bold = true;

        row++;
        worksheet.Cell(row, 1).Value = "Trạng thái:";
        var statusText = invoice.Status == InvoiceStatus.Paid ? "ĐÃ THANH TOÁN" : "CHƯA THANH TOÁN";
        var statusCell = worksheet.Cell(row, 2);
        statusCell.Value = statusText;
        statusCell.Style.Font.Bold = true;
        statusCell.Style.Font.FontColor = invoice.Status == InvoiceStatus.Paid 
            ? XLColor.Green 
            : XLColor.Red;

        // Thông tin khách hàng
        row += 2;
        worksheet.Cell(row, 1).Value = "THÔNG TIN KHÁCH HÀNG";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Range(row, 1, row, 6).Merge();

        row++;
        worksheet.Cell(row, 1).Value = "Họ tên:";
        worksheet.Cell(row, 2).Value = invoice.Customer?.FullName ?? "Chưa có thông tin";
        worksheet.Cell(row, 2).Style.Font.Bold = true;

        row++;
        worksheet.Cell(row, 1).Value = "Email:";
        worksheet.Cell(row, 2).Value = invoice.Customer?.Email ?? "N/A";

        row++;
        worksheet.Cell(row, 1).Value = "Số điện thoại:";
        worksheet.Cell(row, 2).Value = invoice.Customer?.Phone ?? "N/A";

        row++;
        worksheet.Cell(row, 1).Value = "Địa chỉ:";
        worksheet.Cell(row, 2).Value = invoice.Customer?.Address ?? "Chưa cập nhật";

        // Thông tin gói dịch vụ
        row += 2;
        worksheet.Cell(row, 1).Value = "THÔNG TIN GÓI DỊCH VỤ";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGray;
        worksheet.Range(row, 1, row, 6).Merge();

        row++;
        worksheet.Cell(row, 1).Value = "Tên gói:";
        worksheet.Cell(row, 2).Value = invoice.Package?.PackageName ?? "Không xác định";
        worksheet.Cell(row, 2).Style.Font.Bold = true;

        row++;
        worksheet.Cell(row, 1).Value = "Thời hạn:";
        worksheet.Cell(row, 2).Value = $"{invoice.Package?.DurationMonths ?? 0} tháng";

        row++;
        worksheet.Cell(row, 1).Value = "Giá tiền:";
        var priceCell = worksheet.Cell(row, 2);
        priceCell.Value = invoice.Amount?.ToString("N0") + " VNĐ" ?? "0 VNĐ";
        priceCell.Style.Font.Bold = true;
        priceCell.Style.Font.FontColor = XLColor.Red;
        priceCell.Style.Font.FontSize = 16;

        // Blockchain verification
        row += 2;
        worksheet.Cell(row, 1).Value = "XÁC THỰC BLOCKCHAIN";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.FontSize = 14;
        worksheet.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.LightGreen;
        worksheet.Range(row, 1, row, 6).Merge();

        row++;
        worksheet.Cell(row, 1).Value = "Trạng thái bảo mật:";
        var securityCell = worksheet.Cell(row, 2);
        if (invoice.IsTampered == true)
        {
            securityCell.Value = "CẢNH BÁO: DỮ LIỆU CÓ THỂ BỊ THAY ĐỔI!";
            securityCell.Style.Font.FontColor = XLColor.Red;
        }
        else if (!string.IsNullOrEmpty(invoice.BlockchainTxHash) && invoice.BlockchainTxHash != "PENDING")
        {
            securityCell.Value = "ĐÃ ĐƯỢC XÁC THỰC TRÊN BLOCKCHAIN";
            securityCell.Style.Font.FontColor = XLColor.Green;
        }
        else
        {
            securityCell.Value = "Chưa ghi nhận trên blockchain";
            securityCell.Style.Font.FontColor = XLColor.Orange;
        }
        securityCell.Style.Font.Bold = true;

        row++;
        if (!string.IsNullOrEmpty(invoice.BlockchainTxHash) && invoice.BlockchainTxHash != "PENDING")
        {
            worksheet.Cell(row, 1).Value = "TxHash (Polygon):";
            worksheet.Cell(row, 2).Value = invoice.BlockchainTxHash;
            worksheet.Cell(row, 2).Style.Font.FontName = "Consolas";
            worksheet.Cell(row, 2).Style.Font.FontSize = 10;
        }

        // Footer
        row += 3;
        worksheet.Cell(row, 1).Value = "Cảm ơn Quý khách đã sử dụng dịch vụ VietDev!";
        worksheet.Cell(row, 1).Style.Font.Bold = true;
        worksheet.Cell(row, 1).Style.Font.Italic = true;
        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range(row, 1, row, 6).Merge();

        row++;
        worksheet.Cell(row, 1).Value = "Hóa đơn được tạo tự động bởi hệ thống - Không cần đóng dấu";
        worksheet.Cell(row, 1).Style.Font.Italic = true;
        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Range(row, 1, row, 6).Merge();

        // Điều chỉnh cột
        worksheet.Columns(1, 6).AdjustToContents();

        // Xuất ra byte[]
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        var fileBytes = stream.ToArray();

        return await Result<byte[]>.SuccessAsync(fileBytes, "Xuất hóa đơn thành công");
    }
    catch (Exception ex)
    {
        return await Result<byte[]>.FailureAsync($"Lỗi khi xuất Excel: {ex.Message}");
    }
}

        private string ComputeHash(Invoice invoice)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(
                $"{invoice.Id}-{invoice.CustemerId}-{invoice.PackageId}-{invoice.Amount}-{invoice.Note}");
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
