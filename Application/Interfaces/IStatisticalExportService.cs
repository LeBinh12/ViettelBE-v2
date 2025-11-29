using Share;

namespace Application.Interfaces;

public interface IStatisticalExportService
{
    Task<Result<byte[]>> ExportDailyInvoiceSummaryAsync(DateTime? date = null);
    Task<Result<byte[]>> ExportMonthlyRevenueAsync(int year);
    Task<Result<byte[]>> ExportTopCustomersAsync(int top = 5);
    Task<Result<byte[]>> ExportPackagesByCategoryAsync();
    Task<Result<byte[]>> ExportTotalCustomersAsync();
    Task<Result<byte[]>> ExportTamperedInvoicesCountAsync();

}