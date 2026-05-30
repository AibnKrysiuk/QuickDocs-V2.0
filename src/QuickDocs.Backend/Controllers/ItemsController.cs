using QuickDocs.Backend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Core.Models;
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

        // GET: api/Items?usuarioId=1
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Item>>> GetItems([FromQuery] int usuarioId)
        {
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == usuarioId);
            if (!usuarioExiste)
            {
                return BadRequest("El UsuarioId especificado no existe.");
            }

            // Filtramos el catálogo para que cada uno vea solo sus productos
            return await _context.Items
                .Where(i => i.UsuarioId == usuarioId)
                .ToListAsync();
        }

        // POST: api/Items
        [HttpPost]
        public async Task<ActionResult<Item>> PostItem(Item item)
        {
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == item.UsuarioId);
            if (!usuarioExiste)
            {
                return BadRequest("No se puede crear el ítem porque el UsuarioId especificado no existe.");
            }

            _context.Items.Add(item);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetItems), new { usuarioId = item.UsuarioId }, item);
        }
    }
}