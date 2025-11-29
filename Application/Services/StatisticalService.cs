    using Application.DTOs;
    using Application.Interfaces;
    using Domain.Abstractions;
    using Domain.Enums;
    using Share;

    namespace Application.Services;

    public class StatisticalService : IStatisticalService
    {
        private readonly IInvoiceRepository _invoiceRepo;
        private readonly ICustomerRepository _repo;
        private readonly IServicePackageRepository _repository;
        private readonly ICategoryRepository _categoryRepository;
        public StatisticalService(IInvoiceRepository invoiceRepo, ICustomerRepository repo, IServicePackageRepository repository, ICategoryRepository categoryRepository)
        {
            _invoiceRepo = invoiceRepo;
            _repo = repo;
            _repository = repository;
            _categoryRepository = categoryRepository;
        }
        
        public async Task<Result<DailyInvoiceSummaryDto>> GetDailyInvoiceSummaryAsync(DateTime? date = null)
        {
            try
            {
                // Nếu không truyền ngày thì lấy ngày hiện tại
                var targetDate = date?.Date ?? DateTime.UtcNow.Date;

                // Lấy tất cả hóa đơn trong ngày
                var invoices = await _invoiceRepo.GetAllInvoicesAsync();
                var dailyInvoices = invoices.Where(i =>
                    i.CreatedAt.Date == targetDate
                ).ToList();

                var totalAmount = dailyInvoices.Sum(i => i.Amount ?? 0);
                var totalInvoices = dailyInvoices.Count;

                var summary = new DailyInvoiceSummaryDto
                {
                    Date = targetDate,
                    TotalInvoices = totalInvoices,
                    TotalAmount = totalAmount
                };

                return await Result<DailyInvoiceSummaryDto>.SuccessAsync(summary);
            }
            catch (Exception ex)
            {
                return await Result<DailyInvoiceSummaryDto>.FailureAsync($"Lỗi khi thống kê hóa đơn: {ex.Message}");
            }
        }

        
        public async Task<Result<int>> GetTotalCustomersAsync()
        {
            var allCustomers = await _repo.GetAllAsync(); // lấy tất cả khách hàng
            var count = allCustomers.Count(c => !c.isDeleted); // chỉ tính những khách hàng chưa xóa
            return Result<int>.Success(count, "Tổng số khách hàng hệ thống.");
        }
        
        //Tổng hóa đơn
        public async Task<Result<int>> GetTotalPackagesAsync()
        {
            var packages = await _repository.GetAllAsync();
            var count = packages.Count(); // đếm tất cả gói dịch vụ
            return await Result<int>.SuccessAsync(count, "Tổng số gói dịch vụ hệ thống.");
        }

        // Tổng hóa đơn theo category
        public async Task<Result<Dictionary<string, int>>> GetTotalPackagesByCategoryAsync()
        {
            var packages = await _repository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();

            // Tạo dictionary: key = category name, value = số lượng gói
            var result = categories.ToDictionary(
                cat => cat.Name,
                cat => packages.Count(p => p.CategoryId == cat.Id)
            );

            return await Result<Dictionary<string, int>>.SuccessAsync(result, "Tổng số gói dịch vụ theo từng category.");
        }

        //Tổng package
        public async Task<Result<int>> GetTamperedInvoicesCountAsync()
        {
            try
            {
                var invoices = await _invoiceRepo.GetAllInvoicesAsync();
                var tamperedCount = invoices.Count(i => i.IsTampered);
                return await Result<int>.SuccessAsync(tamperedCount, "Số hóa đơn bị lỗi blockchain.");
            }
            catch (Exception ex)
            {
                return await Result<int>.FailureAsync($"Lỗi khi thống kê hóa đơn: {ex.Message}");
            }
        }

        // Doanh thu theo tháng
        public async Task<Result<Dictionary<string, decimal>>> GetMonthlyRevenueAsync(int year)
        {
            try
            {
                var invoices = await _invoiceRepo.GetAllInvoicesAsync();

                // Lọc theo năm
                var yearInvoices = invoices.Where(i => i.CreatedAt.Year == year).ToList();

                // Thống kê doanh thu theo tháng
                var monthlyRevenue = yearInvoices
                    .GroupBy(i => i.CreatedAt.Month)
                    .ToDictionary(
                        g => $"{g.Key:00}/{year}", // Tháng/Năm
                        g => g.Sum(i => i.Amount ?? 0) // Tổng doanh thu
                    );

                // Nếu muốn đảm bảo tất cả 12 tháng có key, dù không có doanh thu
                for (int month = 1; month <= 12; month++)
                {
                    var key = $"{month:00}/{year}";
                    if (!monthlyRevenue.ContainsKey(key))
                        monthlyRevenue[key] = 0;
                }

                return await Result<Dictionary<string, decimal>>.SuccessAsync(monthlyRevenue, "Thống kê doanh thu theo tháng thành công.");
            }
            catch (Exception ex)
            {
                return await Result<Dictionary<string, decimal>>.FailureAsync($"Lỗi khi thống kê doanh thu: {ex.Message}");
            }
        }
        
        //Top 5 khách hàng tìm năng
        public async Task<Result<List<CustomerRevenueDto>>> GetTopCustomersByRevenueAsync(int top = 5)
        {
            try
            {
                var invoices = await _invoiceRepo.GetAllInvoicesAsync();

                // Lọc hóa đơn đã thanh toán
                var paidInvoices = invoices.Where(i => i.Status == InvoiceStatus.Paid);

                // Group theo khách hàng và tính tổng doanh thu
                var customerRevenue = paidInvoices
                    .Where(i => i.Customer != null)
                    .GroupBy(i => i.CustemerId)
                    .Select(g => new CustomerRevenueDto
                    {
                        CustomerId = g.Key,
                        CustomerName = g.First().Customer?.FullName ?? "N/A",
                        TotalRevenue = g.Sum(i => i.Amount ?? 0)
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .Take(top)
                    .ToList();

                return await Result<List<CustomerRevenueDto>>.SuccessAsync(customerRevenue, "Lấy top khách hàng theo doanh thu thành công.");
            }
            catch (Exception ex)
            {
                return await Result<List<CustomerRevenueDto>>.FailureAsync($"Lỗi khi thống kê top khách hàng: {ex.Message}");
            }
        }

        
    }