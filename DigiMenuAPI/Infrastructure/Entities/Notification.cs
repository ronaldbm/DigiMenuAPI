using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Infrastructure.Entities;

public class Notification
{
    [Key]
    public int Id { get; set; }

    public int CompanyId { get; set; }

    public int? BranchId { get; set; }

    public int? TargetUserId { get; set; }

    [Required, MaxLength(50)]
    public string Type { get; set; } = null!;

    [Required, MaxLength(500)]
    public string Message { get; set; } = null!;

    [MaxLength(50)]
    public string? RelatedEntity { get; set; }

    public int? RelatedEntityId { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
