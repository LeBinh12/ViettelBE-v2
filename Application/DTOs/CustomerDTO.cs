using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class CustomerDTO
    {
        public class CustomerRequestDto
        {
            [Required(ErrorMessage = "Họ tên không được để trống.")]
            [MaxLength(255, ErrorMessage = "Họ tên không được vượt quá 255 ký tự.")]
            public string FullName { get; set; }

            [Required(ErrorMessage = "Email không được để trống.")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
            [MaxLength(255, ErrorMessage = "Email không được vượt quá 255 ký tự.")]
            public string Email { get; set; }

            // CHỈ CHẤP NHẬN đúng 10 chữ số, KHÔNG CHẤP NHẬN chữ cái hay 11 số
            [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số và không chứa chữ cái hay ký tự khác.")]
            public string? Phone { get; set; }

            [MaxLength(255, ErrorMessage = "Địa chỉ không được vượt quá 255 ký tự.")]
            public string? Address { get; set; }
        }

        public class CustomerResponseDto
        {
            public Guid Id { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string? Phone { get; set; }
            public string? Address { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        public class CheckEmailResponseDto
        {
            public string Email { get; set; }
            public bool IsValid { get; set; }
            public string Message { get; set; }
        }
    }
    
    // DTO request cho Magic Link
    public class EmailLoginRequest
    {
        public string Email { get; set; } = null!;
    }

    // DTO request cho Email/Password
    public class EmailPasswordLoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
