using DigiMenuAPI.Application.Interfaces;
using DigiMenuAPI.Application.Services;
using DigiMenuAPI.Infrastructure.SQL;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Filters;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Automapper
builder.Services.AddAutoMapper(typeof(Program));

//EF
builder.Services.AddDbContext<ApplicationDbContext>(options => 
    options.UseSqlServer("name=DefaultConnection"
));

//CORS
var allowedHosts = builder.Configuration.GetValue<string>("AllowedHosts")!.Split(",");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(CORSOptions =>
    {
        CORSOptions.WithOrigins(allowedHosts).AllowAnyMethod().AllowAnyHeader();
        //CORSOptions.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();

    });
});

//Cache
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

    .CreateLogger();

builder.Host.UseSerilog();

//Mensajes propios de Log
builder.Services.AddScoped(typeof(LogMessageDispatcher<>));

//Colocar mis interfaces
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<ISubcategoryService, SubcategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseOutputCache();

app.UseAuthorization();

app.MapControllers();
app.MapGet("/ping", () => "pong");

app.Run();
