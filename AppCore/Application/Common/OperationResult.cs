namespace AppCore.Application.Common
{
    /// <summary>
    /// Códigos HTTP semánticos para OperationResult.
    ///
    ///   General        → 400 Bad Request
    ///   Validation     → 422 Unprocessable Entity
    ///   NotFound       → 404 Not Found
    ///   Forbidden      → 403 Forbidden
    ///   Conflict       → 409 Conflict
    ///   ModuleRequired → 403 Forbidden
    /// </summary>
    public enum OperationResultError
    {
        General = 0,
        Validation = 1,
        NotFound = 2,
        Forbidden = 3,
        Conflict = 4,
        ModuleRequired = 5
    }

    /// <summary>
    /// Resultado estándar de todas las operaciones de servicio.
    ///
    /// Incluye tres niveles de información para el frontend:
    ///   1. Success     → ¿La operación fue exitosa?
    ///   2. ErrorCode   → Tipo semántico del error (determina el HTTP status)
    ///   3. ErrorKey    → Key única para i18n (ej: "CATEGORY_NOT_FOUND")
    ///   4. Message     → Mensaje en español como fallback si no hay traducción
    ///
    /// Uso en servicios:
    ///   return OperationResult&lt;T&gt;.Ok(data);
    ///   return OperationResult&lt;T&gt;.NotFound("Categoría no encontrada.", ErrorKeys.CategoryNotFound);
    ///   return OperationResult&lt;T&gt;.Forbidden("Sin permiso.", ErrorKeys.Forbidden);
    ///   return OperationResult&lt;T&gt;.Conflict("Ya existe.", ErrorKeys.CategoryAlreadyExists);
    ///   return OperationResult&lt;T&gt;.ValidationError("Campo inválido.", ErrorKeys.ValidationFailed);
    ///   return OperationResult&lt;T&gt;.ModuleRequired(ModuleCodes.Reservations);
    ///   return OperationResult&lt;T&gt;.Fail("Error genérico.");  // backward compatible → 400
    /// </summary>
    public class OperationResult<T>
    {
        public bool Success { get; private set; }
        public string? Message { get; private set; }
        public T? Data { get; private set; }

        /// <summary>
        /// Código semántico que determina el HTTP status.
        /// Solo relevante cuando Success = false.
        /// </summary>
        public OperationResultError ErrorCode { get; private set; }

        /// <summary>
        /// Key única para i18n en el frontend.
        /// El frontend la usa para mostrar el mensaje en el idioma del usuario.
        /// Si no tiene traducción, usa Message como fallback.
        /// Solo relevante cuando Success = false.
        /// Ejemplo: "CATEGORY_NOT_FOUND", "SLUG_ALREADY_EXISTS"
        /// </summary>
        public string? ErrorKey { get; private set; }

        // ── Constructor privado — usar solo los métodos estáticos ──────
        private OperationResult() { }

        // ── Éxito ──────────────────────────────────────────────────────

        public static OperationResult<T> Ok(T data, string? message = null) =>
            new()
            {
                Success = true,
                Data = data,
                Message = message,
                ErrorCode = OperationResultError.General,
                ErrorKey = null
            };

        // ── Error genérico (400) — backward compatible ─────────────────

        /// <summary>
        /// Error genérico → 400. Sin ErrorKey.
        /// Usar solo cuando no aplica ningún tipo semántico específico.
        /// </summary>
        public static OperationResult<T> Fail(string message) =>
            new()
            {
                Success = false,
                Message = message,
                ErrorCode = OperationResultError.General,
                ErrorKey = null
            };

        // ── Error con código semántico y key i18n ──────────────────────

        public static OperationResult<T> Fail(
            string message,
            OperationResultError errorCode,
            string? errorKey = null) =>
            new()
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode,
                ErrorKey = errorKey
            };

        // ── Helpers de fábrica con nombre explícito ────────────────────

        /// <summary>Recurso no encontrado o no pertenece al tenant. → 404</summary>
        public static OperationResult<T> NotFound(string message, string errorKey) =>
            Fail(message, OperationResultError.NotFound, errorKey);

        /// <summary>Sin permiso sobre el recurso. → 403</summary>
        public static OperationResult<T> Forbidden(string message, string errorKey) =>
            Fail(message, OperationResultError.Forbidden, errorKey);

        /// <summary>Duplicado o estado inválido que genera conflicto. → 409</summary>
        public static OperationResult<T> Conflict(string message, string errorKey) =>
            Fail(message, OperationResultError.Conflict, errorKey);

        /// <summary>Regla de validación de negocio fallida. → 422</summary>
        public static OperationResult<T> ValidationError(string message, string errorKey) =>
            Fail(message, OperationResultError.Validation, errorKey);

        /// <summary>Validación fallida con datos adjuntos (ej: lista de errores por fila). → 422</summary>
        public static OperationResult<T> ValidationError(string message, string errorKey, T data) =>
            new()
            {
                Success = false,
                Message = message,
                ErrorCode = OperationResultError.Validation,
                ErrorKey = errorKey,
                Data = data
            };

        /// <summary>
        /// Módulo no activo en el plan del tenant. → 403
        /// El mensaje se genera automáticamente desde el código del módulo.
        /// </summary>
        public static OperationResult<T> ModuleRequired(string moduleCode) =>
            Fail(
                $"Tu plan no incluye el módulo '{moduleCode}'. Contacta a soporte para activarlo.",
                OperationResultError.ModuleRequired,
                ErrorKeys.ModuleRequired);
    }
}
