using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Filters;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Automapper
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(Assembly.GetExecutingAssembly()));

// EF
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer("name=DefaultConnection"
));

// Acceso al contexto HTTP (Necesario para generar URLs de imágenes)
builder.Services.AddHttpContextAccessor();

// CORS
var allowedHosts = builder.Configuration.GetValue<string>("AllowedHosts")?.Split(",") ?? ["*"];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(CORSOptions =>
    {
        CORSOptions.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Cache
builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromDays(1);
});

// Logger con Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.FromSource("ProductService"))
        .WriteTo.File("Logs/product-log-.txt", rollingInterval: RollingInterval.Day))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.FromSource("CategoryService"))
        .WriteTo.File("Logs/category-log-.txt", rollingInterval: RollingInterval.Day))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.FromSource("SubcategoryService"))
        .WriteTo.File("Logs/subcategory-log-.txt", rollingInterval: RollingInterval.Day))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(Matching.FromSource("SocialLinkService"))
        .WriteTo.File("Logs/socialLink-log-.txt", rollingInterval: RollingInterval.Day))
    .CreateLogger();

builder.Host.UseSerilog();

// Mensajes propios de Log
builder.Services.AddScoped(typeof(LogMessageDispatcher<>));

// Interfaces
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IFooterLinkService, FooterLinkService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IStandardIconService, StandardIconService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<ITagService, TagService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Servir archivos físicos
app.UseStaticFiles();

app.UseCors();

app.UseOutputCache();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/ping", () => "pong");

app.Run();