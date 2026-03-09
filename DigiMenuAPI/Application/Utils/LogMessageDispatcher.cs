using DigiMenuAPI.Application.Interfaces;
using System.Runtime.CompilerServices;
using static DigiMenuAPI.Application.Common.Constants;

namespace DigiMenuAPI.Application.Utils
{
    /// <summary>
    /// Logger estructurado con contexto de tenant automático.
    ///
    /// Todos los logs incluyen automáticamente:
    ///   CompanyId → empresa del usuario autenticado (null en contextos públicos)
    ///   BranchId  → sucursal del usuario (null para CompanyAdmin/SuperAdmin)
    ///   UserId    → usuario autenticado (null en contextos públicos)
    ///
    /// Ejemplo de salida:
    ///   [CategoryService.Create] ℹ️ La categoría ha sido creada correctamente.
    ///   ID: 42, Nombre: Entradas | Company: 7 | Branch: 3 | User: 15
    ///
    /// El contexto de tenant se resuelve desde ITenantService en cada llamada,
    /// nunca se cachea en el constructor — es Scoped y varía por request.
    /// </summary>
    public class LogMessageDispatcher<TService>
    {
        private readonly ILogger<TService> _logger;
        private readonly ITenantService _tenantService;

        public LogMessageDispatcher(
            ILogger<TService> logger,
            ITenantService tenantService)
        {
            _logger = logger;
            _tenantService = tenantService;
        }

        // ── Métodos de log ────────────────────────────────────────────

        public void LogCreate(
            EntityInfo e,
            object? entityData = null,
            [CallerMemberName] string method = "")
        {
            LogInfo(MessageBuilder.Created(e), entityData, method);
        }

        public void LogUpdate(
            EntityInfo e,
            object? entityData = null,
            [CallerMemberName] string method = "")
        {
            LogInfo(MessageBuilder.Updated(e), entityData, method);
        }

        public void LogDelete(
            EntityInfo e,
            object? entityData = null,
            [CallerMemberName] string method = "")
        {
            LogInfo(MessageBuilder.Deleted(e), entityData, method);
        }

        public void LogWarning(
            string msg,
            object? entityOrData = null,
            [CallerMemberName] string method = "")
        {
            var tenant = BuildTenantContext();
            var entity = FormatEntity(entityOrData);

            _logger.LogWarning(
                "[{Service}.{Method}] ⚠️ {Message} {Entity} {Tenant}",
                typeof(TService).Name, method, msg, entity, tenant);
        }

        public void LogError(
            Exception ex,
            string? msg = null,
            object? entityOrData = null,
            [CallerMemberName] string method = "")
        {
            var tenant = BuildTenantContext();
            var entity = FormatEntity(entityOrData);
            var finalMsg = msg ?? MessageBuilder.UnexpectedError(
                new EntityInfo("entidad", Gender.Masculine));

            _logger.LogError(
                ex,
                "[{Service}.{Method}] ❌ {Message} {Entity} {Tenant}",
                typeof(TService).Name, method, finalMsg, entity, tenant);
        }

        // ── Helpers privados ──────────────────────────────────────────

        private void LogInfo(string msg, object? entityOrData, string method)
        {
            var tenant = BuildTenantContext();
            var entity = FormatEntity(entityOrData);

            _logger.LogInformation(
                "[{Service}.{Method}] ℹ️ {Message} {Entity} {Tenant}",
                typeof(TService).Name, method, msg, entity, tenant);
        }

        /// <summary>
        /// Construye el contexto de tenant para incluir en el log.
        /// Usa TryGet para no lanzar excepciones en contextos públicos
        /// donde no hay JWT (menú público, reservas anónimas).
        /// </summary>
        private string BuildTenantContext()
        {
            try
            {
                var companyId = _tenantService.TryGetCompanyId();
                var branchId = _tenantService.TryGetBranchId();
                var userId = _tenantService.TryGetUserId(); // ← limpio, sin try/catch

                var parts = new List<string>();
                if (companyId.HasValue) parts.Add($"Company:{companyId}");
                if (branchId.HasValue) parts.Add($"Branch:{branchId}");
                if (userId.HasValue) parts.Add($"User:{userId}");

                return parts.Count > 0
                    ? $"| {string.Join(" | ", parts)}"
                    : string.Empty;
            }
            catch
            {
                // Fallback si se usa fuera de un request HTTP (background jobs, tests)
                return string.Empty;
            }
        }

        /// <summary>
        /// Intenta obtener el UserId sin lanzar excepción.
        /// TenantService.GetUserId() lanza si no hay claim — aquí necesitamos
        /// el comportamiento Try para contextos públicos.
        /// </summary>
        private int? TryGetUserId()
        {
            try { return _tenantService.GetUserId(); }
            catch { return null; }
        }

        private static string FormatEntity(object? entity)
        {
            if (entity is null) return string.Empty;

            var type = entity.GetType();

            // Tipos primitivos, strings y Guids se formatean directo
            if (type.IsPrimitive || entity is string || entity is Guid)
                return $"Valor:{entity}";

            var id = type.GetProperty("Id")?.GetValue(entity);
            var label = type.GetProperty("Label")?.GetValue(entity)
                     ?? type.GetProperty("Name")?.GetValue(entity);

            return $"ID:{id} Nombre:{label}";
        }
    }
}