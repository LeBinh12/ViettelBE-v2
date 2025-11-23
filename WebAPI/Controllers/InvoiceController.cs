using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/invoice")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPaymentGateway _paymentGateway;

        public InvoiceController(IInvoiceService invoiceService, IPaymentGateway paymentGateway)
        {
            _invoiceService = invoiceService;
            _paymentGateway = paymentGateway;
        }

        [HttpPost("create-payment")]
        public async Task<IActionResult> CreatePayment([FromBody] CreateInvoiceDto dto)
        {
            var result = await _invoiceService.CreateInvoiceAndGetPaymentLinkAsync(dto.CustomerId, dto.PackageId);
            return StatusCode(result.Code, result);
        }

        // Callback từ AutoBank/TBank webhook
        [HttpPost("payment-callback")]
        public async Task<IActionResult> PaymentCallback([FromBody] PaymentCallbackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Desc))
                return BadRequest("Missing desc");

            // Loại bỏ tiền tố THANHTOAN- và gạch
            var idPart = dto.Desc.Replace("THANHTOAN", "").Replace("-", "").Trim();

            if (!Guid.TryParseExact(idPart, "N", out var invoiceId))
                return BadRequest("InvoiceId not found");

            // Tìm hóa đơn trong DB
            var result = await _invoiceService.HandlePaymentCallbackAsync(invoiceId);

            return StatusCode(result.Code, result);
        }



        [HttpGet("{invoiceId}")]
        public async Task<IActionResult> GetInvoice(Guid invoiceId)
        {
            var result = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
            return StatusCode(result.Code, result);
        }
        
        [HttpGet("get-by-customer/{token}")]
        public async Task<IActionResult> GetByCustomerInvoice(string token)
        {
            var result = await _invoiceService.GetInvoicesByCustomerAsync(token);
            return StatusCode(result.Code, result);
        }
    }
}
