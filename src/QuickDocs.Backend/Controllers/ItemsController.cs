using QuickDocs.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ItemsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ItemsController(AppDbContext context)
        {
            _context = context;
        }

        // 🔍 GET: api/Items?usuarioId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems([FromQuery] int? usuarioId)
        {
            // Estrategia temporal: Si no viene usuarioId, usamos el 1 (Admin) para pruebas locales
            int idClave = usuarioId ?? 1;

            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == idClave);
            if (!usuarioExiste)
            {
                return BadRequest("El UsuarioId especificado no existe.");
            }

            return await _context.Items
                .Where(i => i.UsuarioId == idClave)
                .OrderBy(i => i.Descripcion) // Los ordenamos alfabéticamente para que quede prolijo
                .ToListAsync();
        }

        // 💾 POST: api/Items
        [HttpPost]
        public async Task<ActionResult<Item>> PostItem(Item item)
        {
            // Si el frontend no manda un UsuarioId válido, le asignamos el del Admin (1)
            if (item.UsuarioId <= 0)
            {
                item.UsuarioId = 1;
            }

            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == item.UsuarioId);
            if (!usuarioExiste)
            {
                return BadRequest("No se puede crear el ítem porque el UsuarioId especificado no existe.");
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            // Clave: Apuntamos al GET general pasando el usuarioId del dueño
            return CreatedAtAction(nameof(GetItems), new { usuarioId = item.UsuarioId }, item);
        }

        // ✏️ PUT: api/Items/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutItem(int id, Item item)
        {
            if (id != item.Id)
            {
                return BadRequest("El ID del ítem no coincide con el de la URL.");
            }

            // Forzamos a mantener el usuario dueño del registro por seguridad
            if (item.UsuarioId <= 0) item.UsuarioId = 1;

            _context.Entry(item).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Items.AnyAsync(i => i.Id == id))
                {
                    return NotFound("El ítem que intentás modificar ya no existe.");
                }
                throw;
            }

            return NoContent();
        }

        // 🗑️ DELETE: api/Items/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null)
            {
                return NotFound("El ítem que intentás borrar no existe.");
            }

            _context.Items.Remove(item);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}