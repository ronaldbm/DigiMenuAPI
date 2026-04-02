using System.ComponentModel.DataAnnotations;
using AppCore.Domain.Entities;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class AccountSplit : BaseEntity
    {
        public int AccountId { get; set; }
        public Account Account { get; set; } = null!;

        [Required, MaxLength(100)]
        public string SplitName { get; set; } = null!;

        public ICollection<AccountSplitItem> Items { get; set; } = new List<AccountSplitItem>();
    }
}
