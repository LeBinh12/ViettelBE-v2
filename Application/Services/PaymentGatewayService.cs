using System.Security.Cryptography;
using System.Text;
using Application.Interfaces;

namespace Application.Services
{
    public class PaymentGatewayService : IPaymentGateway
    {
        private readonly string _bankCode = "970436"; // Mã ngân hàng (VD: Vietcombank)
        private readonly string _accountNo = "1025981176";
        private readonly string _accountName = "LE PHUOC BINH";

        // API VietQR miễn phí
        private readonly string _vietQrApi = "https://img.vietqr.io/image";

        public string GeneratePaymentLink(Guid invoiceId, decimal amount)
        {
            // Nội dung chuyển khoản (dễ tra lại khi webhook đến)
            var content = $"THANHTOAN-{invoiceId}";

            // URL ảnh QR VietQR
            var qrUrl = $"{_vietQrApi}/{_bankCode}-{_accountNo}-qr_only.png?amount={amount}&addInfo={content}&accountName={_accountName}";

            //  Ở đây có thể lưu lại nội dung "content" để mapping callback sau
            return qrUrl;
        }

        public bool VerifyCallback(Dictionary<string, string> callbackParams)
        {
            // Nếu bạn dùng AutoBank / TBank gửi webhook về, có thể xác thực chữ ký ở đây
            return true;
        }
    }
}