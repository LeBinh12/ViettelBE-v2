using Domain.Entities;
using Share;
using Share.Interfaces;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task<Result<string>> CreateInvoiceAndGetPaymentLinkAsync(Guid customerId, Guid packageId);
    Task<Result<string>> HandlePaymentCallbackAsync(Guid invoiceId, decimal paidAmount);
    Task<Result<Invoice>> GetInvoiceByIdAsync(Guid invoiceId);
}