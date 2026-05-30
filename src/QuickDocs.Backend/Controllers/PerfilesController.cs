using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Core.Models;
using QuickDocs.Backend.Data;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PerfilesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PerfilesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Perfiles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Perfil>> GetPerfil(int id)
        {
            var perfil = await _context.Perfiles.FindAsync(id);

            if (perfil == null)
            {
                return NotFound();
            }

            return perfil;
        }

        // POST: api/Perfiles
        [HttpPost]
        public async Task<ActionResult<Perfil>> PostPerfil(Perfil perfil)
        {
            // Validamos que exista el UsuarioId que nos mandan
            var usuarioExiste = await _context.Usuarios.AnyAsync(u => u.Id == perfil.UsuarioId);
            if (!usuarioExiste)
            {
                return BadRequest("El UsuarioId especificado no existe.");
            }

            _context.Perfiles.Add(perfil);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPerfil), new { id = perfil.Id }, perfil);
        }
    }
}