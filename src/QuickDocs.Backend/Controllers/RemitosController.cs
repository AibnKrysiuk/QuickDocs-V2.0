using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Dtos;
using QuickDocs.Backend.Data;
using QuickDocs.Backend.Services;
using QuickDocs.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq; 
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RemitosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoPdfService _pdfService;

        public RemitosController(AppDbContext context, DocumentoPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        // 🎯 1. LISTADO HISTORIAL
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Remito>>> GetRemitos([FromQuery] int? usuarioId)
        {
            int idClave = usuarioId ?? 1;

            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == idClave);
            if (!usuarioExiste) return BadRequest("El UsuarioId especificado no existe.");

            var remitos = await _context.Documentos
                .OfType<Remito>()
                .Where(r => r.UsuarioId == idClave)
                .Include(r => r.Cliente)
                .Include(r => r.Detalles)
                .OrderByDescending(r => r.FechaEmision)
                .ToListAsync();

            System.Console.WriteLine($"==================================================");
            System.Console.WriteLine($"[BACKEND-DIAG] Cantidad de remitos encontrados: {remitos.Count}");
            foreach (var r in remitos)
            {
                System.Console.WriteLine($" -> Remito ID: {r.Id} | ClienteID: {r.ClienteId} | Renglones en BD: {(r.Detalles != null ? r.Detalles.Count : "NULL")}");
            }
            System.Console.WriteLine($"==================================================");

            return remitos;
        }

        // 🎯 2. CREACIÓN DESDE CERO / CONVERSIÓN
        [HttpPost]
        public async Task<ActionResult> CrearRemito(RemitoCreateDto dto)
        {
            int idUsuarioReal = dto.UsuarioId <= 0 ? 1 : dto.UsuarioId;

            var perfilEmisor = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == idUsuarioReal);
            if (perfilEmisor == null) return BadRequest("El usuario no tiene un Perfil comercial configurado.");

            // Validamos cliente del catálogo si viene ID
            int? clienteIdAsignado = (dto.ClienteId.HasValue && dto.ClienteId.Value > 0) ? dto.ClienteId.Value : null;
            if (clienteIdAsignado.HasValue)
            {
                var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == clienteIdAsignado.Value && c.UsuarioId == idUsuarioReal);
                if (!clienteExiste) return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
            }

            // Validación opcional de Presupuesto origen
            if (dto.PresupuestoId.HasValue && dto.PresupuestoId.Value > 0)
            {
                var presupuestoExiste = await _context.Presupuestos
                    .AnyAsync(p => p.Id == dto.PresupuestoId.Value && p.UsuarioId == idUsuarioReal);
                if (!presupuestoExiste) return BadRequest($"El presupuesto de origen con ID {dto.PresupuestoId.Value} no existe.");
            }

            // 🎯 Forzamos el uso de la colección específica de Remitos para calcular el número
            int ultimoNumero = await _context.Remitos
                .Where(d => d.UsuarioId == idUsuarioReal)
                .Select(d => (int?)d.NumeroCorrelativo)
                .MaxAsync() ?? 0;

            var remito = new Remito
            {
                UsuarioId = idUsuarioReal,
                ClienteId = clienteIdAsignado,
                PresupuestoId = (dto.PresupuestoId > 0) ? dto.PresupuestoId : null,
                Tipo = TipoDocumento.Remito,
                FechaEmision = DateTime.UtcNow,
                DireccionEntrega = dto.DireccionEntrega,
                Estado = EstadoRemito.Vigente,
                PuntoEmision = 1,
                NumeroCorrelativo = ultimoNumero + 1,
                Detalles = new List<DetalleRemito>()
            };

            decimal subtotalAcumulado = 0m;

            foreach (var renglonDto in dto.Detalles)
            {
                // 🎯 Por defecto tomamos lo que viene del DTO (para ítems manuales)
                decimal precioAplicado = 0m; // En remitos podés dejarlo en 0m si no maneja precios, o usar renglonDto.Precio si lo agregaste
                string descripcionSnapshot = renglonDto.Descripcion;

                if (renglonDto.ItemId > 0)
                {
                    var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == renglonDto.ItemId && i.UsuarioId == idUsuarioReal);
                    if (item == null) return BadRequest($"El ítem con ID {renglonDto.ItemId} no existe.");
                    
                    descripcionSnapshot = item.Descripcion;
                    precioAplicado = item.PrecioUnitario; // Si el remito lleva valorización interna
                }

                decimal totalRenglon = precioAplicado * renglonDto.Cantidad;
                subtotalAcumulado += totalRenglon;

                remito.Detalles.Add(new DetalleRemito
                {
                    ItemId = (renglonDto.ItemId > 0) ? renglonDto.ItemId : null, 
                    DescripcionSnapshot = descripcionSnapshot,
                    Cantidad = renglonDto.Cantidad,        
                    PrecioAplicado = precioAplicado 
                });
            }

            if (remito.ClienteId.HasValue && remito.ClienteId.Value > 0)
            {
                var c = await _context.Clientes.FindAsync(remito.ClienteId.Value);
                if (c != null) remito.ClienteNombre = c.Nombre;
            }
            else if (!string.IsNullOrWhiteSpace(dto.ClienteNombreLibre))
            {
                remito.ClienteNombre = dto.ClienteNombreLibre;
            }
            else
            {
                remito.ClienteNombre = "Consumidor Final / Público General";
            }

            remito.Subtotal = subtotalAcumulado;
            remito.Descuento = dto.DescuentoGeneral;
            remito.Total = subtotalAcumulado - remito.Descuento;
            if (remito.Total < 0m) remito.Total = 0m;

            // 🎯 CAMBIO CLAVE: Guardamos apuntando al DbSet específico de Remitos para asegurar la persistencia TPH
            _context.Remitos.Add(remito);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(ObtenerRemitoPorId), new { id = remito.Id }, remito);
        }

        // 🎯 3. OBTENER POR ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Remito>> ObtenerRemitoPorId(int id)
        {
            var remito = await _context.Documentos
                .OfType<Remito>()
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (remito == null) return NotFound("El remito solicitado no existe.");
            return remito;
        }

        // 🎯 4. GENERACIÓN DE PDF
        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DescargarRemitoPdf(int id)
        {
            // 🎯 CAMBIO CLAVE: Buscamos directamente desde el DbSet específico de Remitos
            var remito = await _context.Remitos
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (remito == null) return NotFound("El remito no existe.");

            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == remito.UsuarioId);
            if (perfil == null) return BadRequest("No se encontró el Perfil del emisor para armar el PDF.");

            Cliente clienteData;
            if (remito.ClienteId.HasValue && remito.ClienteId.Value > 0)
            {
                var clienteReal = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == remito.ClienteId.Value);
                clienteData = clienteReal ?? new Cliente { Nombre = remito.ClienteNombre };
            }
            else
            {
                clienteData = new Cliente { Nombre = remito.ClienteNombre };
            }

            byte[] pdfBytes = await _pdfService.GenerarRemitoPdfAsync(remito, perfil, clienteData);

            string nombreArchivo = $"Remito_{remito.Id}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }

        // 🎯 5. MODIFICACIÓN (HISTORIAL)
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarRemito(int id, RemitoCreateDto dto)
        {
            var remitoExistente = await _context.Documentos
                .OfType<Remito>()
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (remitoExistente == null) 
                return NotFound($"No se encontró el remito con ID {id} para modificar.");

            int idUsuarioReal = dto.UsuarioId <= 0 ? 1 : dto.UsuarioId;

            if (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
            {
                var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == dto.ClienteId.Value && c.UsuarioId == idUsuarioReal);
                if (!clienteExiste) return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
            }

            remitoExistente.ClienteId = dto.ClienteId == 0 ? null : dto.ClienteId;
            remitoExistente.DireccionEntrega = dto.DireccionEntrega;
            remitoExistente.FechaEmision = DateTime.UtcNow; // Actualizamos estampa de modificación

            if (remitoExistente.ClienteId.HasValue && remitoExistente.ClienteId.Value > 0)
            {
                var c = await _context.Clientes.FindAsync(remitoExistente.ClienteId.Value);
                if (c != null) remitoExistente.ClienteNombre = c.Nombre;
            }
            else if (!string.IsNullOrWhiteSpace(dto.ClienteNombreLibre))
            {
                remitoExistente.ClienteNombre = dto.ClienteNombreLibre;
            }
            else
            {
                remitoExistente.ClienteNombre = "Consumidor Final / Público General";
            }

            // Limpieza y reemplazo de bultos tal cual como hacés con los detalles de presupuesto
            _context.Set<DetalleRemito>().RemoveRange(remitoExistente.Detalles);
            remitoExistente.Detalles.Clear();

            decimal subtotalAcumulado = 0m;

            foreach (var renglonDto in dto.Detalles)
            {
                var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == renglonDto.ItemId && i.UsuarioId == idUsuarioReal);
                if (item == null) return BadRequest($"El ítem con ID {renglonDto.ItemId} no existe.");

                decimal totalRenglon = item.PrecioUnitario * renglonDto.Cantidad;
                subtotalAcumulado += totalRenglon;

                remitoExistente.Detalles.Add(new DetalleRemito
                {
                    ItemId = item.Id,
                    DescripcionSnapshot = item.Descripcion,
                    Cantidad = renglonDto.Cantidad,
                    PrecioAplicado = item.PrecioUnitario
                });
            }

            remitoExistente.Subtotal = subtotalAcumulado;
            remitoExistente.Descuento = dto.DescuentoGeneral;
            remitoExistente.Total = subtotalAcumulado - remitoExistente.Descuento;
            if (remitoExistente.Total < 0m) remitoExistente.Total = 0m;

            await _context.SaveChangesAsync();
            return Ok(remitoExistente);
        }

        // 🎯 6. ELIMINACIÓN FÍSICA
        [HttpDelete("{id}")]
        public async Task<IActionResult> BorrarRemito(int id)
        {
            var remito = await _context.Documentos
                .OfType<Remito>()
                .Include(r => r.Detalles)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (remito == null) 
                return NotFound($"El remito con ID {id} no existe o ya fue eliminado.");

            _context.Documentos.Remove(remito);
            await _context.SaveChangesAsync();

            System.Console.WriteLine($"[OK] Remito ID {id} y sus bultos eliminados físicamente.");
            return NoContent();
        }
    }
}