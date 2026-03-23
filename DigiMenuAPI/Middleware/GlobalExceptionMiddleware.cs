using AppCore.Application.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DigiMenuAPI.Middleware;

/// <summary>
/// Intercepta el 100% de excepciones no manejadas y devuelve una respuesta JSON
/// estructurada que el frontend puede procesar inmediatamente, eliminando cualquier
/// posibilidad de hang por timeouts de SQL Server u otros errores internos.
///
/// Mapeo de excepciones:
///   SqlException (timeout: numbers -2, 258 o msg de pool exhaustion) → 503 DB_UNAVAILABLE
///   SqlException (otros)                                              → 500 UNEXPECTED_ERROR
///   DbUpdateException wrapping SqlException                           → igual que SqlException
///   OperationCanceledException / TaskCanceledException                → 408 REQUEST_TIMEOUT
///   ModuleNotActiveException                                          → 403 MODULE_REQUIRED
///   TenantAccessException / UnauthorizedAccessException              → 403 FORBIDDEN
///   Cualquier otra Exception                                          → 500 UNEXPECTED_ERROR
///
/// Seguridad: ex.Message y ex.StackTrace NUNCA se envían al cliente.
/// Solo llegan a los sinks de Serilog (archivo / consola) nunca al response HTTP.
/// </summary>
public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    // Numbers de SQL Server que indican timeout / pool exhaustion.
    // -2  = client-side timeout (Connect Timeout o Command Timeout)
    // 258 = server-side wait operation timeout
    private static readonly HashSet<int> SqlTimeoutNumbers = [-2, 258];

    // Opciones estáticas: pre-allocated, cero allocations en el hot path.
    // PascalCase para coincidir con el contrato existente de BaseController.HandleResult<T>.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (statusCode, errorKey, clientMessage) = Classify(ex);

        var method   = context.Request.Method;
        var endpoint = context.GetEndpoint()?.DisplayName
                       ?? context.Request.Path.Value
                       ?? "unknown";

        // ── Logging con la severidad correcta ─────────────────────────────────
        switch (statusCode)
        {
            case StatusCodes.Status503ServiceUnavailable:
                // Transitorio — Warning, sin stack trace.
                // EF Core ya reintenta (3x con backoff). Llegar aquí significa
                // que los 3 reintentos fallaron: la BD sigue sin responder.
                logger.LogWarning(
                    "DB no disponible | {Method} {Endpoint} → {StatusCode} | " +
                    "ExType: {ExceptionType} | SqlError: {SqlErrorNumber}",
                    method, endpoint, statusCode,
                    ex.GetType().Name,
                    ExtractSqlNumber(ex));
                break;

            case StatusCodes.Status500InternalServerError:
                // Error real e inesperado — Error con stack trace completo.
                logger.LogError(
                    ex,
                    "Excepción no manejada | {Method} {Endpoint} → {StatusCode} | ExType: {ExceptionType}",
                    method, endpoint, statusCode, ex.GetType().Name);
                break;

            default:
                // 403, 408 — errores de dominio esperados. Warning sin stack trace.
                logger.LogWarning(
                    "Excepción de dominio | {Method} {Endpoint} → {StatusCode} | " +
                    "ExType: {ExceptionType}",
                    method, endpoint, statusCode, ex.GetType().Name);
                break;
        }

        // ── Guard: si el response ya empezó a escribirse no podemos modificarlo ──
        if (context.Response.HasStarted)
        {
            logger.LogWarning(
                "No se puede escribir respuesta de error — response ya iniciado. {Method} {Endpoint}",
                method, endpoint);
            return;
        }

        // ── Respuesta JSON estructurada ────────────────────────────────────────
        context.Response.Clear();
        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var body = new ErrorResponse(
            Success:   false,
            ErrorCode: errorKey,
            ErrorKey:  errorKey,
            Message:   clientMessage
        );

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
    }

    /// <summary>
    /// Clasifica la excepción y devuelve (HTTP status, errorKey, mensaje seguro para el cliente).
    /// Los mensajes internos NUNCA llegan al cliente en 500/503.
    /// </summary>
    private static (int StatusCode, string ErrorKey, string ClientMessage) Classify(Exception ex)
    {
        // Desenvuelve DbUpdateException para analizar el SqlException interno.
        if (ex is DbUpdateException { InnerException: SqlException } dbEx)
            ex = dbEx.InnerException!;

        return ex switch
        {
            // ── SQL timeout / pool exhaustion → 503 ───────────────────────────
            SqlException sql when IsSqlTimeout(sql) =>
                (StatusCodes.Status503ServiceUnavailable,
                 ErrorKeys.DbUnavailable,
                 "El servicio de base de datos no está disponible temporalmente. " +
                 "Intenta de nuevo en unos momentos."),

            // ── Otros errores SQL → 500 ───────────────────────────────────────
            SqlException =>
                (StatusCodes.Status500InternalServerError,
                 ErrorKeys.UnexpectedError,
                 "Ocurrió un error interno. Contacta a soporte si el problema persiste."),

            // ── Request cancelado por el cliente / timeout → 408 ─────────────
            // TaskCanceledException hereda de OperationCanceledException en .NET,
            // así que este case cubre ambos.
            OperationCanceledException =>
                (StatusCodes.Status408RequestTimeout,
                 ErrorKeys.RequestTimeout,
                 "La solicitud fue cancelada o tardó demasiado tiempo."),

            // ── Módulo premium no activo → 403 ───────────────────────────────
            ModuleNotActiveException =>
                (StatusCodes.Status403Forbidden,
                 ErrorKeys.ModuleRequired,
                 "Tu plan no incluye este módulo. Contacta a soporte para activarlo."),

            // ── Acceso no autorizado al tenant o recurso → 403 ───────────────
            TenantAccessException or UnauthorizedAccessException =>
                (StatusCodes.Status403Forbidden,
                 ErrorKeys.Forbidden,
                 "No tienes permiso para realizar esta acción."),

            // ── Cualquier otra excepción → 500 ────────────────────────────────
            _ =>
                (StatusCodes.Status500InternalServerError,
                 ErrorKeys.UnexpectedError,
                 "Ocurrió un error interno. Contacta a soporte si el problema persiste.")
        };
    }

    private static bool IsSqlTimeout(SqlException ex)
    {
        // 1. Números estándar de timeout
        if (SqlTimeoutNumbers.Contains(ex.Number)) return true;

        // 2. SqlClient a veces reporta Number=0 con mensaje de timeout
        if (ex.Number == 0 && ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return true;

        // 3. Pool exhaustion durante la fase de login (el error exacto del reporte del usuario)
        //    "[Tras el inicio de sesión] completo=29013" → ambas frases presentes en el mensaje
        if (ex.Message.Contains("inicio de sesión", StringComparison.OrdinalIgnoreCase)
            && ex.Message.Contains("tiempo de espera", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Extrae solo el número de error SQL para el log — nunca el mensaje completo
    /// (que podría contener texto de consultas o datos sensibles).
    /// </summary>
    private static string ExtractSqlNumber(Exception ex) => ex switch
    {
        SqlException sql                                    => $"Number={sql.Number}",
        DbUpdateException { InnerException: SqlException s} => $"Number={s.Number}",
        _                                                   => "N/A"
    };

    // DTO privado — no forma parte del contrato público de la API
    private sealed record ErrorResponse(
        bool   Success,
        string ErrorCode,
        string ErrorKey,
        string Message
    );
}
