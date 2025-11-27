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
        
        public string? BlockchainHash { get; set; } // có thể là PublicHash hoặc TxHash

        public string BlockchainTxHash { get; set; }          
        public DateTime? BlockchainRecordedAt { get; set; }  
        
        /// <summary>
        ///     Lưu trạng thái cho Admin biết
        /// </summary>
        public bool IsTampered { get; set; } = false;
        public DateTime? TamperedDetectedAt { get; set; }

        /// <summary>
        ///     Đánh dấu là hóa đơn đã được báo cáo
        /// </summary>
        public bool IsReported { get; set; } = false;
        public DateTime? ReportedAt { get; set; }


            
    }
}