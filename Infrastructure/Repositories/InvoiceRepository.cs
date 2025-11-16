using Microsoft.EntityFrameworkCore;
using Domain.Abstractions;
using Domain.Entities;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public InvoiceRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ---------- Invoice ----------
        
        public async Task<IEnumerable<Invoice>> GetAllInvoicesAsync()
        {
            return await _dbContext.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Package)
                .Include(i => i.Blocks)
                .ToListAsync();
        }
        
        public async Task AddInvoiceAsync(Invoice invoice)
        {
            await _dbContext.Invoices.AddAsync(invoice);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(Guid invoiceId)
        {
            return await _dbContext.Invoices
                .Include(i => i.Customer)
                .Include(i => i.Package)
                .Include(i => i.Blocks)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            var existing = await _dbContext.Invoices.FirstOrDefaultAsync(x => x.Id == invoice.Id);
            if (existing == null) throw new Exception("Invoice not found");

            _dbContext.Entry(existing).CurrentValues.SetValues(invoice);
            await _dbContext.SaveChangesAsync();
        }


        public async Task<IEnumerable<Invoice>> GetInvoicesByCustomerIdAsync(Guid customerId)
        {
            return await _dbContext.Invoices
                .Include(i => i.Package)
                .Include(i => i.Blocks)
                .Where(i => i.CustemerId == customerId)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Invoice>> GetInvoicesToVerifyAsync()
        {
            // lấy incremental hoặc tất cả để verify blockchain
            return await _dbContext.Invoices.Include(i => i.Blocks).ToListAsync();
        }

        // ---------- ServicePackage ----------
        public async Task<ServicePackage?> GetPackageByIdAsync(Guid packageId)
        {
            return await _dbContext.ServicePackages
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == packageId);
        }

        public async Task<IEnumerable<ServicePackage>> GetAllPackagesAsync()
        {
            return await _dbContext.ServicePackages.Include(p => p.Category).ToListAsync();
        }

        // ---------- BlockchainLedger ----------
        public async Task AddBlockchainLedgerAsync(BlockchainLedger block)
        {
            await _dbContext.BlockchainLedgers.AddAsync(block);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IEnumerable<BlockchainLedger>> GetBlockchainByInvoiceIdAsync(Guid invoiceId)
        {
            return await _dbContext.BlockchainLedgers
                .Where(b => b.InvoiceId == invoiceId)
                .OrderBy(b => b.CreatedAt)
                .ToListAsync();
        }

    }