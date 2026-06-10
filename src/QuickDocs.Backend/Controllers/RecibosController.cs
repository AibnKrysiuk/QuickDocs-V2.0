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
    public class RecibosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoPdfService _pdfService; 

        public RecibosController(AppDbContext context, DocumentoPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [HttpPost]
        public async Task<ActionResult> CrearRecibo(ReciboCreateDto dto)
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
                .Where(d => d.UsuarioId == dto.UsuarioId && d.Tipo == TipoDocumento.Recibo)
                .Select(d => (int?)d.NumeroCorrelativo)
                .MaxAsync() ?? 0;

            var recibo = new Recibo
            {
                UsuarioId = dto.UsuarioId,
                ClienteId = dto.ClienteId,
                ClienteNombre = nombreClienteFinal, 
                Tipo = TipoDocumento.Recibo, 
                FechaEmision = DateTime.UtcNow,
                PuntoEmision = 1,                         
                NumeroCorrelativo = ultimoNumero + 1,     
                ImporteRecibido = dto.ImporteRecibido,
                FormaPago = dto.FormaPago, 
                Detalle = dto.Detalle,
            };

            _context.Documentos.Add(recibo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObtenerReciboPorId), new { id = recibo.Id }, recibo);
        }

        // 🎯 NUEVO: Método para actualizar un recibo existente
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarRecibo(int id, ReciboCreateDto dto)
        {
            var recibo = await _context.Documentos
                .OfType<Recibo>()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recibo == null)
                return NotFound($"El recibo con ID {id} no existe.");

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

            // Actualizamos los campos mutables sin alterar numeración ni fecha original
            recibo.ClienteId = dto.ClienteId;
            recibo.ClienteNombre = nombreClienteFinal;
            recibo.ImporteRecibido = dto.ImporteRecibido;
            recibo.FormaPago = dto.FormaPago;
            recibo.Detalle = dto.Detalle;

            await _context.SaveChangesAsync();

            return Ok(recibo); 
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Recibo>> ObtenerReciboPorId(int id)
        {
            var recibo = await _context.Documentos
                .OfType<Recibo>()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recibo == null)
            {
                return NotFound("El recibo solicitado no existe.");
            }

            return recibo;
        }

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> ObtenerPdfRecibo(int id)
        {
            var recibo = await _context.Documentos
                .OfType<Recibo>()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recibo == null)
                return NotFound("El recibo especificado no existe.");

            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == recibo.UsuarioId);
            if (perfil == null)
                return BadRequest("El usuario no tiene un perfil comercial configurado.");

            Cliente clienteData;
            if (recibo.ClienteId.HasValue && recibo.ClienteId.Value > 0)
            {
                var clienteReal = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == recibo.ClienteId.Value);
                clienteData = clienteReal ?? new Cliente
                {
                    Nombre = !string.IsNullOrEmpty(recibo.ClienteNombre) ? recibo.ClienteNombre : "Consumidor Final / Público General",
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }
            else
            {
                clienteData = new Cliente
                {
                    Nombre = !string.IsNullOrEmpty(recibo.ClienteNombre) ? recibo.ClienteNombre : "Consumidor Final / Público General",
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }

            byte[] pdfBytes = await _pdfService.GenerarReciboPdfAsync(recibo, perfil, clienteData);

            string nombreArchivo = $"Recibo_{recibo.Id}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> BorrarRecibo(int id)
        {
            var recibo = await _context.Documentos
                .OfType<Recibo>()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recibo == null) 
                return NotFound($"El recibo con ID {id} no existe o ya fue eliminado.");

            _context.Documentos.Remove(recibo);
            await _context.SaveChangesAsync();

            System.Console.WriteLine($"[OK] Recibo ID {id} eliminado físicamente de SQLite.");
            return NoContent();
        }
    }
}