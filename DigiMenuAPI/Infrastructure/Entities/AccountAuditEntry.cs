using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities;

public class AccountAuditEntry
{
    [Key]
    public long Id { get; set; }

    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    [Required, MaxLength(50)]
    public string Action { get; set; } = null!;

    public int? UserId { get; set; }

    [MaxLength(200)]
    public string? UserName { get; set; }

    [MaxLength(2000)]
    public string? Details { get; set; }

    [Required, MaxLength(500)]
    public string HumanReadable { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
