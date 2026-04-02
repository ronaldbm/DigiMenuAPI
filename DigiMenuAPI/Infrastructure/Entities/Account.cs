using System.ComponentModel.DataAnnotations;
using AppCore.Domain.Entities;
using AppCore.Infrastructure.Entities;
using DigiMenuAPI.Application.Common.Enums;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Account : BaseEntity
    {
        public int BranchId { get; set; }
        public Branch Branch { get; set; } = null!;

        [Required, MaxLength(200)]
        public string ClientIdentifier { get; set; } = null!;

        public AccountStatus Status { get; set; } = AccountStatus.Open;

        [MaxLength(500)]
        public string? Notes { get; set; }

        public int? TabAuthorizedByUserId { get; set; }
        public AppUser? TabAuthorizedByUser { get; set; }

        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public ICollection<AccountItem>     Items     { get; set; } = new List<AccountItem>();
        public ICollection<AccountDiscount> Discounts { get; set; } = new List<AccountDiscount>();
        public ICollection<AccountSplit>    Splits    { get; set; } = new List<AccountSplit>();
    }
}
