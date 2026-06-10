using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Dtos;
using QuickDocs.Backend.Data;
using QuickDocs.Core.Models;
using QuickDocs.Backend.Services; 
using System;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotasCreditoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoPdfService _pdfService; 

        public NotasCreditoController(AppDbContext context, DocumentoPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [HttpPost]
        public async Task<ActionResult> CrearNotaCredito(NotaCreditoCreateDto dto)
        {
            string nombreClienteFinal = "Consumidor Final / Público General";

            if (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
            {
                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId.Value && c.UsuarioId == dto.UsuarioId);
                if (cliente == null)
                {
                    return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
                }
                nombreClienteFinal = cliente.Nombre; 
            }
            else if (!string.IsNullOrEmpty(dto.ClienteNombreLibre))
            {
                nombreClienteFinal = dto.ClienteNombreLibre; 
            }

            int ultimoNumero = await _context.Documentos
                .Where(d => d.UsuarioId == dto.UsuarioId && d.Tipo == TipoDocumento.NotaCredito)
                .Select(d => (int?)d.NumeroCorrelativo)
                .MaxAsync() ?? 0;

            var notaCredito = new NotaCredito
            {
                UsuarioId = dto.UsuarioId,
                ClienteId = dto.ClienteId,
                ClienteNombre = nombreClienteFinal, 
                Tipo = TipoDocumento.NotaCredito, 
                FechaEmision = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow.AddDays(30), 
                PuntoEmision = 1,                         
                NumeroCorrelativo = ultimoNumero + 1,     
                Total = dto.Total,
                Detalle = dto.Detalle,
                Estado = EstadoNotaCredito.Vigente 
            };

            _context.Documentos.Add(notaCredito);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObtenerNotaCreditoPorId), new { id = notaCredito.Id }, notaCredito);
        }

        // 🎯 NUEVO: Método para actualizar una nota de crédito existente
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarNotaCredito(int id, NotaCreditoCreateDto dto)
        {
            var notaCredito = await _context.Documentos
                .OfType<NotaCredito>()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notaCredito == null)
                return NotFound($"La nota de crédito con ID {id} no existe.");

            string nombreClienteFinal = "Consumidor Final / Público General";

            if (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
            {
                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId.Value && c.UsuarioId == dto.UsuarioId);
                if (cliente == null)
                {
                    return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
                }
                nombreClienteFinal = cliente.Nombre;
            }
            else if (!string.IsNullOrEmpty(dto.ClienteNombreLibre))
            {
                nombreClienteFinal = dto.ClienteNombreLibre;
            }

            // Actualizamos los campos mutables
            notaCredito.ClienteId = dto.ClienteId;
            notaCredito.ClienteNombre = nombreClienteFinal;
            notaCredito.Total = dto.Total;
            notaCredito.Detalle = dto.Detalle;

            await _context.SaveChangesAsync();

            return Ok(notaCredito);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<NotaCredito>> ObtenerNotaCreditoPorId(int id)
        {
            var notaCredito = await _context.Documentos
                .OfType<NotaCredito>()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notaCredito == null)
            {
                return NotFound("La nota de crédito solicitada no existe.");
            }

            return notaCredito;
        }

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> ObtenerPdfNotaCredito(int id)
        {
            var notaCredito = await _context.Documentos
                .OfType<NotaCredito>()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notaCredito == null)
                return NotFound("La nota de crédito específica no existe.");

            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == notaCredito.UsuarioId);
            if (perfil == null)
                return BadRequest("El usuario no tiene un perfil comercial configurado.");

            Cliente clienteData;
            if (notaCredito.ClienteId.HasValue && notaCredito.ClienteId.Value > 0)
            {
                var clienteReal = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == notaCredito.ClienteId.Value);
                clienteData = clienteReal ?? new Cliente
                {
                    Nombre = !string.IsNullOrEmpty(notaCredito.ClienteNombre) ? notaCredito.ClienteNombre : "Consumidor Final / Público General",
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }
            else
            {
                clienteData = new Cliente
                {
                    Nombre = !string.IsNullOrEmpty(notaCredito.ClienteNombre) ? notaCredito.ClienteNombre : "Consumidor Final / Público General",
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }

            byte[] pdfBytes = await _pdfService.GenerarNotaCreditoPdfAsync(notaCredito, perfil, clienteData);

            string nombreArchivo = $"NotaCredito_{notaCredito.Id}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> BorrarNotaCredito(int id)
        {
            var notaCredito = await _context.Documentos
                .OfType<NotaCredito>()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notaCredito == null) 
                return NotFound($"La nota de crédito con ID {id} no existe o ya fue eliminada.");

            _context.Documentos.Remove(notaCredito);
            await _context.SaveChangesAsync();

            System.Console.WriteLine($"[OK] Nota de Crédito ID {id} eliminada físicamente de SQLite.");
            return NoContent();
        }
    }
}