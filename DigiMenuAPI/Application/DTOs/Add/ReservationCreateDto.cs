namespace DigiMenuAPI.Application.DTOs.Add
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
