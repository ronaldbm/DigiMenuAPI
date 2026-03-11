using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.DTOs.Create;
using DigiMenuAPI.Application.DTOs.Read;
using DigiMenuAPI.Application.DTOs.Update;

namespace DigiMenuAPI.Application.Interfaces
{
    /// <summary>
    /// Gestión de usuarios del tenant autenticado.
    ///
    /// Reglas de acceso:
    ///   CompanyAdmin → ve y gestiona todos los usuarios de su empresa.
    ///   BranchAdmin  → ve y gestiona solo los usuarios de su Branch.
    ///   Staff        → sin acceso a gestión de usuarios.
    ///
    /// Reglas de negocio:
    ///   - No se puede superar Company.MaxUsers al crear (-1 = ilimitado).
    ///   - Un usuario no puede editar su propio rol ni eliminarse a sí mismo.
    ///   - Solo se puede asignar roles de menor jerarquía (UserRoles.CanAssign).
    ///   - Soft delete: IsDeleted = true, nunca eliminación física.
    ///   - Al crear un usuario se genera contraseña temporal y se envía por email.
    ///   - Resetear contraseña genera nueva temporal y reenvía email.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Lista usuarios de la empresa. BranchAdmin solo ve su Branch.
        /// </summary>
        Task<OperationResult<List<AppUserSummaryDto>>> GetAll();

        /// <summary>
        /// Detalle completo de un usuario. Valida que pertenece al tenant autenticado.
        /// </summary>
        Task<OperationResult<AppUserReadDto>> GetById(int userId);

        /// <summary>
        /// Crea un usuario con contraseña temporal y envía email de bienvenida.
        /// CompanyId se toma del JWT.
        /// </summary>
        Task<OperationResult<AppUserReadDto>> Create(AppUserCreateDto dto);

        /// <summary>
        /// Edita nombre, email y Branch asignada.
        /// No permite cambiar rol ni CompanyId.
        /// </summary>
        Task<OperationResult<AppUserReadDto>> Update(AppUserUpdateDto dto);

        /// <summary>
        /// Activa o desactiva un usuario.
        /// Un usuario no puede desactivarse a sí mismo.
        /// </summary>
        Task<OperationResult<bool>> ToggleActive(int userId);

        /// <summary>
        /// Soft delete. Un usuario no puede eliminarse a sí mismo.
        /// </summary>
        Task<OperationResult<bool>> Delete(int userId);

        /// <summary>
        /// Genera nueva contraseña temporal, activa MustChangePassword
        /// y envía email al usuario con la nueva contraseña.
        /// </summary>
        Task<OperationResult<bool>> ResetPassword(int userId);
    }
}