using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AppCore.Domain.Entities;
using AppCore.Infrastructure.Entities;
using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class AccountDiscount : BaseEntity
    {
        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        public int BranchDiscountId { get; set; }
        public BranchDiscount BranchDiscount { get; set; } = null!;

        /// <summary>If set, this discount applies only to this specific item. If null, applies to the whole account.</summary>
        public int? AccountItemId { get; set; }
        public AccountItem? AccountItem { get; set; }

        public DiscountType DiscountType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        public DiscountAppliesTo AppliesTo { get; set; }

        [Required, MaxLength(300)]
        public string Reason { get; set; } = null!;

        public AccountDiscountStatus Status { get; set; } = AccountDiscountStatus.Approved;

        public int? AuthorizedByUserId { get; set; }
        public AppUser? AuthorizedByUser { get; set; }
    }
}
