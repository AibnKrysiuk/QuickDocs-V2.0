using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Dtos;
using QuickDocs.Backend.Data;
using QuickDocs.Core.Models;
using QuickDocs.Backend.Services; // Asegurate de que este namespace apunte a tu IPdfService
using System;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecibosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoPdfService _pdfService; // 1. Inyectamos el servicio de PDF

        public RecibosController(AppDbContext context, DocumentoPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [HttpPost]
        public async Task<ActionResult> CrearRecibo(ReciboCreateDto dto)
        {
            // 1. VALIDACIÓN BLINDADA: Solo busca al cliente si el ClienteId tiene valor
            if (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
            {
                var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == dto.ClienteId.Value && c.UsuarioId == dto.UsuarioId);
                if (cliente == null)
                {
                    return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
                }
            }

            // 2. VALIDACIÓN OPCIONAL: Si viene un RemitoId, verificar que exista y sea del mismo usuario
            if (dto.RemitoId.HasValue)
            {
                var remito = await _context.Documentos
                    .OfType<Remito>()
                    .FirstOrDefaultAsync(r => r.Id == dto.RemitoId.Value && r.UsuarioId == dto.UsuarioId);
                
                if (remito == null)
                {
                    return BadRequest($"El remito con ID {dto.RemitoId.Value} no existe o no pertenece a este usuario.");
                }
            }

            // 3. GENERACIÓN DEL DOCUMENTO (ClienteId acepta null perfectamente acá)
            var recibo = new Recibo
            {
                UsuarioId = dto.UsuarioId,
                ClienteId = dto.ClienteId,
                FechaEmision = DateTime.UtcNow,
                ImporteRecibido = dto.ImporteRecibido,
                FormaPago = dto.FormaPago, 
                Detalle = dto.Detalle,
                RemitoId = dto.RemitoId
            };

            // 4. PERSISTENCIA EN LA BASE DE DATOS
            _context.Documentos.Add(recibo);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObtenerReciboPorId), new { id = recibo.Id }, recibo);
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

        // 🎯 5. ENDPOINT PARA GENERAR Y DESCARGAR EL PDF DEL RECIBO
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> ObtenerPdfRecibo(int id)
        {
            // Buscamos el recibo
            var recibo = await _context.Documentos
                .OfType<Recibo>()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (recibo == null)
                return NotFound("El recibo especificado no existe.");

            // Buscamos el perfil del emisor (tu tía)
            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == recibo.UsuarioId);
            if (perfil == null)
                return BadRequest("El usuario no tiene un perfil comercial configurado.");

            // Lógica del cliente a prueba de nulos
            Cliente clienteData;
            if (recibo.ClienteId.HasValue && recibo.ClienteId.Value > 0)
            {
                var clienteReal = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == recibo.ClienteId.Value);
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

            // Generamos los bytes del PDF usando el servicio
            // Nota: Asumo que en tu IPdfService vas a implementar un método específico para recibos
            byte[] pdfBytes = await _pdfService.GenerarReciboPdfAsync(recibo, perfil, clienteData);

            string nombreArchivo = $"Recibo_{recibo.Id}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
    }
}