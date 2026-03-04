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
        byte Status, // 1: Pendiente, 2: Confirmada, 3: Cancelada
         DateTime CreatedAt
     );
}
