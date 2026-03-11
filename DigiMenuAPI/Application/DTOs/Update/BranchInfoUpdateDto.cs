namespace DigiMenuAPI.Application.DTOs.Update
{
    public record BranchInfoUpdateDto(
        int BranchId,
        string BusinessName,
        string? Tagline,
        /// <summary>Archivo de imagen del logo. Null = no cambiar.</summary>
        IFormFile? Logo,
        /// <summary>Archivo de imagen del favicon. Null = no cambiar.</summary>
        IFormFile? Favicon,
        /// <summary>Archivo de imagen de fondo. Null = no cambiar.</summary>
        IFormFile? BackgroundImage
    );
}