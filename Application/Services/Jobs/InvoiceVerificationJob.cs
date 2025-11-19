using Application.Interfaces;
using Domain.Entities;
using System.Security.Cryptography;
using System.Text;
using Domain.Abstractions;

namespace Application.Services.Jobs
{
    public class InvoiceVerificationJob
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly IBlockchainService _blockchainService;

        public InvoiceVerificationJob(IInvoiceRepository invoiceRepo, IBlockchainService blockchainService)
        {
            _invoiceRepo = invoiceRepo;
            _blockchainService = blockchainService;
        }

        // Phương thức chạy định kỳ
        public async Task VerifyInvoicesAsync()
        {
            var invoices = await _invoiceRepo.GetAllInvoicesAsync();

            foreach (var invoice in invoices)
            {
                try
                {
                    // Tính lại public hash tại thời điểm này
                    var currentHash = ComputePublicHash(invoice);

                    // Lấy hash từ blockchain
                    var blockchainHash = await _blockchainService.GetInvoiceHashAsync(invoice.Id);

                    if (currentHash != blockchainHash)
                    {
                        // Nếu phát hiện thay đổi
                        Console.WriteLine($"[ALERT] Invoice {invoice.Id} may have been modified!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error verifying invoice {invoice.Id}: {ex.Message}");
                }
            }
        }

        private string ComputePublicHash(Invoice invoice)
        {
            using var sha = SHA256.Create();
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

            var json = System.Text.Json.JsonSerializer.Serialize(invoiceData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            var bytes = Encoding.UTF8.GetBytes(json);
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }
    }
}
