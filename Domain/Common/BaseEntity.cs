using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Common
{
    public abstract class BaseEntity : Entity
    {
        [NotMapped] // DB không có
        public string? CreatedBy { get; set; }

        [Column("CreateDate")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped] // DB không có
        public string? UpdatedBy { get; set; }

        [Column("UpdateDate")]
        public DateTime? UpdatedAt { get; set; }

        [Column("IsDelete")]
        public bool isDeleted { get; set; } = false;
    }
}
