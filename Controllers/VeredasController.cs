using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Data;
using SistemaVeredas.Models;
using SistemaVeredas.Services;

namespace SistemaVeredas.Controllers
{
    public class VeredasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FotoService _fotos;

        public VeredasController(AppDbContext context, FotoService fotos)
        {
            _context = context;
            _fotos = fotos;
        }

        // GET: Veredas
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Veredas.Include(v => v.Paquete).Include(v => v.Proveedor);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Veredas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vereda = await _context.Veredas
                .Include(v => v.Paquete)
                .Include(v => v.Proveedor)
                .Include(v => v.Mediciones).ThenInclude(m => m.TipoSuelo)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vereda == null)
            {
                return NotFound();
            }

            return View(vereda);
        }

        // GET: Veredas/Create
        public IActionResult Create()
        {
            ViewData["PaqueteId"] = new SelectList(_context.Paquetes, "Id", "Nombre");
            ViewData["ProveedorId"] = new SelectList(_context.Proveedores, "Id", "Nombre");
            return View();
        }

        // POST: Veredas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        // "Fotos" no se bindea: lo arma el servidor a partir de los archivos subidos.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Apellido,Calle,EntreCalle1,EntreCalle2,Altura,Ubicacion,Observacion,FechaReclamo,FechaRelevo,Prioridad,Estado,ACargoFrentista,ProveedorId,PaqueteId")] Vereda vereda, List<IFormFile>? archivos)
        {
            var errorFotos = FotoService.Validar(archivos);
            if (errorFotos != null) ModelState.AddModelError("Fotos", errorFotos);

            if (ModelState.IsValid)
            {
                _context.Add(vereda);
                await _context.SaveChangesAsync(); // primero se guarda para tener el Id

                var rutas = await _fotos.GuardarAsync(vereda.Id, archivos);
                if (rutas.Count > 0)
                {
                    vereda.Fotos = FotoService.Unir(rutas);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PaqueteId"] = new SelectList(_context.Paquetes, "Id", "Nombre", vereda.PaqueteId);
            ViewData["ProveedorId"] = new SelectList(_context.Proveedores, "Id", "Nombre", vereda.ProveedorId);
            return View(vereda);
        }

        // GET: Veredas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vereda = await _context.Veredas.FindAsync(id);
            if (vereda == null)
            {
                return NotFound();
            }
            ViewData["PaqueteId"] = new SelectList(_context.Paquetes, "Id", "Nombre", vereda.PaqueteId);
            ViewData["ProveedorId"] = new SelectList(_context.Proveedores, "Id", "Nombre", vereda.ProveedorId);
            return View(vereda);
        }

        // POST: Veredas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Apellido,Calle,EntreCalle1,EntreCalle2,Altura,Ubicacion,Observacion,FechaReclamo,FechaRelevo,Prioridad,Estado,ACargoFrentista,ProveedorId,PaqueteId")] Vereda vereda,
            List<IFormFile>? archivos, List<string>? fotosAEliminar)
        {
            if (id != vereda.Id)
            {
                return NotFound();
            }

            // Las fotos actuales se leen de la base (no vienen del formulario).
            var fotosActuales = await _context.Veredas.AsNoTracking()
                .Where(v => v.Id == id).Select(v => v.Fotos).FirstOrDefaultAsync();
            vereda.Fotos = fotosActuales;

            var errorFotos = FotoService.Validar(archivos);
            if (errorFotos != null) ModelState.AddModelError("Fotos", errorFotos);

            if (ModelState.IsValid)
            {
                var rutas = FotoService.Separar(fotosActuales);
                foreach (var ruta in (fotosAEliminar ?? new List<string>()).Where(rutas.Contains).ToList())
                {
                    _fotos.Eliminar(ruta);
                    rutas.Remove(ruta);
                }
                rutas.AddRange(await _fotos.GuardarAsync(vereda.Id, archivos));
                vereda.Fotos = FotoService.Unir(rutas);

                try
                {
                    _context.Update(vereda);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VeredaExists(vereda.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PaqueteId"] = new SelectList(_context.Paquetes, "Id", "Nombre", vereda.PaqueteId);
            ViewData["ProveedorId"] = new SelectList(_context.Proveedores, "Id", "Nombre", vereda.ProveedorId);
            return View(vereda);
        }

        // GET: Veredas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vereda = await _context.Veredas
                .Include(v => v.Paquete)
                .Include(v => v.Proveedor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (vereda == null)
            {
                return NotFound();
            }

            return View(vereda);
        }

        // POST: Veredas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var vereda = await _context.Veredas.FindAsync(id);
            if (vereda != null)
            {
                _context.Veredas.Remove(vereda);
            }

            await _context.SaveChangesAsync();
            _fotos.EliminarCarpeta(id);
            return RedirectToAction(nameof(Index));
        }

        private bool VeredaExists(int id)
        {
            return _context.Veredas.Any(e => e.Id == id);
        }
    }
}
