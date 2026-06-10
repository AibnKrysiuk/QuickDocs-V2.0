using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Data;
using QuickDocs.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DocumentosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Documento>>> GetHistorial(
            [FromQuery] int usuarioId,
            [FromQuery] int tipoFiltro, // 0=Todos, 1=Presupuesto, 2=Remito, 3=Recibo, 4=NotaCredito
            [FromQuery] string? buscarCliente)
        {
            try
            {
                // 1. Iniciamos la consulta
                IQueryable<Documento> query = _context.Documentos;

                // 2. Filtramos por usuario
                query = query.Where(d => d.UsuarioId == usuarioId);

                // 3. Filtramos por Tipo de Documento usando switch para mayor claridad
                // Los índices 1 al 4 corresponden a los nuevos elementos del ComboBox
                if (tipoFiltro > 0)
                {
                    query = tipoFiltro switch
                    {
                        1 => query.Where(d => d.Tipo == TipoDocumento.Presupuesto),
                        2 => query.Where(d => d.Tipo == TipoDocumento.Remito),
                        3 => query.Where(d => d.Tipo == TipoDocumento.Recibo),
                        4 => query.Where(d => d.Tipo == TipoDocumento.NotaCredito),
                        _ => query // Si envían un número fuera de rango, trae todos
                    };
                }

                // 4. Filtro por nombre de cliente
                if (!string.IsNullOrWhiteSpace(buscarCliente))
                {
                    string busqueda = buscarCliente.Trim().ToLower();
                    query = query.Where(d => d.ClienteNombre.ToLower().Contains(busqueda));
                }

                // 5. Orden cronológico inverso
                query = query.OrderByDescending(d => d.FechaEmision)
                             .ThenByDescending(d => d.Id);

                var documentos = await query.ToListAsync();

                return Ok(documentos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno al recuperar el historial: {ex.Message}");
            }
        }
    }
}