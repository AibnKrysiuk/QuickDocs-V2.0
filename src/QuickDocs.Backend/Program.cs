using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Data;
using QuickDocs.Backend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<DocumentoPdfService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

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
        var nuevoAdmin = new QuickDocs.Core.Models.Usuario
        {
            Username = "Admin",
            Email = "admin@quickdocs.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("1234"),
            Activo = true,
            FechaRegistro = DateTime.UtcNow
        };
        context.Usuarios.Add(nuevoAdmin);
        context.SaveChanges();
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