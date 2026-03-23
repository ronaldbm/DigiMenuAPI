using Serilog.Context;

namespace DigiMenuAPI.Middleware;

/// <summary>
/// Lee X-Correlation-ID del request (o genera un GUID nuevo), lo inyecta en el
/// LogContext de Serilog para que aparezca en TODOS los logs del request, y lo
/// devuelve en el response header para trazabilidad end-to-end.
///
/// Debe registrarse PRIMERO en el pipeline para que el CorrelationId esté
/// disponible incluso en los logs del GlobalExceptionMiddleware.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString("D");

        // PushProperty vive dentro del using: se elimina automáticamente al salir
        // (incluso por excepción), evitando contaminación entre threads del pool.
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // OnStarting garantiza que el header llega al cliente incluso si la
            // respuesta se corta antes (short-circuit middleware, 503, etc.)
            context.Response.OnStarting(() =>
            {
                context.Response.Headers.TryAdd(HeaderName, correlationId);
                return Task.CompletedTask;
            });

            await next(context);
        }
    }
}
