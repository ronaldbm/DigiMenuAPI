namespace DigiMenuAPI.Application.DTOs.ReadDTOs
{
    public record ReservationReadDto(
        int Id,
        string CustomerName,
        string Phone,
        DateTime ReservationDate,
        TimeSpan ReservationTime,
        int PeopleCount,
        string? Comments,
        byte Status, // 1: Pendiente, 2: Confirmada, 3: Cancelada
        DateTime CreatedAt
    );
}
