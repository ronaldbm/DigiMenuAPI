namespace DigiMenuAPI.Application.DTOs.Create
{
    /// <summary>El Setting se crea automáticamente al crear una Branch con valores por defecto.</summary>
    public record SettingCreateDto(
        int BranchId,
        string BusinessName
    );
}
