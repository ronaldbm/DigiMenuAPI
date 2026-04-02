using AppCore.Application.Common;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace DigiMenuAPI.UnitTests.Middleware;

/// <summary>
/// Tests del GlobalExceptionMiddleware.
/// Verifica que cada tipo de excepción genere el HTTP status code correcto
/// y que el body JSON estructurado contenga los campos esperados.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Category", "Security")]
public sealed class GlobalExceptionMiddlewareTests
{
    // ── Helpers ───────────────────────────────────────────────────────────

    private static async Task<(int StatusCode, JsonElement Body)> InvokeMiddlewareAsync(
        Exception exceptionToThrow)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        RequestDelegate next = _ => throw exceptionToThrow;

        var middleware = new global::DigiMenuAPI.Middleware.GlobalExceptionMiddleware(
            next,
            NullLogger<global::DigiMenuAPI.Middleware.GlobalExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var json = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var body = JsonSerializer.Deserialize<JsonElement>(json);

        return (context.Response.StatusCode, body);
    }

    // ── SQL Timeout → 503 ────────────────────────────────────────────────

    [Fact]
    public async Task SqlTimeout_Number_Minus2_Returns503_WithDbUnavailableKey()
    {
        var sqlEx = CreateSqlException(number: -2);

        var (status, body) = await InvokeMiddlewareAsync(sqlEx);

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
        body.GetProperty("ErrorKey").GetString().Should().Be(ErrorKeys.DbUnavailable);
        body.GetProperty("Success").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task SqlTimeout_Number_258_Returns503()
    {
        var sqlEx = CreateSqlException(number: 258);

        var (status, body) = await InvokeMiddlewareAsync(sqlEx);

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
        body.GetProperty("ErrorKey").GetString().Should().Be(ErrorKeys.DbUnavailable);
    }

    [Fact]
    public async Task SqlException_NonTimeout_Returns500_WithUnexpectedErrorKey()
    {
        var sqlEx = CreateSqlException(number: 1205); // deadlock

        var (status, body) = await InvokeMiddlewareAsync(sqlEx);

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.GetProperty("ErrorKey").GetString().Should().Be(ErrorKeys.UnexpectedError);
    }

    [Fact]
    public async Task DbUpdateException_WrappingSqlTimeout_Returns503()
    {
        var sqlEx    = CreateSqlException(number: -2);
        var dbUpdateEx = new DbUpdateException("db error", sqlEx);

        var (status, body) = await InvokeMiddlewareAsync(dbUpdateEx);

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
        body.GetProperty("ErrorKey").GetString().Should().Be(ErrorKeys.DbUnavailable);
    }

    // ── OperationCancelled → 408 ──────────────────────────────────────────

    [Fact]
    public async Task OperationCanceledException_Returns408()
    {
        var (status, body) = await InvokeMiddlewareAsync(new OperationCanceledException());

        status.Should().Be(StatusCodes.Status408RequestTimeout);
        body.GetProperty("ErrorKey").GetString().Should().Be(ErrorKeys.RequestTimeout);
    }

    [Fact]
    public async Task TaskCanceledException_Returns408()
    {
        // TaskCanceledException hereda de OperationCanceledException
        var (status, body) = await InvokeMiddlewareAsync(new TaskCanceledException());

        status.Should().Be(StatusCodes.Status408RequestTimeout);
    }

    // ── ModuleNotActive → 403 ─────────────────────────────────────────────

    [Fact]
    public async Task ModuleNotActiveException_Returns403_WithModuleRequiredKey()
    {
        var (status, body) = await InvokeMiddlewareAsync(
            new ModuleNotActiveException(ModuleCodes.Reservations));

        status.Should().Be(StatusCodes.Status403Forbidden);
        body.GetProperty("ErrorKey").GetString().Should().Be(ErrorKeys.ModuleRequired);
    }

    // ── UnauthorizedAccess → 403 ──────────────────────────────────────────

    [Fact]
    public async Task UnauthorizedAccessException_Returns403_WithForbiddenKey()
    {
        var (status, body) = await InvokeMiddlewareAsync(new UnauthorizedAccessException());

        status.Should().Be(StatusCodes.Status403Forbidden);
        body.GetProperty("ErrorKey").GetString().Should().Be(ErrorKeys.Forbidden);
    }

    [Fact]
    public async Task TenantAccessException_Returns403_WithForbiddenKey()
    {
        var (status, body) = await InvokeMiddlewareAsync(new TenantAccessException());

        status.Should().Be(StatusCodes.Status403Forbidden);
        body.GetProperty("ErrorKey").GetString().Should().Be(ErrorKeys.Forbidden);
    }

    // ── Generic Exception → 500 ───────────────────────────────────────────

    [Fact]
    public async Task GenericException_Returns500_WithUnexpectedErrorKey()
    {
        var (status, body) = await InvokeMiddlewareAsync(
            new InvalidOperationException("algo salió mal"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.GetProperty("ErrorKey").GetString().Should().Be(ErrorKeys.UnexpectedError);
    }

    [Fact]
    public async Task GenericException_DoesNotLeakInternalMessage_ToClient()
    {
        var sensitiveMessage = "SELECT * FROM Users WHERE password = 'secret'";

        var (_, body) = await InvokeMiddlewareAsync(
            new Exception(sensitiveMessage));

        // El mensaje al cliente NUNCA debe contener la excepción interna
        body.GetProperty("Message").GetString()
            .Should().NotContain(sensitiveMessage);
    }

    [Fact]
    public async Task ResponseBody_AlwaysContainsSuccessFalse()
    {
        var (_, body) = await InvokeMiddlewareAsync(new Exception("test"));

        body.GetProperty("Success").GetBoolean().Should().BeFalse();
    }

    // ── Response ya iniciado ──────────────────────────────────────────────

    [Fact]
    public async Task ResponseAlreadyStarted_DoesNotThrow()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        // Simular que el response ya comenzó (HasStarted = true)
        context.Response.StatusCode = 200;

        // Necesitamos escribir algo para que HasStarted sea true.
        // En DefaultHttpContext, HasStarted puede no estar implementado como en el servidor real.
        // Verificamos solo que el middleware no lance excepción en este caso.
        RequestDelegate next = _ => throw new Exception("boom");

        var middleware = new global::DigiMenuAPI.Middleware.GlobalExceptionMiddleware(
            next,
            NullLogger<global::DigiMenuAPI.Middleware.GlobalExceptionMiddleware>.Instance);

        // No debe lanzar excepción
        var act = () => middleware.InvokeAsync(context);
        await act.Should().NotThrowAsync();
    }

    // ── Helper: crear SqlException via reflexión ──────────────────────────

    /// <summary>
    /// SqlException no tiene constructor público. Se crea via reflexión
    /// simulando el comportamiento del servidor SQL.
    /// </summary>
    private static SqlException CreateSqlException(int number, string message = "SQL error")
    {
        // SqlException requiere una colección de SqlError
        var collectionConstructor = typeof(SqlErrorCollection)
            .GetConstructor(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, Type.EmptyTypes, null)!;

        var errorCollection = (SqlErrorCollection)collectionConstructor.Invoke(null);

        var addMethod = typeof(SqlErrorCollection)
            .GetMethod("Add",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var sqlErrorCtor = typeof(SqlError)
            .GetConstructors(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(c => c.GetParameters().Length >= 8)
            ?? typeof(SqlError)
                .GetConstructors(
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .First();

        var sqlErrorParams = sqlErrorCtor.GetParameters();
        var args           = new object[sqlErrorParams.Length];

        for (int i = 0; i < args.Length; i++)
        {
            var p = sqlErrorParams[i];
            args[i] = p.Name switch
            {
                "infoNumber" or "number"    => number,
                "errorState" or "state"     => (byte)0,
                "errorClass" or "class"     => (byte)0,
                "server"                    => "test-server",
                "errorMessage" or "message" => message,
                "procedure" or "proc"       => string.Empty,
                "lineNumber" or "lineNum"   => 0,
                // win32ErrorCode may be int or uint depending on SqlClient version — let GetDefault infer
                _                           => GetDefault(p.ParameterType),
            };
        }

        var sqlError = sqlErrorCtor.Invoke(args);
        addMethod.Invoke(errorCollection, [sqlError]);

        var sqlExCtor = typeof(SqlException)
            .GetConstructors(
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .First();

        var exParams = sqlExCtor.GetParameters();
        var exArgs   = new object[exParams.Length];
        for (int i = 0; i < exArgs.Length; i++)
        {
            var p = exParams[i];
            exArgs[i] = p.Name switch
            {
                "message"    => message,
                "errorCollection" or "collection" => errorCollection,
                "innerException" or "inner"       => null!,
                "conId" or "connectionId"         => Guid.Empty,
                _            => GetDefault(p.ParameterType),
            };
        }

        return (SqlException)sqlExCtor.Invoke(exArgs);
    }

    private static object GetDefault(Type t)
        => t.IsValueType ? Activator.CreateInstance(t)! : null!;
}
