using Domain.Entities;
using Share;
using Share.Interfaces;

namespace Application.Interfaces;

public interface IInvoiceService
{
    Task<Result<string>> CreateInvoiceAndGetPaymentLinkAsync(Guid customerId, Guid packageId);
    Task<Result<bool>> HandlePaymentCallbackAsync(Guid invoiceId);
    Task<Result<Invoice>> GetInvoiceByIdAsync(Guid invoiceId);
}