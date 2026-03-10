namespace DigiMenuAPI.Application.DTOs.Email
{
    public record ReservationConfirmationEmailDto(
        string ToEmail,
        string ClientName,
        string BusinessName,
        DateTime ReservationDate,
        string ReservationTime,
        int GuestCount,
        string? Notes,
        string? BusinessPhone,
        string? BusinessAddress
    );
}