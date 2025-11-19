using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs
{
    public class CategoryRequest
    {
        public string Name { get; set; }
        public string Description { get; set; } = " ";
    }

    public class CategoryResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = " ";
        public string Description { get; set; } = " ";
    }

    public class CategoryUpdateRequest : CategoryRequest
    {
        public Guid Id { get; set; }
    }
}
