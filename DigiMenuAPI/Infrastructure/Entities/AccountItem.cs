using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AppCore.Domain.Entities;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class AccountItem : BaseEntity
    {
        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public int BranchProductId { get; set; }
        public BranchProduct BranchProduct { get; set; } = null!;

        /// <summary>Snapshot of the product name at the time the item was added.</summary>
        [Required, MaxLength(200)]
        public string ProductName { get; set; } = null!;

        /// <summary>Snapshot of BranchProduct.OfferPrice ?? Price at the time the item was added.</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; } = 1;

        [MaxLength(300)]
        public string? Notes { get; set; }

        public ICollection<AccountSplitItem> SplitItems { get; set; } = new List<AccountSplitItem>();
        public ICollection<AccountDiscount>  Discounts  { get; set; } = new List<AccountDiscount>();
    }
}
