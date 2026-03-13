using AppCore.Application.Common;

namespace DigiMenuAPI.Application.DTOs.Read
{
    public record ReservationReadDto(
         int Id,
         int BranchId,
         string CustomerName,
         string Phone,
         DateTime ReservationDate,
         TimeSpan ReservationTime,
         int PeopleCount,
         string? TableNumber,
         string? Allergies,
         string? Comments,
         ReservationStatus Status,
         DateTime CreatedAt
     );
}
