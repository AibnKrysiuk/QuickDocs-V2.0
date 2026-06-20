using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Core.Models;
using QuickDocs.Backend.Data;
using QuickDocs.Backend.Services;
using QuickDocs.Backend.Dtos; // 🛠️ Añadimos el namespace de los DTOs
using System;
using System.IO;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerfilesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoPdfService _pdfService;

        public PerfilesController(AppDbContext context, DocumentoPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // GET: api/Perfiles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Perfil>> GetPerfil(int id)
        {
            var perfil = await _context.Perfiles.FindAsync(id);

            if (perfil == null)
            {
                return NotFound();
            }

            return perfil;
        }

        // POST: api/Perfiles (Modificado para soportar Multipart Form Data y Upsert)
        [HttpPost]
        public async Task<IActionResult> PostPerfil([FromForm] PerfilRegistroDto dto)
        {
            // Estrategia de contingencia para pruebas locales con UsuarioId = 1
            if (dto.UsuarioId <= 0)
            {
                dto.UsuarioId = 1;
            }

            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == dto.UsuarioId);
            if (!usuarioExiste)
            {
                return BadRequest("El UsuarioId especificado no existe.");
            }

            // 🔍 Buscamos si ya existe el perfil para este usuario
            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == dto.UsuarioId);
            bool esNuevo = false;

            if (perfil == null)
            {
                perfil = new Perfil { UsuarioId = dto.UsuarioId };
                esNuevo = true;
            }

            // Mapeamos los campos del DTO al Modelo
            perfil.NombreFantasia = dto.NombreFantasia;
            perfil.Direccion = dto.Direccion;
            perfil.Localidad = dto.Localidad; // 🎯 ASIGNACIÓN DEL NUEVO CAMPO
            perfil.CuitCuil = dto.CuitCuil;
            perfil.TelefonoPrincipal = dto.TelefonoPrincipal;
            perfil.TelefonoSecundario = dto.TelefonoSecundario;
            perfil.EmailContacto = dto.EmailContacto;
            perfil.CondicionIva = dto.CondicionIva;

            // 💾 PROCESAMIENTO Y GUARDADO FÍSICO DEL LOGO
            if (dto.LogoArchivo != null && dto.LogoArchivo.Length > 0)
            {
                try
                {
                    // Almacenamiento local aislado en tu Pop!_OS (~/.local/share/QuickDocs/Logos)
                    var folderBase = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    var directorioLogos = Path.Combine(folderBase, "QuickDocs", "Logos");
                    
                    // Nos aseguramos de que el directorio exista
                    Directory.CreateDirectory(directorioLogos);

                    // Ponemos un nombre fijo/único por usuario para no acumular basura (ej: logo_1.png)
                    var extension = Path.GetExtension(dto.LogoArchivo.FileName);
                    var nombreArchivo = $"logo_{perfil.UsuarioId}{extension}";
                    var rutaFisicaCompleta = Path.Combine(directorioLogos, nombreArchivo);

                    // Escribimos el flujo binario en el disco duro del servidor
                    using (var stream = new FileStream(rutaFisicaCompleta, FileMode.Create))
                    {
                        await dto.LogoArchivo.CopyToAsync(stream);
                    }

                    // Guardamos únicamente la cadena de texto de la ruta en el modelo de la DB
                    perfil.LogoPath = rutaFisicaCompleta;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Error interno al guardar el archivo del logo: {ex.Message}");
                }
            }

            // Guardamos los cambios en SQLite según corresponda
            if (esNuevo)
            {
                _context.Perfiles.Add(perfil);
            }
            else
            {
                _context.Perfiles.Update(perfil);
            }

            await _context.SaveChangesAsync();

            return Ok(perfil);
        }

        // 🎯 ENDPOINT: Previsualizar la cabecera sin alterar la base de datos
        [HttpPost("previsualizar-pdf")]
        public IActionResult PrevisualizarPdf([FromBody] Perfil? perfilVentana)
        {
            Perfil perfilParaPdf = perfilVentana ?? new Perfil();

            if (perfilParaPdf.UsuarioId <= 0)
            {
                perfilParaPdf.UsuarioId = 1;
            }

            if (string.IsNullOrWhiteSpace(perfilParaPdf.NombreFantasia))
            {
                perfilParaPdf.NombreFantasia = "MI EMPRESA GENÉRICA S.A.";
                perfilParaPdf.Direccion = "Av. Siempre Viva 742";
                perfilParaPdf.Localidad = "Springfield"; // 🎯 VALOR POR DEFECTO PARA PREVISUALIZACIÓN
                perfilParaPdf.CondicionIva = "Monotributista / Responsable Inscripto";
                perfilParaPdf.TelefonoPrincipal = "+54 11 1234-5678";
            }

            byte[] pdfBytes = _pdfService.GenerarPdfPruebaCabecera(perfilParaPdf);

            return File(pdfBytes, "application/pdf", "Previsualizacion_Cabecera.pdf");
        }
    }
}