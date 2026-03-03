using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DigiMenuAPI.Application.Filters
{
    /// <summary>
    /// Verifica que la empresa del usuario autenticado tenga activo
    /// el módulo requerido antes de ejecutar el action.
    ///
    /// Uso en controllers:
    /// <code>
    ///     [RequireModule(ModuleCodes.Reservations)]
    ///     public async Task&lt;ActionResult&gt; GetAll() { ... }
    /// </code>
    ///
    /// Requiere que el usuario esté autenticado con JWT y que el token
    /// contenga el claim "companyId" (int), generado por AuthService.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class RequireModuleAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _moduleCode;

        public RequireModuleAttribute(string moduleCode)
        {
            _moduleCode = moduleCode;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Obtener el CompanyId desde los claims del JWT
            var companyIdClaim = context.HttpContext.User.FindFirst("companyId");
            if (companyIdClaim is null || !int.TryParse(companyIdClaim.Value, out int companyId))
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    Success = false,
                    Message = "No se pudo identificar la empresa del usuario autenticado."
                });
                return;
            }

            var dbContext = context.HttpContext.RequestServices
                .GetRequiredService<ApplicationDbContext>();

            bool hasModule = await dbContext.CompanyModules
                .AsNoTracking()
                .AnyAsync(cm =>
                    cm.CompanyId == companyId &&
                    cm.PlatformModule.Code == _moduleCode &&
                    cm.IsActive);

            if (!hasModule)
            {
                context.Result = new ObjectResult(new
                {
                    Success = false,
                    Message = $"Tu plan no incluye el módulo '{_moduleCode}'. Contacta a soporte para activarlo."
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