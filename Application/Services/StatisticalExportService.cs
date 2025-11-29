using Application.DTOs;
using Application.Interfaces;
using ClosedXML.Excel;
using Share;

namespace Application.Services;

public class StatisticalExportService : IStatisticalExportService
{
    private readonly IStatisticalService _statisticalService;

    public StatisticalExportService(IStatisticalService statisticalService)
    {
        _statisticalService = statisticalService;
    }

    // Xuất thống kê doanh thu theo ngày
    public async Task<Result<byte[]>> ExportDailyInvoiceSummaryAsync(DateTime? date = null)
    {
        var summaryResult = await _statisticalService.GetDailyInvoiceSummaryAsync(date);
        if (!summaryResult.Succeeded) return await Result<byte[]>.FailureAsync(summaryResult.Message);

        var summary = summaryResult.Data;

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Daily Summary");

        ws.Cell(1, 1).Value = "Thống kê hóa đơn ngày";
        ws.Cell(2, 1).Value = "Ngày:";
        ws.Cell(2, 2).Value = summary.Date.ToString("dd/MM/yyyy");
        ws.Cell(3, 1).Value = "Tổng số hóa đơn:";
        ws.Cell(3, 2).Value = summary.TotalInvoices;
        ws.Cell(4, 1).Value = "Tổng doanh thu:";
        ws.Cell(4, 2).Value = summary.TotalAmount;

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Result<byte[]>.SuccessAsync(stream.ToArray(), "Xuất thống kê ngày thành công");
    }

    // Xuất thống kê doanh thu theo tháng
    public async Task<Result<byte[]>> ExportMonthlyRevenueAsync(int year)
    {
        var result = await _statisticalService.GetMonthlyRevenueAsync(year);
        if (!result.Succeeded) return await Result<byte[]>.FailureAsync(result.Message);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Monthly Revenue");

        ws.Cell(1, 1).Value = "Tháng/Năm";
        ws.Cell(1, 2).Value = "Doanh thu";

        int row = 2;
        foreach (var kv in result.Data.OrderBy(x => x.Key))
        {
            ws.Cell(row, 1).Value = kv.Key;
            ws.Cell(row, 2).Value = kv.Value;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Result<byte[]>.SuccessAsync(stream.ToArray(), "Xuất doanh thu tháng thành công");
    }

    // Xuất Top khách hàng
    public async Task<Result<byte[]>> ExportTopCustomersAsync(int top = 5)
    {
        var result = await _statisticalService.GetTopCustomersByRevenueAsync(top);
        if (!result.Succeeded) return await Result<byte[]>.FailureAsync(result.Message);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Top Customers");

        ws.Cell(1, 1).Value = "STT";
        ws.Cell(1, 2).Value = "Tên khách hàng";
        ws.Cell(1, 3).Value = "Doanh thu";

        int row = 2;
        int index = 1;
        foreach (var customer in result.Data)
        {
            ws.Cell(row, 1).Value = index++;
            ws.Cell(row, 2).Value = customer.CustomerName;
            ws.Cell(row, 3).Value = customer.TotalRevenue;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Result<byte[]>.SuccessAsync(stream.ToArray(), "Xuất top khách hàng thành công");
    }

    // Xuất tổng gói dịch vụ theo category
    public async Task<Result<byte[]>> ExportPackagesByCategoryAsync()
    {
        var result = await _statisticalService.GetTotalPackagesByCategoryAsync();
        if (!result.Succeeded) return await Result<byte[]>.FailureAsync(result.Message);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Packages by Category");

        ws.Cell(1, 1).Value = "Danh mục";
        ws.Cell(1, 2).Value = "Số lượng gói";

        int row = 2;
        foreach (var kv in result.Data)
        {
            ws.Cell(row, 1).Value = kv.Key;
            ws.Cell(row, 2).Value = kv.Value;
            row++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Result<byte[]>.SuccessAsync(stream.ToArray(), "Xuất gói dịch vụ theo category thành công");
    }
    
    // Xuất tổng số khách hàng
    public async Task<Result<byte[]>> ExportTotalCustomersAsync()
    {
        var result = await _statisticalService.GetTotalCustomersAsync();
        if (!result.Succeeded) return await Result<byte[]>.FailureAsync(result.Message);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Total Customers");

        ws.Cell(1, 1).Value = "Tổng số khách hàng:";
        ws.Cell(1, 2).Value = result.Data;

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Result<byte[]>.SuccessAsync(stream.ToArray(), "Xuất tổng số khách hàng thành công");
    }

// Xuất số hóa đơn bị trục trặc
    public async Task<Result<byte[]>> ExportTamperedInvoicesCountAsync()
    {
        var result = await _statisticalService.GetTamperedInvoicesCountAsync();
        if (!result.Succeeded) return await Result<byte[]>.FailureAsync(result.Message);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Tampered Invoices");

        ws.Cell(1, 1).Value = "Số hóa đơn bị trục trặc:";
        ws.Cell(1, 2).Value = result.Data;

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return await Result<byte[]>.SuccessAsync(stream.ToArray(), "Xuất số hóa đơn bị trục trặc thành công");
    }

}
