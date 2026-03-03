using DigiMenuAPI.Application.Common;
using DigiMenuAPI.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DigiMenuAPI.Infrastructure.Filters
{
    /// <summary>
    /// Filtro de acción que verifica si la empresa autenticada tiene
    /// el módulo premium requerido activo y sin expirar.
    ///
    /// Uso:
    ///   [RequireModule(ModuleCodes.Reservations)]
    ///   public class ReservationsController : BaseController { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireModuleAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _moduleCode;

        public RequireModuleAttribute(string moduleCode)
        {
            _moduleCode = moduleCode;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            var tenantService = context.HttpContext.RequestServices
                .GetRequiredService<ITenantService>();

            var moduleGuard = context.HttpContext.RequestServices
                .GetRequiredService<IModuleGuard>();

            var companyId = tenantService.TryGetCompanyId();

            if (companyId is null)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Success = false,
                    Message = "No autenticado."
                });
                return;
            }

            bool hasModule = await moduleGuard.HasModuleAsync(companyId.Value, _moduleCode);

            if (!hasModule)
            {
                context.Result = new ObjectResult(new
                {
                    Success = false,
                    Message = $"Tu plan no incluye el módulo '{_moduleCode}'. Contacta al soporte para activarlo.",
                    Module  = _moduleCode
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
