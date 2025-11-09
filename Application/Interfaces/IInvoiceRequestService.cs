using Application.DTOs;
using Share;

namespace Application.Interfaces;

public interface IInvoiceRequestService
{
    Task<Result<InvoiceRequestCheckResultDto>> CreateInvoiceRequestTokenAsync(InvoiceRequestDto dto);
    Task<Result<ConfirmInvoiceResultDto>> ConfirmInvoiceAsync(ConfirmInvoiceRequestDto dto);
}