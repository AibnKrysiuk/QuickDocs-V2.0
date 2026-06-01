using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Data;
using QuickDocs.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

//AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<DocumentoPdfService>();

// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseNpgsql(connectionString));

// 📂 Definimos la ruta segura en Pop!_OS (~/.local/share/QuickDocs/quickdocs.db)
var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
var databasePath = Path.Combine(folder, "QuickDocs", "quickdocs.db");

// Nos aseguramos de que la carpeta contenedora exista antes de arrancar
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);

var connectionString = $"Data Source={databasePath}";

// Configuramos EF Core para usar SQLite con esa ruta
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Configuración de Autenticación JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false; 
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero 
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// El orden de estos middlewares es sagrado
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.UseHttpsRedirection();

// ====================================================================
// 💾 BLOQUE DE SEEDING (¡CORREGIDO ACÁ! Antes del app.Run)
// ====================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();

    context.Database.EnsureCreated();

    var usuarioAdmin = context.Usuarios.FirstOrDefault(u => u.Username == "Admin");

    // 1. Garantizamos la existencia del usuario Administrador (Id = 1)
    if (usuarioAdmin != null)
    {
        if (!usuarioAdmin.PasswordHash.StartsWith("$2"))
        {
            usuarioAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234");
            context.SaveChanges();
        }
    }
    else
    {
        usuarioAdmin = new QuickDocs.Core.Models.Usuario
        {
            Username = "Admin",
            Email = "admin@quickdocs.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234"),
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        context.Usuarios.Add(usuarioAdmin);
        context.SaveChanges(); // Aquí SQLite le asigna el Id = 1
    }

    // 2. 🔥 NUEVO: Garantizamos la existencia del Perfil Comercial Base
    // Esto evita el BadRequest en el controlador si el usuario no cargó sus datos todavía
    var perfilExiste = context.Perfiles.Any(p => p.UsuarioId == usuarioAdmin.Id);
    if (!perfilExiste)
    {
        var perfilGenerico = new QuickDocs.Core.Models.Perfil
        {
            UsuarioId = usuarioAdmin.Id,
            NombreFantasia = "QuickDocs - Soluciones Globales",
            CondicionIva = "Responsable Inscripto", // O la condición por defecto de tu región
            Direccion = "Av. Principal 123, Ciudad Autónoma",
            TelefonoPrincipal = "+54 11 4321-0000",
            TelefonoSecundario = string.Empty,
            LogoPath = string.Empty
        };
        
        context.Perfiles.Add(perfilGenerico);
        context.SaveChanges();
        System.Console.WriteLine("[SEED] Perfil comercial genérico de QuickDocs inyectado con éxito.");
    }
}
// ====================================================================

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}