namespace Application.DTOs;

public class CustomerDTO
{
    public class CustomerRequestDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }

    public class CustomerResponseDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CheckEmailResponseDto
    {
        public string Email { get; set; }
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }
}