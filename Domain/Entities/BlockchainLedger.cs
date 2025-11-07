using Domain.Common;


namespace Domain.Entities
{
    public class BlockchainLedger : Entity
    {
        public Guid InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public string PreviousHash { get; set; } = string.Empty;
        public string CurrentHash { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    }
}
