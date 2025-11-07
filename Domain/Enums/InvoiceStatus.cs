using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Enums
{
    public enum InvoiceStatus
    {
        Pending = 0,     // Chờ thanh toán
        Paid = 1,        // Đã thanh toán
        Cancelled = 2,   // Đã hủy
        Overdue = 3,     // Quá hạn
        Refunded = 4     // Đã hoàn tiền
    }
}
