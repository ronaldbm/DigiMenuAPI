namespace DigiMenuAPI.Application.DTOs.Update
{
    /// <summary>
    /// Edición de datos básicos de una Branch.
    /// IsActive se maneja por separado con PATCH /toggle-active
    /// para evitar ambigüedad entre edición y cambio de estado.
    /// </summary>
    public record BranchUpdateDto(
        int Id,
        string Name,
        string Slug,
        string? Address,
        string? Phone,
        string? Email
    );
}