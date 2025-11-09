using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Domain.Common;

namespace Domain.Entities
{
    [Table("Customers")] 
    public class Customer : BaseEntity
    {

        [Required(ErrorMessage = "️ Họ tên không được để trống.")]
        [MaxLength(255, ErrorMessage = " Họ tên không được vượt quá 255 ký tự.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = " Email không được để trống.")]
        [EmailAddress(ErrorMessage = " Email không hợp lệ. Vui lòng nhập đúng định dạng (ví dụ: ten@gmail.com).")]
        [MaxLength(255, ErrorMessage = "️ Email không được vượt quá 255 ký tự.")]
        public string Email { get; set; }

        [MaxLength(10, ErrorMessage = " Số điện thoại không được vượt quá 10 ký tự.")]
        public string? Phone { get; set; }

        [MaxLength(255, ErrorMessage = "️ Địa chỉ không được vượt quá 255 ký tự.")]
        public string? Address { get; set; }
        
    }
}
