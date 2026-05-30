using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Dtos;
using QuickDocs.Backend.Data;
using QuickDocs.Backend.Services; // Asegurate de importar la carpeta de servicios
using QuickDocs.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PresupuestosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoPdfService _pdfService; // 1. Declaramos el servicio del PDF

        // 2. Lo inyectamos en el constructor
        public PresupuestosController(AppDbContext context, DocumentoPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [HttpPost]
        public async Task<ActionResult> CrearPresupuesto(PresupuestoCreateDto dto)
        {
            var perfilEmisor = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == dto.UsuarioId);
            if (perfilEmisor == null) return BadRequest("El usuario no tiene un Perfil comercial configurado.");

            if (dto.ClienteId.HasValue)
            {
                var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == dto.ClienteId.Value && c.UsuarioId == dto.UsuarioId);
                if (!clienteExiste) return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
            }

            var presupuesto = new Presupuesto
            {
                UsuarioId = dto.UsuarioId,
                ClienteId = dto.ClienteId,
                FechaEmision = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow.AddDays(15), 
                Estado = EstadoPresupuesto.Vigente,
                Detalles = new List<DetallePresupuesto>()
            };

            decimal subtotalAcumulado = 0m; 

            foreach (var renglonDto in dto.Detalles)
            {
                var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == renglonDto.ItemId && i.UsuarioId == dto.UsuarioId);
                if (item == null) return BadRequest($"El ítem con ID {renglonDto.ItemId} no existe.");

                decimal totalRenglon = item.PrecioUnitario * renglonDto.Cantidad;
                subtotalAcumulado += totalRenglon;

                presupuesto.Detalles.Add(new DetallePresupuesto
                {
                    ItemId = item.Id,
                    DescripcionSnapshot = item.Descripcion,
                    Cantidad = renglonDto.Cantidad,        
                    PrecioAplicado = item.PrecioUnitario 
                });
            }

            presupuesto.Subtotal = subtotalAcumulado;
            presupuesto.Descuento = (decimal)dto.DescuentoGeneral;
            presupuesto.Total = subtotalAcumulado - presupuesto.Descuento;
            if (presupuesto.Total < 0m) presupuesto.Total = 0m;

            _context.Documentos.Add(presupuesto);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObtenerPresupuestoPorId), new { id = presupuesto.Id }, presupuesto);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Presupuesto>> ObtenerPresupuestoPorId(int id)
        {
            var presupuesto = await _context.Documentos
                .OfType<Presupuesto>()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presupuesto == null) return NotFound("El presupuesto solicitado no existe.");
            return presupuesto;
        }

        // 3. EL GRAN ENDPOINT FINAL: GET /api/Presupuestos/{id}/pdf
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DescargarPresupuestoPdf(int id)
        {
            // Buscamos el presupuesto con sus renglones
            var presupuesto = await _context.Documentos
                .OfType<Presupuesto>()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presupuesto == null) return NotFound("El presupuesto no existe.");

            // Buscamos el perfil de tu tía (emisor)
            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == presupuesto.UsuarioId);
            if (perfil == null) return BadRequest("No se encontró el Perfil del emisor para armar el PDF.");
            
            // Buscamos el cliente asociado
            // LOGICA DEL CLIENTE AL VUELO:
            Cliente clienteData;
            
            // Usamos una validación segura para el tipo anulable
            if (presupuesto.ClienteId.HasValue && presupuesto.ClienteId.Value > 0)
            {
                // Si tiene un ID válido, lo buscamos de forma segura
                var clienteReal = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == presupuesto.ClienteId.Value);
                
                // Si por alguna razón el ID existía pero el cliente fue borrado, usamos el genérico para que no rompa
                clienteData = clienteReal ?? new Cliente
                {
                    Nombre = "Consumidor Final / Público General",
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }
            else
            {
                // Si es null o 0, directo al cliente ficticio
                clienteData = new Cliente
                {
                    Nombre = "Consumidor Final / Público General",
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }

            // Invocamos al motor de QuestPDF que acabás de reparar
            byte[] pdfBytes = _pdfService.GenerarPresupuestoPdf(presupuesto, perfil, clienteData);

            // Retornamos el binario puro como un archivo descargable
            string nombreArchivo = $"Presupuesto_{id}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
    }
}