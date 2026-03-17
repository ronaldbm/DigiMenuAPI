using AppCore.Application.Common;
using DigiMenuAPI.Application.DTOs.Read;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Gestión de los idiomas habilitados por Company.
    ///
    /// CompanyId se extrae del JWT vía ITenantService.
    /// Solo accesible a SuperAdmin y CompanyAdmin.
    ///
    /// Reglas de negocio:
    ///   - Una Company debe tener al menos un idioma habilitado.
    ///   - No se puede eliminar el idioma por defecto si quedan más idiomas.
    ///   - Si se añade el primer idioma, queda automáticamente como default.
    ///   - SetDefault requiere que el idioma esté ya habilitado.
    /// </summary>
    public interface ICompanyLanguageService
    {
        /// <summary>
        /// Devuelve todos los idiomas del catálogo de la plataforma,
        /// con IsSelected=true y IsDefault=true para los que la Company tiene activos.
        /// </summary>
        Task<OperationResult<List<SupportedLanguageReadDto>>> GetSupportedLanguages();

        /// <summary>
        /// Devuelve solo los idiomas habilitados para la Company actual.
        /// </summary>
        Task<OperationResult<List<CompanyLanguageReadDto>>> GetCompanyLanguages();

        /// <summary>
        /// Habilita un idioma para la Company.
        /// Si es el primero que se agrega, queda como default automáticamente.
        /// </summary>
        Task<OperationResult<List<CompanyLanguageReadDto>>> AddLanguage(string code);

        /// <summary>
        /// Deshabilita un idioma para la Company.
        /// No permitido si es el único idioma o si es el default y hay otros.
        /// </summary>
        Task<OperationResult<List<CompanyLanguageReadDto>>> RemoveLanguage(string code);

        /// <summary>
        /// Marca un idioma habilitado como el idioma por defecto/fallback de la Company.
        /// </summary>
        Task<OperationResult<List<CompanyLanguageReadDto>>> SetDefault(string code);
    }
}
