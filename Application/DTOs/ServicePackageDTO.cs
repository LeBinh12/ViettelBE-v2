using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class ServicePackageRequest
    {
        public string PackageName { get; set; } = " ";
        public decimal Price { get; set; }
        public string Description { get; set; } = " ";
        public int DurationMonths { get; set; }
        public Guid CategoryId { get; set; }
    }

    public class ServicePackageResponse
    {
        public Guid Id { get; set; }
        public string PackageName { get; set; } = " ";
        public decimal Price { get; set; }
        public string Description { get; set; } = " ";
        public int DurationMonths { get; set; }
        public string CategoryName { get; set; }
    }

    public class ServicePackageUpdateRequest : ServicePackageRequest
    {
        public Guid Id { get; set; }
    }
}
