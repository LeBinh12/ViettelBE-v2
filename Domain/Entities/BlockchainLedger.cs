using Domain.Common;


namespace Domain.Entities
{
    public class BlockchainLedger : Entity
    {
        public Guid InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }
        // Hash public trên blockchain
        public string? PublicHash { get; set; } // nếu null => chưa push lên public blockchain
        public string? TransactionHash { get; set; } // tx hash trên blockchain public
        public string? BlockchainNetwork { get; set; } // ví dụ: "Polygon Testnet"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
