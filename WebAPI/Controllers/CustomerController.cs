using Application.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private readonly CustomerService _service;

        public CustomerController(CustomerService service)
        {
            _service = service;
        }

        // Lấy tất cả khách hàng
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var customers = await _service.GetAllAsync();
            return Ok(customers);
        }

        // Thêm khách hàng mới (POST)
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] Customer customer)
        {
            try
            {
                // 1️⃣ Kiểm tra định dạng email
                if (string.IsNullOrEmpty(customer.Email) ||
                    !Regex.IsMatch(customer.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new { message = "❌ Email không hợp lệ. Vui lòng nhập đúng định dạng." });
                }

                // 2️⃣ Kiểm tra email đã tồn tại chưa
                bool exists = await _service.EmailExistsAsync(customer.Email);
                if (exists)
                {
                    return BadRequest(new { message = "❌ Email đã tồn tại trong hệ thống." });
                }

                // 3️⃣ Nếu hợp lệ thì thêm mới
                await _service.AddAsync(customer);
                return Ok(new { message = "✅ Thêm khách hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi thêm khách hàng: {ex.Message}" });
            }
        }

        // Cập nhật thông tin khách hàng (PUT)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Customer customer)
        {
            try
            {
                customer.Id = id;

                // 1️ Kiểm tra định dạng email
                if (string.IsNullOrEmpty(customer.Email) ||
                    !Regex.IsMatch(customer.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    return BadRequest(new { message = "❌ Email không hợp lệ. Vui lòng nhập đúng định dạng." });
                }

                // 2️ Kiểm tra email đã tồn tại (nhưng bỏ qua email của chính mình)
                bool exists = await _service.EmailExistsForOtherAsync(customer.Email, id);
                if (exists)
                {
                    return BadRequest(new { message = "❌ Email đã tồn tại cho người dùng khác." });
                }

                // 3️ Nếu hợp lệ thì cập nhật
                await _service.UpdateAsync(customer);
                return Ok(new { message = "✅ Cập nhật khách hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Lỗi khi cập nhật khách hàng: {ex.Message}" });
            }
        }

        // Xóa khách hàng (DELETE)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "🗑️ Đã xóa khách hàng." });
        }

        // Kiểm tra email hợp lệ và chưa tồn tại
        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email) ||
                !Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                return Ok(new { email, isValid = false, message = "❌ Email không hợp lệ." });
            }

            bool exists = await _service.EmailExistsAsync(email);
            if (exists)
            {
                return Ok(new { email, isValid = false, message = "❌ Email đã tồn tại." });
            }

            return Ok(new { email, isValid = true, message = "✅ Email hợp lệ và chưa tồn tại." });
        }
    }
}
