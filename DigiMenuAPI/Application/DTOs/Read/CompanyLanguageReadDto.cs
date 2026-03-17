namespace DigiMenuAPI.Application.DTOs.Read
{
    /// <summary>
    /// Idioma habilitado para una Company.
    /// </summary>
    public record CompanyLanguageReadDto(
        string Code,
        string Name,
        string Flag,
        bool IsDefault
    );

    /// <summary>
    /// Idioma soportado por la plataforma, con indicador de si la Company lo tiene activo.
    /// Se usa en el panel admin para mostrar toggles de habilitación.
    /// </summary>
    public record SupportedLanguageReadDto(
        string Code,
        string Name,
        string Flag,
        int DisplayOrder,
        bool IsActive,
        bool IsSelected,   // true si la Company ya tiene este idioma habilitado
        bool IsDefault     // true si este es el idioma principal de la Company
    );
}
