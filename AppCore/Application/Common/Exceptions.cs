// DigiMenuAPI/Application/Common/Exceptions.cs
namespace AppCore.Application.Common
{
    /// <summary>
    /// Se lanza cuando una empresa intenta usar un módulo premium
    /// que no tiene activado o que ha expirado.
    /// </summary>
    public class ModuleNotActiveException : Exception
    {
        public string ModuleCode { get; }

        public ModuleNotActiveException(string moduleCode)
            : base($"El módulo '{moduleCode}' no está activo para tu empresa.")
        {
            ModuleCode = moduleCode;
        }
    }

    /// <summary>Se lanza cuando el tenant no tiene permiso sobre un recurso.</summary>
    public class TenantAccessException : Exception
    {
        public TenantAccessException()
            : base("No tienes acceso a este recurso.") { }
    }
}
