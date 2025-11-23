using Domain.Entities;
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
    public bool IsChange { get; set; }
}

// DTO trả về cảnh báo nếu có thông tin thay đổi
public class InvoiceRequestCheckResultDto
{
    public bool HasChanges { get; set; }
    public List<string> ChangedFields { get; set; } = new();
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