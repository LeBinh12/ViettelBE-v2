using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Domain.Common;

namespace Domain.Entities
{
    public class Category : BaseEntity
    {
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        // Nếu muốn thiết lập quan hệ 1-nhiều với ServicePackage
        public List<ServicePackage>? Packages { get; set; }
        

    }
}
