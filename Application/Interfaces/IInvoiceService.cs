using Application.DTOs;
using Domain.Entities;
using Share;
using Share.Interfaces;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task<Result<string>> CreateInvoiceAndGetPaymentLinkAsync(Guid customerId, Guid packageId);
    Task<Result<bool>> HandlePaymentCallbackAsync(Guid invoiceId);
    Task<Result<InvoiceWithBlockchainStatus>> GetInvoiceByIdAsync(Guid invoiceId);
    Task<Result<List<InvoiceWithBlockchainStatus>>> GetInvoicesByCustomerAsync(string customerId);
    Task<Result<List<InvoiceWithBlockchainStatus>>> GetInvoicesByCustomerIdAsync(Guid customerId);

    Task<Result<bool>> BackupInvoiceAsync(Guid invoiceId);
    Task<Result<List<InvoiceResponseFilterDto>>> GetAllInvoicesAsync(InvoiceFilterDto? filter = null);
    
    Task<Result<byte[]>> ExportInvoiceToExcelAsync(Guid invoiceId);
}