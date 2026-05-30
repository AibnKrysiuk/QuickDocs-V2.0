using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickDocs.Core.Models;
using QuickDocs.Backend.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickDocs.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuariosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Usuarios
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Usuario>>> GetUsuarios()
        {
            // Usamos Include para traer también el perfil asociado si lo tiene
            return await _context.Usuarios.Include(u => u.Perfil).ToListAsync();
        }

        // POST: api/Usuarios
        [HttpPost]
        public async Task<ActionResult<Usuario>> PostUsuario(Usuario usuario)
        {
            // Nota: En producción acá encriptaríamos la contraseña, 
            // por ahora lo guardamos directo para validar el circuito.
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUsuarios), new { id = usuario.Id }, usuario);
        }
    }
}