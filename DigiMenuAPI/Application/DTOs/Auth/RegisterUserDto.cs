namespace DigiMenuAPI.Application.DTOs.Auth
{
    public record RegisterUserDto(
        string FullName,
        string Email,
        string Password,
        byte Role   // 2=Admin, 3=Staff
    );
}
