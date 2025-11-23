using Application.DTOs;
using Share;

namespace Application.Interfaces;

public interface IInvoiceRequestService
{
    Task<Result<InvoiceRequestCheckResultDto>> CreateInvoiceRequestTokenAsync(InvoiceRequestDto dto);
    Task<Result<string>> ConfirmInvoiceAsync(ConfirmInvoiceRequestDto dto);
    Task<Result<bool>> InvoiceCheckHistoryRequestTokenAsync(InvoicecCheckHistoryRequestDto dto);
}