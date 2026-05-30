using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Dtos;
using QuickDocs.Backend.Data;
using QuickDocs.Core.Models;
using QuickDocs.Backend.Services; // Tu servicio centralizado de PDFs
using System;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotasCreditoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoPdfService _pdfService; // 1. Inyectamos nuestro servicio de PDFs

        public NotasCreditoController(AppDbContext context, DocumentoPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [HttpPost]
        public async Task<ActionResult> CrearNotaCredito(NotaCreditoCreateDto dto)
        {
            // 1. VALIDACIÓN BLINDADA: Solo busca al cliente si el ClienteId tiene valor (gracias a tu fix del [Required])
            if (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
            {
                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId.Value && c.UsuarioId == dto.UsuarioId);
                if (cliente == null)
                {
                    return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
                }
            }

            // 2. GENERACIÓN DEL DOCUMENTO (Alineado a tu modelo del Core)
            var notaCredito = new NotaCredito
            {
                UsuarioId = dto.UsuarioId,
                ClienteId = dto.ClienteId,
                FechaEmision = DateTime.UtcNow,
                // Le damos 30 días de vigencia al saldo a favor por defecto
                FechaVencimiento = DateTime.UtcNow.AddDays(30), 
                Total = dto.Total,
                Detalle = dto.Detalle,
                Estado = EstadoNotaCredito.Vigente 
            };

            // 3. PERSISTENCIA EN LA TABLA UNIFICADA TPH
            _context.Documentos.Add(notaCredito);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObtenerNotaCreditoPorId), new { id = notaCredito.Id }, notaCredito);
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

        // 🎯 4. ENDPOINT PARA GENERAR Y DESCARGAR EL PDF DE LA NOTA DE CRÉDITO
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> ObtenerPdfNotaCredito(int id)
        {
            // Buscamos la nota de crédito específica
            var notaCredito = await _context.Documentos
                .OfType<NotaCredito>()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (notaCredito == null)
                return NotFound("La nota de crédito especificada no existe.");

            // Buscamos el perfil del emisor
            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == notaCredito.UsuarioId);
            if (perfil == null)
                return BadRequest("El usuario no tiene un perfil comercial configurado.");

            // Lógica del cliente a prueba de nulos
            Cliente clienteData;
            if (notaCredito.ClienteId.HasValue && notaCredito.ClienteId.Value > 0)
            {
                var clienteReal = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == notaCredito.ClienteId.Value);
                clienteData = clienteReal ?? new Cliente
                {
                    Nombre = "Consumidor Final / Público General",
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }
            else
            {
                clienteData = new Cliente
                {
                    Nombre = "Consumidor Final / Público General",
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }

            // Invocamos el método asíncrono en el servicio
            byte[] pdfBytes = await _pdfService.GenerarNotaCreditoPdfAsync(notaCredito, perfil, clienteData);

            string nombreArchivo = $"NotaCredito_{notaCredito.Id}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
    }
}