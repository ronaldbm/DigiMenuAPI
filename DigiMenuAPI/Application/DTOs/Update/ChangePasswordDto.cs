namespace DigiMenuAPI.Application.DTOs.Update
{
    public record ChangePasswordDto(
        int UserId,
        string CurrentPassword,
        string NewPassword
    );
}
