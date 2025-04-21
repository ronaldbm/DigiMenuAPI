using DigiMenuAPI.Application.Utils;
using System.Runtime.CompilerServices;
using static DigiMenuAPI.Application.Common.Constants;

public class LogMessageDispatcher<TService>
{
    private readonly ILogger<TService> _logger;

    public LogMessageDispatcher(ILogger<TService> logger)
    {
        _logger = logger;
    }

    // Ahora recibimos EntityInfo + entidad opcional
    public void LogCreate(EntityInfo e, object? entityData = null, [CallerMemberName] string method = "")
    {
        var msg = MessageBuilder.Created(e);
        LogInfo(msg, entityData, method);
    }

    public void LogUpdate(EntityInfo e, object? entityData = null, [CallerMemberName] string method = "")
    {
        var msg = MessageBuilder.Updated(e);
        LogInfo(msg, entityData, method);
    }

    public void LogDelete(EntityInfo e, object? entityData = null, [CallerMemberName] string method = "")
    {
        var msg = MessageBuilder.Deleted(e);
        LogInfo(msg, entityData, method);
    }

    public void LogWarning(string msg, object? entityOrData = null, [CallerMemberName] string method = "")
    {
        var extra = FormatEntity(entityOrData);
        _logger.LogWarning("[{Method}] ⚠️ {Message} {Extra}", method, msg, extra);
    }

    public void LogError(Exception ex, string? msg = null, object? entityOrData = null, [CallerMemberName] string method = "")
    {
        var extra = FormatEntity(entityOrData);
        var finalMsg = msg ?? MessageBuilder.UnexpectedError(new EntityInfo("entidad", Gender.Masculine));
        _logger.LogError(ex, "[{Method}] ❌ {Message} {Extra}", method, finalMsg, extra);
    }

    private void LogInfo(string msg, object? entityOrData, string method)
    {
        var extra = FormatEntity(entityOrData);
        _logger.LogInformation("[{Method}] ℹ️ {Message} {Extra}", method, msg, extra);
    }

    private string FormatEntity(object? entity)
    {
        if (entity == null) return "";
        var t = entity.GetType();
        if (t.IsPrimitive || entity is string || entity is Guid)
            return $"Valor: {entity}";
        var id = t.GetProperty("Id")?.GetValue(entity);
        var label = t.GetProperty("Label")?.GetValue(entity) ?? t.GetProperty("Name")?.GetValue(entity);
        return $"ID: {id}, Nombre: {label}";
    }
}
