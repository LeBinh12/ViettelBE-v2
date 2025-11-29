using Application.DTOs;
using Share;

namespace Application.Interfaces;

public interface IStatisticalService
{
    Task<Result<int>> GetTotalCustomersAsync();
    Task<Result<DailyInvoiceSummaryDto>> GetDailyInvoiceSummaryAsync(DateTime? date = null);
    Task<Result<int>> GetTotalPackagesAsync();
    Task<Result<Dictionary<string, int>>> GetTotalPackagesByCategoryAsync();
    Task<Result<int>> GetTamperedInvoicesCountAsync();
    Task<Result<Dictionary<string, decimal>>> GetMonthlyRevenueAsync(int year);
    Task<Result<List<CustomerRevenueDto>>> GetTopCustomersByRevenueAsync(int top = 5);
    
}