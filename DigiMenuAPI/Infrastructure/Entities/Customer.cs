using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AppCore.Domain.Entities;

namespace DigiMenuAPI.Infrastructure.Entities
{
    public class Customer : BaseEntity
    {
        // ── Multi-Tenant ─────────────────────────────────────────────
        public int CompanyId { get; set; }
        public Company Company { get; set; } = null!;

        // ── Identity ─────────────────────────────────────────────────
        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // ── Credit / Tab Limits ──────────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        public decimal CreditLimit { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentBalance { get; set; } = 0;

        public int MaxOpenTabs { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxTabAmount { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        // ── Navigation ───────────────────────────────────────────────
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
