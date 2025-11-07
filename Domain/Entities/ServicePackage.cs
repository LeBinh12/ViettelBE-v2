using Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities
{
    public class ServicePackage : BaseEntity
    {
        public string PackageName { get; set; }
        public Decimal Price { get; set; }
        public string Description { get; set; }
        public int DurationMonths { get; set; }
        public Guid? CategoryId { get; set; }
        public Category? Category { get; set; }

    }
}
