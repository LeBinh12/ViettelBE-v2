namespace Application.DTOs;

public class CustomerRevenueDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
}