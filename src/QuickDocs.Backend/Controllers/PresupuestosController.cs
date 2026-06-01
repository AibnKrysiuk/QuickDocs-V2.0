using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Dtos;
using QuickDocs.Backend.Data;
using QuickDocs.Backend.Services;
using QuickDocs.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq; // 👈 Importante para usar los filtros de LINQ
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

        // 🔍 NEW ENDPOINT - GET: api/Presupuestos?usuarioId=1
        // Lo necesitamos sí o sí para alimentar la lista izquierda de la UI
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

            // 🕵️‍♂️ LOG DE CONTROL: Esto va a escupir la verdad en la terminal del BACKEND
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
        public async Task<ActionResult> CrearPresupuesto(PresupuestoCreateDto dto)
        {
            // Estrategia de contingencia: si no viene o es <= 0, usamos el 1 (Admin)
            int idUsuarioReal = dto.UsuarioId <= 0 ? 1 : dto.UsuarioId;

            var perfilEmisor = await _context.Perfiles.FirstOrDefaultAsync(p => p.UsuarioId == idUsuarioReal);
            if (perfilEmisor == null) return BadRequest("El usuario no tiene un Perfil comercial configurado.");

            if (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
            {
                var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == dto.ClienteId.Value && c.UsuarioId == idUsuarioReal);
                if (!clienteExiste) return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
            }

            // Buscamos el último número correlativo para este usuario y tipo de documento para autoincrementarlo
            int ultimoNumero = await _context.Documentos
                .Where(d => d.UsuarioId == idUsuarioReal && d.Tipo == TipoDocumento.Presupuesto)
                .Select(d => (int?)d.NumeroCorrelativo)
                .MaxAsync() ?? 0;

            var presupuesto = new Presupuesto
            {
                UsuarioId = idUsuarioReal,
                ClienteId = dto.ClienteId == 0 ? null : dto.ClienteId,
                Tipo = TipoDocumento.Presupuesto, // Especificamos el tipo de documento base
                FechaEmision = DateTime.UtcNow,
                FechaVencimiento = DateTime.UtcNow.AddDays(15), 
                Estado = EstadoPresupuesto.Vigente,
                PuntoEmision = 1,
                NumeroCorrelativo = ultimoNumero + 1, // Auto-numeración del talonario
                Detalles = new List<DetallePresupuesto>()
            };

            decimal subtotalAcumulado = 0m; 

            foreach (var renglonDto in dto.Detalles)
            {
                decimal precioAplicado = renglonDto.Precio;
                string descripcionSnapshot = renglonDto.Descripcion;

                // Si viene un ItemId válido (> 0), traemos los datos oficiales del catálogo
                if (renglonDto.ItemId.HasValue && renglonDto.ItemId.Value > 0)
                {
                    var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == renglonDto.ItemId.Value && i.UsuarioId == idUsuarioReal);
                    if (item == null) return BadRequest($"El ítem con ID {renglonDto.ItemId.Value} no existe.");
                    
                    // Priorizamos los datos congelados del catálogo si existen
                    descripcionSnapshot = item.Descripcion;
                    precioAplicado = item.PrecioUnitario;
                }

                decimal totalRenglon = precioAplicado * renglonDto.Cantidad;
                subtotalAcumulado += totalRenglon;

                presupuesto.Detalles.Add(new DetallePresupuesto
                {
                    ItemId = (renglonDto.ItemId > 0) ? renglonDto.ItemId : null, // Guarda Null si es un ítem libre
                    DescripcionSnapshot = descripcionSnapshot,
                    Cantidad = renglonDto.Cantidad,        
                    PrecioAplicado = precioAplicado 
                });
            }

            // Mapeamos el nombre del cliente al documento para que quede congelado históricamente
            if (presupuesto.ClienteId.HasValue)
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
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }
            else
            {
                clienteData = new Cliente
                {
                    Nombre = presupuesto.ClienteNombre,
                    CuitCuil = "00-00000000-0",
                    Direccion = "No especificada"
                };
            }

            byte[] pdfBytes = _pdfService.GenerarPresupuestoPdf(presupuesto, perfil, clienteData);

            string nombreArchivo = $"Presupuesto_{presupuesto.NumeroFormateado}_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", nombreArchivo);
        }
    
        // 🔄 NEW ENDPOINT - PUT: api/Presupuestos/5
        // Se ejecuta cuando el usuario modifica un presupuesto existente y presiona Guardar
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarPresupuesto(int id, PresupuestoCreateDto dto)
        {
            // 1. Buscamos el presupuesto existente con sus detalles incluidos
            var presupuestoExistente = await _context.Documentos
                .OfType<Presupuesto>()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presupuestoExistente == null) 
                return NotFound($"No se encontró el presupuesto con ID {id} para modificar.");

            int idUsuarioReal = dto.UsuarioId <= 0 ? 1 : dto.UsuarioId;

            // 2. Validaciones de seguridad de Negocio
            if (dto.ClienteId.HasValue && dto.ClienteId.Value > 0)
            {
                var clienteExiste = await _context.Clientes.AnyAsync(c => c.Id == dto.ClienteId.Value && c.UsuarioId == idUsuarioReal);
                if (!clienteExiste) return BadRequest("El cliente especificado no existe o no pertenece a este usuario.");
            }

            // 3. Actualizamos los datos de la cabecera
            presupuestoExistente.ClienteId = dto.ClienteId == 0 ? null : dto.ClienteId;
            presupuestoExistente.FechaEmision = DateTime.UtcNow; // Refrescamos la fecha de la última modificación
            presupuestoExistente.FechaVencimiento = DateTime.UtcNow.AddDays(15);

            if (presupuestoExistente.ClienteId.HasValue)
            {
                var c = await _context.Clientes.FindAsync(presupuestoExistente.ClienteId.Value);
                if (c != null) presupuestoExistente.ClienteNombre = c.Nombre;
            }
            else
            {
                presupuestoExistente.ClienteNombre = "Consumidor Final / Público General";
            }

            // 4. Limpieza y reemplazo atómico del Detalle (Maestro-Detalle)
            // Removemos los renglones anteriores para evitar duplicados o basura flotante
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

            // 5. Ajuste final de Totales
            presupuestoExistente.Subtotal = subtotalAcumulado;
            presupuestoExistente.Descuento = (decimal)dto.DescuentoGeneral;
            presupuestoExistente.Total = subtotalAcumulado - presupuestoExistente.Descuento;
            if (presupuestoExistente.Total < 0m) presupuestoExistente.Total = 0m;

            // Le avisamos a Entity Framework que el estado de la entidad mutó
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

            // Devolvemos el objeto actualizado para que el front-end refresque su grilla local si quiere
            return Ok(presupuestoExistente);
        }

        // 🗑️ NEW ENDPOINT - DELETE: api/Presupuestos/5
        // Elimina por completo el presupuesto y sus renglones asociados (en cascada por FK)
        [HttpDelete("{id}")]
        public async Task<IActionResult> BorrarPresupuesto(int id)
        {
            var presupuesto = await _context.Documentos
                .OfType<Presupuesto>()
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (presupuesto == null) 
                return NotFound($"El presupuesto con ID {id} no existe o ya fue eliminado.");

            // Removemos de la base de datos (EF se encarga de limpiar Detalles por la relación)
            _context.Documentos.Remove(presupuesto);
            await _context.SaveChangesAsync();

            System.Console.WriteLine($"[OK] Presupuesto ID {id} y sus renglones eliminados físicamente de SQLite.");
            return NoContent();
        }
    }
}