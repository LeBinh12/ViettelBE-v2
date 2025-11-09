using Domain.Entities;
using Application.Interfaces;
using System.Text.RegularExpressions;

namespace Application.Services
{
    public class CustomerService
    {
        private readonly ICustomerRepository _repo;

        public CustomerService(ICustomerRepository repo)
        {
            _repo = repo;
        }

        //  Kiểm tra định dạng email hợp lệ
        private bool IsEmailFormatValid(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return regex.IsMatch(email);
        }

        //  Kiểm tra email hợp lệ và chưa tồn tại
        public async Task<bool> IsEmailValidAsync(string email)
        {
            if (!IsEmailFormatValid(email))
                return false;

            var existing = await _repo.GetByEmailAsync(email);
            return existing == null;
        }

        //  Lấy toàn bộ khách hàng
        public async Task<List<Customer>> GetAllAsync() => await _repo.GetAllAsync();

        //  Thêm khách hàng mới
        public async Task AddAsync(Customer customer)
        {
            // 1️ Kiểm tra định dạng email
            if (!IsEmailFormatValid(customer.Email))
                throw new Exception("❌ Email không hợp lệ. Vui lòng nhập đúng định dạng.");

            // 2️ Kiểm tra email đã tồn tại chưa
            var existing = await _repo.GetByEmailAsync(customer.Email);
            if (existing != null)
                throw new Exception("❌ Email đã tồn tại trong hệ thống.");

            // 3️ Thiết lập thông tin mặc định
            customer.Id = Guid.NewGuid();
            customer.CreateDate = DateTime.UtcNow;
            customer.UpdateDate = DateTime.UtcNow;
            customer.IsDelete = false;

            // 4️ Lưu
            await _repo.AddAsync(customer);
        }

        //  Cập nhật khách hàng
        public async Task UpdateAsync(Customer customer)
        {
            var existing = await _repo.GetByIdAsync(customer.Id);
            if (existing == null)
                throw new Exception("❌ Không tìm thấy khách hàng.");

            // 1️ Kiểm tra định dạng email
            if (!IsEmailFormatValid(customer.Email))
                throw new Exception("❌ Email không hợp lệ.");

            // 2️ Kiểm tra email trùng (trừ chính mình)
            var duplicate = await _repo.GetByEmailAsync(customer.Email);
            if (duplicate != null && duplicate.Id != customer.Id)
                throw new Exception("❌ Email đã được sử dụng bởi khách hàng khác.");

            // 3️ Cập nhật thông tin
            existing.FullName = customer.FullName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.Address = customer.Address;
            existing.UpdateDate = DateTime.UtcNow;

            await _repo.UpdateAsync(existing);
        }

        //  Xóa khách hàng (soft delete)
        public async Task DeleteAsync(Guid id)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null)
                throw new Exception("❌ Không tìm thấy khách hàng để xóa.");

            existing.IsDelete = true;
            existing.UpdateDate = DateTime.UtcNow;
            await _repo.UpdateAsync(existing);
        }

        //  Kiểm tra email đã tồn tại (cho controller dùng check-email)
        public async Task<bool> EmailExistsAsync(string email)
        {
            if (!IsEmailFormatValid(email))
                return false;

            var existing = await _repo.GetByEmailAsync(email);
            return existing != null;
        }

        //  Kiểm tra email tồn tại cho người khác (dùng cho PUT)
        public async Task<bool> EmailExistsForOtherAsync(string email, Guid id)
        {
            if (!IsEmailFormatValid(email))
                return false;

            var existing = await _repo.GetByEmailAsync(email);
            return existing != null && existing.Id != id;
        }
    }
}
