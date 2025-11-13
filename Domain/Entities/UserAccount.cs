using Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    [Table("UserAccounts")]
    public class UserAccount : AuditableIdentityUser
    {
        public string? Role { get; set; }
        public bool isDeleted { get; set; } = false;

       
        [Column("Username")]
        public override string? UserName { get; set; }

        [Column("PasswordHash")]
        public override string? PasswordHash { get; set; }

        [Column("Email")]
        public override string? Email { get; set; }
    }
}
