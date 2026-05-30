using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using QuickDocs.Core.Models; // Ajustá según el namespace real de tus modelos
using QuickDocs.Backend.Data; // Ajustá según tu DbContext real
using QuickDocs.Backend.Dtos;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context; // Cambiá 'YourDbContext' por el nombre real de tu contexto
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Modelo intermedio para recibir los datos del Login
        public class LoginDto
        {
            public string Username { get; set; } = string.Empty;
            public string Contrasena { get; set; } = string.Empty;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginData)
        {
            // 1. Buscamos al usuario por su Username
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Username == loginData.Username);

            if (usuario == null || !usuario.Activo)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            // 2. Verificamos la contraseña usando BCrypt
            bool passwordValido = BCrypt.Net.BCrypt.Verify(loginData.Contrasena, usuario.PasswordHash);
            
            if (!passwordValido)
            {
                return Unauthorized(new { mensaje = "Usuario o contraseña incorrectos." });
            }

            // 3. Si todo está OK, fabricamos el Token JWT
            var token = GenerarJwtToken(usuario);

            return Ok(new { 
                token = token, 
                usuarioId = usuario.Id,
                username = usuario.Username 
            });
        }

        private string GenerarJwtToken(Usuario usuario)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                    new Claim(ClaimTypes.Name, usuario.Username),
                    new Claim(ClaimTypes.Email, usuario.Email)
                }),
                Expires = DateTime.UtcNow.AddDays(7), // El token dura una semana
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature
                )
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            return tokenHandler.WriteToken(token);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest("El usuario y la contraseña son obligatorios.");
            }

            // Pasamos a minúscula o respetamos el casing según prefieras, pero validamos duplicados sin importar mayúsculas/minúsculas
            var usuarioExiste = await _context.Usuarios
                .AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower());

            if (usuarioExiste)
            {
                return BadRequest("El nombre de usuario ya está registrado.");
            }

            // Creamos el nuevo usuario aplicando BCrypt a la clave
            var nuevoUsuario = new Usuario
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Email = $"{dto.Username.ToLower()}@quickdocs.com", // Un mail genérico temporal
                FechaRegistro = DateTime.UtcNow,
                Activo = true
            };

            _context.Usuarios.Add(nuevoUsuario);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Usuario registrado exitosamente." });
        }
    }
}