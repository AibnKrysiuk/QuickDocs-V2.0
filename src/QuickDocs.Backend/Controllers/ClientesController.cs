using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Backend.Data;
using QuickDocs.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Clientes?usuarioId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes([FromQuery] int usuarioId)
        {
            // Validamos primero si el usuario existe para no hacer consultas al cohete
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == usuarioId);
            if (!usuarioExiste)
            {
                return BadRequest("El UsuarioId especificado no existe.");
            }

            // Aplicamos el filtro estricto de Multi-tenancy
            return await _context.Clientes
                .Where(c => c.UsuarioId == usuarioId)
                .ToListAsync();
        }

        // POST: api/Clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            // Validamos que el dueño del registro exista
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == cliente.UsuarioId);
            if (!usuarioExiste)
            {
                return BadRequest("No se puede crear el cliente porque el UsuarioId especificado no existe.");
            }

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetClientes), new { usuarioId = cliente.UsuarioId }, cliente);
        }
    }
}