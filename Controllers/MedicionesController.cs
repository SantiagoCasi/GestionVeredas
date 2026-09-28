using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Data;
using SistemaVeredas.Models;

namespace SistemaVeredas.Controllers
{
    public class MedicionesController : Controller
    {
        private readonly AppDbContext _context;

        public MedicionesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Mediciones
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Mediciones.Include(m => m.TipoSuelo).Include(m => m.Vereda);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Mediciones/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicion = await _context.Mediciones
                .Include(m => m.TipoSuelo)
                .Include(m => m.Vereda)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (medicion == null)
            {
                return NotFound();
            }

            return View(medicion);
        }

        // GET: Mediciones/Create
        public IActionResult Create()
        {
            ViewData["TipoSueloId"] = new SelectList(_context.TiposSuelo, "Id", "Tipo");
            ViewData["VeredaId"] = new SelectList(_context.Veredas, "Id", "Calle");
            return View();
        }

        // POST: Mediciones/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,VeredaId,TipoSueloId,Sector,Largo,Ancho")] Medicion medicion)
        {
            if (ModelState.IsValid)
            {
                _context.Add(medicion);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TipoSueloId"] = new SelectList(_context.TiposSuelo, "Id", "Tipo", medicion.TipoSueloId);
            ViewData["VeredaId"] = new SelectList(_context.Veredas, "Id", "Calle", medicion.VeredaId);
            return View(medicion);
        }

        // GET: Mediciones/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicion = await _context.Mediciones.FindAsync(id);
            if (medicion == null)
            {
                return NotFound();
            }
            ViewData["TipoSueloId"] = new SelectList(_context.TiposSuelo, "Id", "Tipo", medicion.TipoSueloId);
            ViewData["VeredaId"] = new SelectList(_context.Veredas, "Id", "Calle", medicion.VeredaId);
            return View(medicion);
        }

        // POST: Mediciones/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,VeredaId,TipoSueloId,Sector,Largo,Ancho")] Medicion medicion)
        {
            if (id != medicion.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medicion);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicionExists(medicion.Id))
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
            ViewData["TipoSueloId"] = new SelectList(_context.TiposSuelo, "Id", "Tipo", medicion.TipoSueloId);
            ViewData["VeredaId"] = new SelectList(_context.Veredas, "Id", "Calle", medicion.VeredaId);
            return View(medicion);
        }

        // GET: Mediciones/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicion = await _context.Mediciones
                .Include(m => m.TipoSuelo)
                .Include(m => m.Vereda)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (medicion == null)
            {
                return NotFound();
            }

            return View(medicion);
        }

        // POST: Mediciones/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicion = await _context.Mediciones.FindAsync(id);
            if (medicion != null)
            {
                _context.Mediciones.Remove(medicion);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MedicionExists(int id)
        {
            return _context.Mediciones.Any(e => e.Id == id);
        }
    }
}
