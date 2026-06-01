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
        public async Task<ActionResult<IEnumerable<Cliente>>> GetClientes([FromQuery] int? usuarioId)
        {
            // Estrategia temporal: Si no viene usuarioId, usamos el 1 (Admin) para poder probar locales
            int idClave = usuarioId ?? 1;

            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == idClave);
            if (!usuarioExiste)
            {
                return BadRequest("El UsuarioId especificado no existe.");
            }

            return await _context.Clientes
                .Where(c => c.UsuarioId == idClave)
                .ToListAsync();
        }

        // POST: api/Clientes
        [HttpPost]
        public async Task<ActionResult<Cliente>> PostCliente(Cliente cliente)
        {
            // Si el frontend no manda un UsuarioId válido, le asignamos el del Admin (1)
            if (cliente.UsuarioId <= 0)
            {
                cliente.UsuarioId = 1;
            }

            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == cliente.UsuarioId);
            if (!usuarioExiste)
            {
                return BadRequest("No se puede crear el cliente porque el UsuarioId especificado no existe.");
            }

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetClientes), new { usuarioId = cliente.UsuarioId }, cliente);
        }

        // PUT: api/Clientes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCliente(int id, Cliente cliente)
        {
            if (id != cliente.Id)
            {
                return BadRequest("El ID del cliente no coincide con el de la URL.");
            }

            // Forzamos a mantener el usuario dueño del registro por seguridad
            if (cliente.UsuarioId <= 0) cliente.UsuarioId = 1;

            _context.Entry(cliente).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Clientes.AnyAsync(c => c.Id == id))
                {
                    return NotFound("El cliente que intentás modificar ya no existe.");
                }
                throw;
            }

            return NoContent();
        }

        // DELETE: api/Clientes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCliente(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);
            if (cliente == null)
            {
                return NotFound("El cliente que intentás borrar no existe.");
            }

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}