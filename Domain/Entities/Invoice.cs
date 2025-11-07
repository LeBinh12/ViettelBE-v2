using Domain.Enums;
using System;
using System.Collections.Generic;
using Domain.Common;


namespace Domain.Entities
{
    public  class Invoice : BaseEntity
    {
        public Decimal? Amount { get; set; }
        public Guid CustemerId { get; set; }
        public Customer? Customer { get; set; }

        public Guid PackageId { get; set; }
        public ServicePackage? Package { get; set; }

        public DateTime DueDate { get; set; }
        public string? Note { get; set; }
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Pending;

    }
}
