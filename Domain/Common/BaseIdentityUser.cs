

using Microsoft.AspNetCore.Identity;

namespace Domain.Common
{
    public abstract class AuditableIdentityUser : IdentityUser<Guid>
    {
        public string? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
