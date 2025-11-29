using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatisticalExportController : ControllerBase
    {
        private readonly IStatisticalExportService _exportService;

        public StatisticalExportController(IStatisticalExportService exportService)
        {
            _exportService = exportService;
        }

        // Xuất thống kê hóa đơn theo ngày
        [HttpGet("daily-summary")]
        public async Task<IActionResult> ExportDailySummary([FromQuery] DateTime? date)
        {
            var result = await _exportService.ExportDailyInvoiceSummaryAsync(date);
            if (!result.Succeeded)
                return BadRequest(result.Message);

            string fileName = $"Daily_Summary_{(date?.ToString("yyyyMMdd") ?? DateTime.UtcNow.ToString("yyyyMMdd"))}.xlsx";
            return File(result.Data, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        fileName);
        }

        // Xuất doanh thu theo tháng
        [HttpGet("monthly-revenue")]
        public async Task<IActionResult> ExportMonthlyRevenue([FromQuery] int year)
        {
            var result = await _exportService.ExportMonthlyRevenueAsync(year);
            if (!result.Succeeded)
                return BadRequest(result.Message);

            string fileName = $"Monthly_Revenue_{year}.xlsx";
            return File(result.Data, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        fileName);
        }

        // Xuất top khách hàng theo doanh thu
        [HttpGet("top-customers")]
        public async Task<IActionResult> ExportTopCustomers([FromQuery] int top = 5)
        {
            var result = await _exportService.ExportTopCustomersAsync(top);
            if (!result.Succeeded)
                return BadRequest(result.Message);

            string fileName = $"Top_{top}_Customers.xlsx";
            return File(result.Data, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        fileName);
        }

        // Xuất tổng gói dịch vụ theo category
        [HttpGet("packages-by-category")]
        public async Task<IActionResult> ExportPackagesByCategory()
        {
            var result = await _exportService.ExportPackagesByCategoryAsync();
            if (!result.Succeeded)
                return BadRequest(result.Message);

            string fileName = "Packages_By_Category.xlsx";
            return File(result.Data, 
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                        fileName);
        }
        
        // Xuất tổng số khách hàng
        [HttpGet("total-customers")]
        public async Task<IActionResult> ExportTotalCustomers()
        {
            var result = await _exportService.ExportTotalCustomersAsync();
            if (!result.Succeeded)
                return BadRequest(result.Message);

            string fileName = $"Total_Customers_{DateTime.UtcNow:yyyyMMdd}.xlsx";
            return File(result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

// Xuất số hóa đơn bị trục trặc
        [HttpGet("tampered-invoices")]
        public async Task<IActionResult> ExportTamperedInvoices()
        {
            var result = await _exportService.ExportTamperedInvoicesCountAsync();
            if (!result.Succeeded)
                return BadRequest(result.Message);

            string fileName = $"Tampered_Invoices_{DateTime.UtcNow:yyyyMMdd}.xlsx";
            return File(result.Data,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

    }
}
