namespace DigiMenuAPI.Application.DTOs.Create
{
    public record ReservationCreateDto(
        int BranchId,
        string CustomerName,
        string Phone,
        DateTime ReservationDate,
        TimeSpan ReservationTime,
        int PeopleCount,
        string? TableNumber,
        string? Allergies,
        string? Comments
    );
}
