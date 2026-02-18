namespace DigiMenuAPI.Application.DTOs.AddDTOs
{
    public record ReservationCreateDto(
        string CustomerName,
        string Phone,
        DateTime ReservationDate,
        TimeSpan ReservationTime,
        int PeopleCount,
        string? Comments
    );
}
