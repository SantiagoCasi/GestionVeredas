using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Data;
using SistemaVeredas.Models;
using SistemaVeredas.Models.ViewModels;
using SistemaVeredas.Services;

namespace SistemaVeredas.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;

        public UsuariosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .OrderBy(u => u.UsApellido).ThenBy(u => u.UsNombre)
                .ToListAsync();
            return View(usuarios.Select(UsuarioViewModel.Desde).ToList());
        }

        // GET: Usuarios/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsId == id);
            if (usuario == null) return NotFound();

            return View(UsuarioViewModel.Desde(usuario));
        }

        // GET: Usuarios/Create
        public IActionResult Create()
        {
            return View(new UsuarioCreateViewModel());
        }

        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UsuarioCreateViewModel model)
        {
            if (await EmailEnUso(model.UsEmail, null))
                ModelState.AddModelError(nameof(model.UsEmail), "Ya existe un usuario con ese email.");

            if (!ModelState.IsValid) return View(model);

            var usuario = new Usuario
            {
                UsNombre = model.UsNombre,
                UsApellido = model.UsApellido,
                UsEmail = model.UsEmail.Trim(),
                UsContrasena = PasswordService.HashPassword(model.UsContrasena),
                UsActivo = model.UsActivo,
                FechaCreacion = DateTime.Now
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Usuarios/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            var model = new UsuarioEditViewModel
            {
                UsId = usuario.UsId,
                UsNombre = usuario.UsNombre,
                UsApellido = usuario.UsApellido,
                UsEmail = usuario.UsEmail,
                UsActivo = usuario.UsActivo
            };
            return View(model);
        }

        // POST: Usuarios/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UsuarioEditViewModel model)
        {
            if (id != model.UsId) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            if (await EmailEnUso(model.UsEmail, id))
                ModelState.AddModelError(nameof(model.UsEmail), "Ya existe un usuario con ese email.");

            if (!ModelState.IsValid) return View(model);

            usuario.UsNombre = model.UsNombre;
            usuario.UsApellido = model.UsApellido;
            usuario.UsEmail = model.UsEmail.Trim();
            usuario.UsActivo = model.UsActivo;

            if (!string.IsNullOrWhiteSpace(model.NuevaContrasena))
                usuario.UsContrasena = PasswordService.HashPassword(model.NuevaContrasena);

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Usuarios/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsId == id);
            if (usuario == null) return NotFound();

            return View(UsuarioViewModel.Desde(usuario));
        }

        // POST: Usuarios/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> EmailEnUso(string? email, int? exceptoId)
        {
            var e = (email ?? string.Empty).Trim();
            return _context.Usuarios.AnyAsync(u => u.UsEmail == e && (exceptoId == null || u.UsId != exceptoId));
        }
    }
}
