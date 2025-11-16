namespace Application.Interfaces;

public interface IBlockchainService
{
    Task<string> PushInvoiceHashAsync(Guid invoiceId, string hash);
    Task<string> GetInvoiceHashAsync(Guid invoiceId);
}
