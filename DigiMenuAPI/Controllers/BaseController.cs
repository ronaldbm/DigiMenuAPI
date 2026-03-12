using AppCore.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace DigiMenuAPI.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        /// <summary>
        /// Traduce un OperationResult al ActionResult HTTP correcto.
        ///
        /// Éxito:
        ///   200 OK           → Success = true + Data != null
        ///   204 No Content   → Success = true + Data == null
        ///
        /// Error (mapeado por ErrorCode):
        ///   400 Bad Request      → General
        ///   403 Forbidden        → Forbidden | ModuleRequired
        ///   404 Not Found        → NotFound
        ///   409 Conflict         → Conflict
        ///   422 Unprocessable    → Validation
        ///
        /// La respuesta de error siempre incluye:
        ///   { success, errorCode, errorKey, message }
        /// El frontend usa errorKey para i18n, message como fallback en español.
        /// </summary>
        protected ActionResult HandleResult<T>(OperationResult<T> result)
        {
            if (result is null)
                return NotFound(new
                {
                    Success = false,
                    ErrorCode = OperationResultError.NotFound.ToString(),
                    ErrorKey = ErrorKeys.UnexpectedError,
                    Message = "Recurso no encontrado."
                });

            if (result.Success)
                return result.Data is not null
                    ? Ok(result)
                    : NoContent();

            // Objeto de error estructurado para i18n
            var errorBody = new
            {
                result.Success,
                ErrorCode = result.ErrorCode.ToString(),
                result.ErrorKey,
                result.Message
            };

            return result.ErrorCode switch
            {
                OperationResultError.NotFound =>
                    NotFound(errorBody),

                OperationResultError.Forbidden or
                OperationResultError.ModuleRequired =>
                    StatusCode(StatusCodes.Status403Forbidden, errorBody),

                OperationResultError.Conflict =>
                    Conflict(errorBody),

                OperationResultError.Validation =>
                    UnprocessableEntity(errorBody),

                _ => BadRequest(errorBody)
            };
        }
    }
}