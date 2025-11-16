using Domain.Enums;
using System;
using System.Collections.Generic;
using Domain.Common;

namespace Domain.Entities
{
    public class Invoice : BaseEntity
    {
        public decimal? Amount { get; set; }
        public Guid CustemerId { get; set; }
        public Customer? Customer { get; set; }

        public Guid PackageId { get; set; }
        public ServicePackage? Package { get; set; }

        public DateTime DueDate { get; set; }
        public string? Note { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;
        public DateTime LastModified { get; set; } = DateTime.UtcNow;

        // Sửa lại kiểu dữ liệu để EF Core nhận dạng đúng navigation property
        public List<BlockchainLedger> Blocks { get; set; } = new List<BlockchainLedger>();

        public void AddBlock(string publicHash, string txHash, string network)
        {
            Blocks.Add(new BlockchainLedger
            {
                InvoiceId = this.Id,
                PublicHash = publicHash,
                TransactionHash = txHash,
                BlockchainNetwork = network,
                CreatedAt = DateTime.UtcNow
            });
            LastModified = DateTime.UtcNow;
        }
        
    }
}