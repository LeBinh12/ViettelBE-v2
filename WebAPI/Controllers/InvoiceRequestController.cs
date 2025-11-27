using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/invoice")]
public class InvoiceRequestController : ControllerBase
{
    private readonly IInvoiceRequestService _service;

    public InvoiceRequestController(IInvoiceRequestService service)
    {
        _service = service;
    }

    [HttpPost("request")]
    public async Task<IActionResult> CreateInvoiceRequest([FromBody] InvoiceRequestDto dto)
    {
        var result = await _service.CreateInvoiceRequestTokenAsync(dto);
        return StatusCode(result.Code,result);
    }
    
    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmInvoice([FromBody] ConfirmInvoiceRequestDto dto)
    {
        var result = await _service.ConfirmInvoiceAsync(dto);
        return StatusCode(result.Code, result);
    }
    
    [HttpPost("request-history-token")]
    public async Task<IActionResult> RequestInvoiceHistoryToken([FromBody] InvoicecCheckHistoryRequestDto dto)
    {
        var result = await _service.InvoiceCheckHistoryRequestTokenAsync(dto);
        return StatusCode(result.Code, result);
    }
    
    [HttpPost("report/{invoiceId}")]
    public async Task<IActionResult> ReportInvoice(Guid invoiceId)
    {
        var result = await _service.ReportInvoiceAsync(invoiceId);
        return StatusCode(result.Code, result);
    }

}