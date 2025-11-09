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

        // Sửa lại kiểu dữ liệu để EF Core nhận dạng đúng navigation property
        public List<BlockchainLedger> Blocks { get; set; } = new List<BlockchainLedger>();

        public void AddBlock(string previousHash, string currentHash)
        {
            Blocks.Add(new BlockchainLedger
            {
                InvoiceId = this.Id,
                PreviousHash = previousHash,
                CurrentHash = currentHash,
                CreateDate = DateTime.UtcNow
            });
        }

        public void MarkAsPaid(string previousHash, string currentHash)
        {
            Status = InvoiceStatus.Paid;
            AddBlock(previousHash, currentHash);
        }
    }
}