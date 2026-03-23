using AppCore.Application.Interfaces;
using AppCore.Application.Services;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using AppCore.Application.Services.Email;
using AppCore.Application.Utils;
using AppCore.Infrastructure.Email;
using AppCore.Infrastructure.SQL;
using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.SQL;
using DigiMenuAPI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;
using System.Text;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ── SERILOG ──────────────────────────────────────────────────────────
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .CreateLogger();

        builder.Host.UseSerilog();

        // ── MULTI-TENANT ─────────────────────────────────────────────────────
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ITenantService, TenantService>();

        // ── DATABASE ─────────────────────────────────────────────────────────
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions
                    .EnableRetryOnFailure(
                        maxRetryCount:     3,
                        maxRetryDelay:     TimeSpan.FromSeconds(6),
                        errorNumbersToAdd: [-2, 258]) // timeout numbers explícitos
                    .UseNetTopologySuite()));

        // AppCore services depend on CoreDbContext — ApplicationDbContext extends it,
        // so we register a scoped factory so CoreDbContext resolves to the same instance.
        builder.Services.AddScoped<CoreDbContext>(sp =>
            sp.GetRequiredService<ApplicationDbContext>());
        
        // ── MODULE GUARD ─────────────────────────────────────────────────────
        builder.Services.AddMemoryCache();
        builder.Services.AddScoped<IModuleGuard, ModuleGuard>();

        // ── JWT AUTHENTICATION ────────────────────────────────────────────────
        var jwtKey = builder.Configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key no configurado en appsettings.");

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        builder.Services.AddAuthorization();

        builder.Services.AddProblemDetails();

        // ── RATE LIMITING ─────────────────────────────────────────────────────────
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Auth endpoints: max 10 peticiones por minuto por IP
            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 0;
            });
        });

        // ── EMAIL ─────────────────────────────────────────────────────────────────
        var emailProvider = builder.Configuration["Email:Provider"] ?? "SendGrid";

        if (emailProvider.Equals("Smtp", StringComparison.OrdinalIgnoreCase))
            builder.Services.AddScoped<IEmailService, SmtpEmailService>();
        else
            builder.Services.AddScoped<IEmailService, SendGridEmailService>();

        builder.Services.AddScoped<IEmailQueueService, EmailQueueService>();
        builder.Services.AddHostedService<EmailOutboxProcessor>();

        // ── APPLICATION SERVICES ──────────────────────────────────────────────
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ISettingService, SettingService>();
        builder.Services.AddScoped<IModuleService, ModuleService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<IBranchProductService, BranchProductService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<IFooterLinkService, FooterLinkService>();
        builder.Services.AddScoped<IStandardIconService, StandardIconService>();
        builder.Services.AddScoped<IReservationService, ReservationService>();
        builder.Services.AddScoped<IStoreService, StoreService>();
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();
        builder.Services.AddScoped<ICacheService, CacheService>();
        builder.Services.AddScoped<IBranchService, BranchService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IScheduleService, ScheduleService>();
        builder.Services.AddScoped<IBranchEventService, BranchEventService>();
        builder.Services.AddScoped<IBranchPromotionService, BranchPromotionService>();
        builder.Services.AddScoped<ICarouselService, CarouselService>();
        builder.Services.AddScoped<IDashboardService, DashboardService>();
        builder.Services.AddScoped<ICompanyLanguageService, CompanyLanguageService>();
        builder.Services.AddScoped(typeof(LogMessageDispatcher<>));

        // ── AUTOMAPPER + OUTPUTCACHE + CONTROLLERS ────────────────────────────
        builder.Services.AddAutoMapper(cfg =>
            cfg.AddMaps(typeof(Program).Assembly)); 
        builder.Services.AddOutputCache();
        builder.Services.AddControllers();

        // ── CORS ─────────────────────────────────────────────────────────────
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
                policy
                    .WithOrigins(
                        builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                        ?? ["http://localhost:5173"])
                    .AllowAnyHeader()
                    .AllowAnyMethod());
        });

        // ── OPENAPI (.NET 10 nativo) + Scalar UI ─────────────────────────────
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, ct) =>
            {
                document.Info = new()
                {
                    Title = "DigiMenuAPI",
                    Version = "v1",
                    Description = "API SaaS multiempresa para menús digitales"
                };

                // Instancia explícita en lugar de new()
                if (document.Components == null)
                    document.Components = new OpenApiComponents();

                if (document.Components.SecuritySchemes == null)
                    document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>();

                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Bearer. Introduce el token sin el prefijo 'Bearer'."
                };

                return Task.CompletedTask;
            });
        });
        // ════════════════════════════════════════════════════════════════════
        var app = builder.Build();
        // ════════════════════════════════════════════════════════════════════

        // ── MIDDLEWARE DE CORRELACIÓN Y EXCEPCIONES GLOBALES ─────────────────
        // Orden crítico: CorrelationId primero (sella el LogContext),
        // GlobalException segundo (envuelve todo el pipeline restante).
        // Ambos deben ir ANTES de UseSerilogRequestLogging para que el
        // CorrelationId aparezca en los logs de request y de excepción.
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            // Expone el JSON del schema en: /openapi/v1.json
            app.MapOpenApi();

            // UI interactiva en: /scalar/v1
            app.MapScalarApiReference();
        }

        app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} → {StatusCode} in {Elapsed:0.0000}ms [{CorrelationId}]";
            opts.EnrichDiagnosticContext = (diag, ctx) =>
            {
                diag.Set("RequestHost", ctx.Request.Host.Value);
                diag.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
                if (ctx.Request.Headers.TryGetValue("X-Correlation-ID", out var cid))
                    diag.Set("CorrelationId", cid.ToString());
            };
        });
        app.UseHttpsRedirection();
        app.UseCors("Frontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();
        app.UseOutputCache();
        app.UseStaticFiles();

        app.MapControllers();
        app.Run();
    }
}