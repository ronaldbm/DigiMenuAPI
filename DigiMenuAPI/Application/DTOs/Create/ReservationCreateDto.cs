using System.ComponentModel.DataAnnotations;

namespace DigiMenuAPI.Application.DTOs.Create
{
    public record ReservationCreateDto(
        [Range(1, int.MaxValue)] int BranchId,
        [Required, MaxLength(100)] string CustomerName,
        [Required, MaxLength(20)] string Phone,
        DateTime ReservationDate,
        TimeSpan ReservationTime,
        [Range(1, 100)] int PeopleCount,
        [MaxLength(20)] string? TableNumber,
        [MaxLength(500)] string? Allergies,
        [MaxLength(500)] string? Comments
    );
}
