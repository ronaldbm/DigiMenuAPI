using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AppCore.Domain.Entities;
using AppCore.Infrastructure.Entities;
using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class BranchDiscount : BaseEntity
    {
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Name { get; set; } = null!;

        public DiscountType DiscountType { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DefaultValue { get; set; }

        public DiscountAppliesTo AppliesTo { get; set; }

        public bool RequiresApproval { get; set; } = false;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MaxValueForStaff { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<AccountDiscount> AccountDiscounts { get; set; } = new List<AccountDiscount>();
    }
}
