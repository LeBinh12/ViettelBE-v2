namespace Application.DTOs
{
    public class UserPagingRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        //public string? Keyword { get; set; } // tìm kiếm theo username hoặc email
    }
}
