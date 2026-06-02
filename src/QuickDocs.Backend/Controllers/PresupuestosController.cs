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
    public class PresupuestosController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly DocumentoPdfService _pdfService;

        public PresupuestosController(AppDbContext context, DocumentoPdfService pdfService)
        {
            _context = context;
            _pdfService = pdfService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Presupuesto>>> GetPresupuestos([FromQuery] int? usuarioId)
        {
            int idClave = usuarioId ?? 1;

            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == idClave);
            if (!usuarioExiste) return BadRequest("El UsuarioId especificado no existe.");

            var presupuestos = await _context.Documentos
                .OfType<Presupuesto>()
                .Where(p => p.UsuarioId == idClave)
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                .OrderByDescending(p => p.FechaEmision)
                .ToListAsync();

            System.Console.WriteLine($"==================================================");
            System.Console.WriteLine($"[BACKEND-DIAG] Cantidad de presupuestos encontrados: {presupuestos.Count}");
            foreach (var p in presupuestos)
            {
                System.Console.WriteLine($" -> Presupuesto ID: {p.Id} | ClienteID: {p.ClienteId} | Renglones en BD: {(p.Detalles != null ? p.Detalles.Count : "NULL")}");
            }
            System.Console.WriteLine($"==================================================");

            return presupuestos;
        }

        [HttpPost]
        [HttpPost]
        public async Task<ActionResult> CrearPresupuesto(PresupuestoCreateDto dto)
        {
            int idUsuarioReal = dto.UsuarioId <= 0 ? 1 : dto.UsuarioId;

            var perfilEmisor = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == idUsuarioReal);
            if (perfilEmisor == null) return BadRequest("El usuario no tiene un Perfil comercial configurado.");

            // 🎯 INTERCEPCIÓN PASO 2: Si es un cliente libre/nuevo, lo creamos como Prospecto antes de seguir
            int? clienteIdAsignado = (dto.ClienteId.HasValue && dto.ClienteId.Value > 0) ? dto.ClienteId.Value : null;

            if (clienteIdAsignado.HasValue)
            {
                var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == clienteIdAsignado.Value && c.UsuarioId == idUsuarioReal);
                if (!clienteExiste) return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
            }
            else if (!string.IsNullOrWhiteSpace(dto.ClienteNombreLibre))
            {
                // Es un cliente libre tipeado a mano. Lo persistimos en la tabla Clientes como Prospecto.
                var nuevoClienteProspecto = new Cliente
                {
                    UsuarioId = idUsuarioReal,
                    Nombre = dto.ClienteNombreLibre,
                    CuitCuil = dto.ClienteCuitLibre ?? string.Empty,
                    Direccion = dto.ClienteDireccionLibre,
                    EsProspecto = true,
                    FechaAlta = DateTime.UtcNow
                };

                _context.Clientes.Add(nuevoClienteProspecto);
                await _context.SaveChangesAsync(); // Impactamos para obtener el Id autogenerado

                clienteIdAsignado = nuevoClienteProspecto.Id;
            }

            int ultimoNumero = await _context.Documentos
                .Where(d => d.UsuarioId == idUsuarioReal && d.Tipo == TipoDocumento.Presupuesto)
                .Select(d => (int?)d.NumeroCorrelativo)
                .MaxAsync() ?? 0;

            int diasAsignados = dto.DiasValidez <= 0 ? 15 : dto.DiasValidez;

            var presupuesto = new Presupuesto
            {
                UsuarioId = idUsuarioReal,
                ClienteId = clienteIdAsignado, // 🎯 Siempre va a tener un ID real si se escribió un nombre
                Tipo = TipoDocumento.Presupuesto, 
                FechaEmision = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow.AddDays(diasAsignados), 
                Estado = EstadoPresupuesto.Vigente,
                PuntoEmision = 1,
                NumeroCorrelativo = ultimoNumero + 1, 
                Detalles = new List<DetallePresupuesto>()
            };

            decimal subtotalAcumulado = 0m; 

            foreach (var renglonDto in dto.Detalles)
            {
                decimal precioAplicado = renglonDto.Precio;
                string descripcionSnapshot = renglonDto.Descripcion;

                if (renglonDto.ItemId.HasValue && renglonDto.ItemId.Value > 0)
                {
                    var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == renglonDto.ItemId.Value && i.UsuarioId == idUsuarioReal);
                    if (item == null) return BadRequest($"El ítem con ID {renglonDto.ItemId.Value} no existe.");
                    
                    descripcionSnapshot = item.Descripcion;
                    precioAplicado = item.PrecioUnitario;
                }

                decimal totalRenglon = precioAplicado * renglonDto.Cantidad;
                subtotalAcumulado += totalRenglon;

                presupuesto.Detalles.Add(new DetallePresupuesto
                {
                    ItemId = (renglonDto.ItemId > 0) ? renglonDto.ItemId : null, 
                    DescripcionSnapshot = descripcionSnapshot,
                    Cantidad = renglonDto.Cantidad,        
                    PrecioAplicado = precioAplicado 
                });
            }

            // 🎯 Asignamos el nombre definitivo al presupuesto usando el ID consolidado
            if (presupuesto.ClienteId.HasValue && presupuesto.ClienteId.Value > 0)
            {
                var c = await _context.Clientes.FindAsync(presupuesto.ClienteId.Value);
                if (c != null) presupuesto.ClienteNombre = c.Nombre;
            }
            else
            {
                presupuesto.ClienteNombre = "Consumidor Final / Público General";
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

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> DescargarPresupuestoPdf(int id)
        {
            var presupuesto = await _context.Documentos
                .OfType<Presupuesto>()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presupuesto == null) return NotFound("El presupuesto no existe.");

            var perfil = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == presupuesto.UsuarioId);
            if (perfil == null) return BadRequest("No se encontró el Perfil del emisor para armar el PDF.");
            
            Cliente clienteData;
            
            if (presupuesto.ClienteId.HasValue && presupuesto.ClienteId.Value > 0)
            {
                var clienteReal = await _context.Clientes.FirstOrDefaultAsync(c => c.Id == presupuesto.ClienteId.Value);
                clienteData = clienteReal ?? new Cliente
                {
                    Nombre = presupuesto.ClienteNombre,
                    CuitCuil = null,
                    Direccion = null
                };
            }
            else
            {
                // Si es un cliente libre/manual, generamos el objeto de transporte temporal sin datos rígidos falsos
                clienteData = new Cliente
                {
                    Nombre = presupuesto.ClienteNombre,
                    CuitCuil = null, 
                    Direccion = null  
                };
            }

            byte[] pdfBytes = _pdfService.GenerarPresupuestoPdf(presupuesto, perfil, clienteData);

            string nombreArchivo = $"Presupuesto_{presupuesto.NumeroFormateado}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
    
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarPresupuesto(int id, PresupuestoCreateDto dto)
        {
            var presupuestoExistente = await _context.Documentos
                .OfType<Presupuesto>()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presupuestoExistente == null) 
                return NotFound($"No se encontró el presupuesto con ID {id} para modificar.");

            int idUsuarioReal = dto.UsuarioId <= 0 ? 1 : dto.UsuarioId;

            if (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
            {
                var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == dto.ClienteId.Value && c.UsuarioId == idUsuarioReal);
                if (!clienteExiste) return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
            }

            // 🎯 Usamos los días de validez dinámicos enviados por el DTO en la modificación
            int diasAsignados = dto.DiasValidez <= 0 ? 15 : dto.DiasValidez;

            presupuestoExistente.ClienteId = dto.ClienteId == 0 ? null : dto.ClienteId;
            presupuestoExistente.FechaEmision = DateTime.UtcNow; 
            presupuestoExistente.FechaVencimiento = DateTime.UtcNow.AddDays(diasAsignados);

            if (presupuestoExistente.ClienteId.HasValue && presupuestoExistente.ClienteId.Value > 0)
            {
                var c = await _context.Clientes.FindAsync(presupuestoExistente.ClienteId.Value);
                if (c != null) presupuestoExistente.ClienteNombre = c.Nombre;
            }
            else if (!string.IsNullOrWhiteSpace(dto.ClienteNombreLibre))
            {
                presupuestoExistente.ClienteNombre = dto.ClienteNombreLibre;
            }
            else
            {
                presupuestoExistente.ClienteNombre = "Consumidor Final / Público General";
            }

            _context.Set<DetallePresupuesto>().RemoveRange(presupuestoExistente.Detalles);
            presupuestoExistente.Detalles.Clear();

            decimal subtotalAcumulado = 0m;

            foreach (var renglonDto in dto.Detalles)
            {
                decimal precioAplicado = renglonDto.Precio;
                string descripcionSnapshot = renglonDto.Descripcion;

                if (renglonDto.ItemId.HasValue && renglonDto.ItemId.Value > 0)
                {
                    var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == renglonDto.ItemId.Value && i.UsuarioId == idUsuarioReal);
                    if (item == null) return BadRequest($"El ítem con ID {renglonDto.ItemId.Value} no existe.");
                    
                    descripcionSnapshot = item.Descripcion;
                    precioAplicado = item.PrecioUnitario;
                }

                decimal totalRenglon = precioAplicado * renglonDto.Cantidad;
                subtotalAcumulado += totalRenglon;

                presupuestoExistente.Detalles.Add(new DetallePresupuesto
                {
                    ItemId = (renglonDto.ItemId > 0) ? renglonDto.ItemId : null,
                    DescripcionSnapshot = descripcionSnapshot,
                    Cantidad = renglonDto.Cantidad,
                    PrecioAplicado = precioAplicado
                });
            }

            presupuestoExistente.Subtotal = subtotalAcumulado;
            presupuestoExistente.Descuento = (decimal)dto.DescuentoGeneral;
            presupuestoExistente.Total = subtotalAcumulado - presupuestoExistente.Descuento;
            if (presupuestoExistente.Total < 0m) presupuestoExistente.Total = 0m;

            _context.Entry(presupuestoExistente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
                System.Console.WriteLine($"[OK] Presupuesto ID {id} modificado y recalculado con éxito en la BD.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Documentos.AnyAsync(e => e.Id == id)) return NotFound();
                else throw;
            }

            return Ok(presupuestoExistente);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> BorrarPresupuesto(int id)
        {
            var presupuesto = await _context.Documentos
                .OfType<Presupuesto>()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presupuesto == null) 
                return NotFound($"El presupuesto con ID {id} no existe o ya fue eliminado.");

            _context.Documentos.Remove(presupuesto);
            await _context.SaveChangesAsync();

            System.Console.WriteLine($"[OK] Presupuesto ID {id} y sus renglones eliminados físicamente de SQLite.");
            return NoContent();
        }
    }
}