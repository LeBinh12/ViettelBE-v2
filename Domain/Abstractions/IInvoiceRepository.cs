using Domain.Entities;

namespace Domain.Abstractions;

public interface IInvoiceRepository
{
    // Invoice
    Task<IEnumerable<Invoice>> GetAllInvoicesAsync();
    Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId);
    Task AddInvoiceAsync(Invoice invoice);
    Task UpdateInvoiceAsync(Invoice invoice);
    Task<IEnumerable<Invoice>> GetInvoicesByCustomerIdAsync(Guid customerId);

    // ServicePackage
    Task<ServicePackage?> GetPackageByIdAsync(Guid packageId);
    Task<IEnumerable<ServicePackage>> GetAllPackagesAsync();
    

    // Blockchain Ledger
    Task AddBlockToInvoiceAsync(Guid invoiceId, BlockchainLedger block);
    Task<IEnumerable<BlockchainLedger>> GetBlockchainByInvoiceIdAsync(Guid invoiceId);
}