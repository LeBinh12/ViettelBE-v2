using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatisticalController : ControllerBase
{
    private readonly IStatisticalService _statisticalService;

    public StatisticalController(IStatisticalService statisticalService)
    {
        _statisticalService = statisticalService;
    }
    
    // Tổng số khách hàng
    [HttpGet("total-customers")]
    public async Task<IActionResult> GetTotalCustomers()
    {
        var result = await _statisticalService.GetTotalCustomersAsync();
        return StatusCode(result.Code, result);
    }

    // Tổng số gói dịch vụ
    [HttpGet("total-packages")]
    public async Task<IActionResult> GetTotalPackages()
    {
        var result = await _statisticalService.GetTotalPackagesAsync();
        return StatusCode(result.Code, result);
    }

    // Tổng số gói theo category
    [HttpGet("total-packages-by-category")]
    public async Task<IActionResult> GetTotalPackagesByCategory()
    {
        var result = await _statisticalService.GetTotalPackagesByCategoryAsync();
        return StatusCode(result.Code, result);
    }

    // Số hóa đơn gặp lỗi blockchain
    [HttpGet("tampered-invoices-count")]
    public async Task<IActionResult> GetTamperedInvoicesCount()
    {
        var result = await _statisticalService.GetTamperedInvoicesCountAsync();
        return StatusCode(result.Code, result);
    }

    // Doanh thu theo tháng
    [HttpGet("monthly-revenue/{year}")]
    public async Task<IActionResult> GetMonthlyRevenue(int year)
    {
        var result = await _statisticalService.GetMonthlyRevenueAsync(year);
        return StatusCode(result.Code, result);
    }

    // Top 5 khách hàng có doanh thu cao nhất
    [HttpGet("top-customers")]
    public async Task<IActionResult> GetTopCustomers([FromQuery] int top = 5)
    {
        var result = await _statisticalService.GetTopCustomersByRevenueAsync(top);
        return StatusCode(result.Code, result);
    }
    
    [HttpGet("daily-summary")]
    public async Task<IActionResult> GetDailyInvoiceSummary([FromQuery] string? date)
    {
        DateTime? targetDate = null;
        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateTime.TryParse(date, out var parsedDate))
                return BadRequest("Ngày không hợp lệ, định dạng yyyy-MM-dd");

            targetDate = parsedDate;
        }

        var result = await _statisticalService.GetDailyInvoiceSummaryAsync(targetDate);
        return StatusCode(result.Code, result);

    }
}