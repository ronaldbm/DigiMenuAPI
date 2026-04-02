using System.ComponentModel.DataAnnotations.Schema;
using AppCore.Domain.Entities;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class AccountSplitItem : BaseEntity
    {
        public int AccountSplitId { get; set; }
        public AccountSplit AccountSplit { get; set; } = null!;

        public int AccountItemId { get; set; }
        public AccountItem AccountItem { get; set; } = null!;

        [Column(TypeName = "decimal(18,3)")]
        public decimal Quantity { get; set; }
    }
}
