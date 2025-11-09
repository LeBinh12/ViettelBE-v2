using Application.Interfaces;
using Domain.Entities;
using Share;
using System.Security.Cryptography;
using System.Text;
using Domain.Abstractions;
using Domain.Enums;

namespace Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IPaymentGateway _paymentGateway;

        public InvoiceService(IInvoiceRepository invoiceRepo, IPaymentGateway paymentGateway)
        {
            _invoiceRepo = invoiceRepo;
            _paymentGateway = paymentGateway;
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
                };

                // Hash block đầu tiên
                var hash = ComputeHash(invoice);
                invoice.AddBlock("0", hash);

                //Lưu vào DB
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

        public async Task<Result<string>> HandlePaymentCallbackAsync(Guid invoiceId, decimal paidAmount)
        {
            var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
            if (invoice == null) return Result<string>.Failure("Invoice not found");

            if (paidAmount < invoice.Amount)
                return Result<string>.Failure("Payment amount is less than invoice amount");

            var previousHash = invoice.Blocks.LastOrDefault()?.CurrentHash ?? "0";
            var currentHash = ComputeHash(invoice);

            invoice.MarkAsPaid(previousHash, currentHash);

            await _invoiceRepo.UpdateInvoiceAsync(invoice);

            return Result<string>.Success("Invoice marked as paid");
        }


        public async Task<Result<Invoice>> GetInvoiceByIdAsync(Guid invoiceId)
        {
            try
            {
                var invoice = await _invoiceRepo.GetInvoiceByIdAsync(invoiceId);
                if (invoice == null) return await Result<Invoice>.FailureAsync("Invoice not found");
                return Result<Invoice>.Success(invoice);
            }
            catch (Exception ex)
            {
                return await Result<Invoice>.FailureAsync(ex);
            }
        }

        private string ComputeHash(Invoice invoice)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes($"{invoice.Id}-{invoice.CustemerId}-{invoice.PackageId}-{invoice.Amount}-{invoice.Status}-{DateTime.UtcNow:O}");
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
    }
}
