using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Dtos;
using QuickDocs.Backend.Data;
using QuickDocs.Core.Models;
using QuickDocs.Backend.Services; // Tu servicio centralizado de PDFs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemitosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoPdfService _pdfService; // 1. Inyectamos nuestro servicio de PDFs

        public RemitosController(AppDbContext context, DocumentoPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [HttpPost]
        public async Task<ActionResult> CrearRemito(RemitoCreateDto dto)
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

            // 2. VALIDACIÓN OPCIONAL: Si viene de un presupuesto, verificar que exista
            if (dto.PresupuestoId.HasValue)
            {
                var presupuestoOrigen = await _context.Documentos
                    .OfType<Presupuesto>()
                    .FirstOrDefaultAsync(p => p.Id == dto.PresupuestoId.Value && p.UsuarioId == dto.UsuarioId);

                if (presupuestoOrigen == null)
                {
                    return BadRequest($"El presupuesto de origen con ID {dto.PresupuestoId.Value} no existe o no pertenece a este usuario.");
                }
            }

            // 3. GENERACIÓN DE CABECERA
            var remito = new Remito
            {
                UsuarioId = dto.UsuarioId,
                ClienteId = dto.ClienteId,
                DireccionEntrega = dto.DireccionEntrega,
                PresupuestoId = dto.PresupuestoId,
                FechaEmision = DateTime.UtcNow,
                Estado = EstadoRemito.Vigente, 
                FechaEntrega = null,           
                Detalles = new List<DetalleRemito>()
            };

            decimal subtotalAcumulado = 0m;

            // 4. PROCESAMIENTO DE LOS RENGLONES
            foreach (var itemDto in dto.Detalles)
            {
                var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemDto.ItemId && i.UsuarioId == dto.UsuarioId);
                if (item == null)
                {
                    return BadRequest($"El ítem con ID {itemDto.ItemId} no existe en tu catálogo.");
                }

                decimal totalRenglon = item.PrecioUnitario * itemDto.Cantidad;
                subtotalAcumulado += totalRenglon;

                var detalle = new DetalleRemito
                {
                    ItemId = item.Id,
                    DescripcionSnapshot = item.Descripcion,
                    Cantidad = itemDto.Cantidad,
                    PrecioAplicado = item.PrecioUnitario 
                };

                remito.Detalles.Add(detalle);
            }

            // 5. CÁLCULO DE TOTALES FINALES
            remito.Subtotal = subtotalAcumulado;
            remito.Descuento = dto.DescuentoGeneral;
            remito.Total = subtotalAcumulado - dto.DescuentoGeneral;

            if (remito.Total < 0m) remito.Total = 0m;

            // 6. PERSISTENCIA EN LA BASE DE DATOS
            _context.Documentos.Add(remito);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObtenerRemitoPorId), new { id = remito.Id }, remito);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Remito>> ObtenerRemitoPorId(int id)
        {
            var remito = await _context.Documentos
                .OfType<Remito>()
                .Include(r => r.Detalles) 
                .FirstOrDefaultAsync(r => r.Id == id);

            if (remito == null)
            {
                return NotFound("El remito solicitado no existe.");
            }

            return remito;
        }

        // 🎯 7. ENDPOINT PARA GENERAR Y DESCARGAR EL PDF DEL REMITO
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> ObtenerPdfRemito(int id)
        {
            // Buscamos el remito cargando sus renglones (Detalles)
            var remito = await _context.Documentos
                .OfType<Remito>()
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (remito == null)
                return NotFound("El remito especificado no existe.");

            // Buscamos el perfil del emisor
            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == remito.UsuarioId);
            if (perfil == null)
                return BadRequest("El usuario no tiene un perfil comercial configurado.");

            // Lógica del cliente a prueba de nulos
            Cliente clienteData;
            if (remito.ClienteId.HasValue && remito.ClienteId.Value > 0)
            {
                var clienteReal = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == remito.ClienteId.Value);
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

            // Invocamos el método asíncrono que vamos a crear en el servicio
            byte[] pdfBytes = await _pdfService.GenerarRemitoPdfAsync(remito, perfil, clienteData);

            string nombreArchivo = $"Remito_{remito.Id}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
    }
}