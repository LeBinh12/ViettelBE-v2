using Domain.Entities;
using Domain.Enums;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace Application.DTOs;

public class CreateInvoiceDto
{
    public Guid CustomerId { get; set; }
    public Guid PackageId { get; set; }
}

public class InvoicecCheckHistoryRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

}

public class InvoiceRequestDto
{
    public string Email { get; set; } = string.Empty;
    public Guid PackageId { get; set; }
    public decimal Amount { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    
    public string? Password { get; set; } = string.Empty;
    public bool IsRegister { get; set; } = false;
}

// DTO trả về cảnh báo nếu có thông tin thay đổi
public class InvoiceRequestCheckResultDto
{
    public bool isPassword { get; set; }
    public string Token { get; set; } = string.Empty;
}

public class ConfirmInvoiceRequestDto
{
    public string Token { get; set; } = string.Empty;
}

public class ConfirmInvoiceResultDto
{
    public Guid InvoiceId { get; set; }
}


public class InvoiceWithBlockchainStatus
{
    public Invoice Invoice { get; set; } = null!;
    public string? BlockchainLatestHashOnChain { get; set; }
    public bool IsBlockchainMatched { get; set; }
}

[FunctionOutput]
public class GetLatestInvoiceHashOutputDTO : IFunctionOutputDTO
{
    [Parameter("bytes32", "", 1)]
    public byte[] Hash { get; set; }

    [Parameter("bool", "", 2)]
    public bool Exists { get; set; }
}


public class InvoiceFilterDto
{
    public Guid? InvoiceId { get; set; }
    public Guid? CustomerName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? PackageName { get; set; }
    public InvoiceStatus? Status { get; set; }

}


public class InvoiceResponseFilterDto
{
    public Guid? InvoiceId { get; set; }
    public string? CustomerName { get; set; }
    public decimal? Amount { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? PackageName { get; set; }
    public InvoiceStatus? Status { get; set; }
    public bool IsTampered { get; set; } = false;
    public DateTime CreatedAt { get; set; }

}


public class DailyInvoiceSummaryDto
{
    public DateTime Date { get; set; }
    public int TotalInvoices { get; set; }
    public decimal TotalAmount { get; set; }
}
