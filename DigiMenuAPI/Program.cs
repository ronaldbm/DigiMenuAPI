using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.SQL;
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

        // ── DATABASE ─────────────────────────────────────────────────────────
        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // ── MULTI-TENANT ─────────────────────────────────────────────────────
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<ITenantService, TenantService>();

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

        // ── APPLICATION SERVICES ──────────────────────────────────────────────
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<ISettingService, SettingService>();
        builder.Services.AddScoped<IModuleService, ModuleService>();
        builder.Services.AddScoped<ICategoryService, CategoryService>();
        builder.Services.AddScoped<IProductService, ProductService>();
        builder.Services.AddScoped<ITagService, TagService>();
        builder.Services.AddScoped<IFooterLinkService, FooterLinkService>();
        builder.Services.AddScoped<IStandardIconService, StandardIconService>();
        builder.Services.AddScoped<IReservationService, ReservationService>();
        builder.Services.AddScoped<IStoreService, StoreService>();
        builder.Services.AddScoped<IFileStorageService, FileStorageService>();
        builder.Services.AddScoped<ICacheService, CacheService>();
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

        if (app.Environment.IsDevelopment())
        {
            // Expone el JSON del schema en: /openapi/v1.json
            app.MapOpenApi();

            // UI interactiva en: /scalar/v1
            app.MapScalarApiReference();
        }

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();
        app.UseCors("Frontend");
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseOutputCache();
        app.UseStaticFiles();

        app.MapControllers();
        app.Run();
    }
}