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
    public class TipoSuelosController : Controller
    {
        private readonly AppDbContext _context;

        public TipoSuelosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: TipoSuelos
        public async Task<IActionResult> Index()
        {
            return View(await _context.TiposSuelo.ToListAsync());
        }

        // GET: TipoSuelos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tipoSuelo = await _context.TiposSuelo
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tipoSuelo == null)
            {
                return NotFound();
            }

            return View(tipoSuelo);
        }

        // GET: TipoSuelos/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TipoSuelos/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Tipo,Medida,Color,Disponible")] TipoSuelo tipoSuelo)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tipoSuelo);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tipoSuelo);
        }

        // GET: TipoSuelos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tipoSuelo = await _context.TiposSuelo.FindAsync(id);
            if (tipoSuelo == null)
            {
                return NotFound();
            }
            return View(tipoSuelo);
        }

        // POST: TipoSuelos/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Tipo,Medida,Color,Disponible")] TipoSuelo tipoSuelo)
        {
            if (id != tipoSuelo.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tipoSuelo);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TipoSueloExists(tipoSuelo.Id))
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
            return View(tipoSuelo);
        }

        // GET: TipoSuelos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var tipoSuelo = await _context.TiposSuelo
                .FirstOrDefaultAsync(m => m.Id == id);
            if (tipoSuelo == null)
            {
                return NotFound();
            }

            return View(tipoSuelo);
        }

        // POST: TipoSuelos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipoSuelo = await _context.TiposSuelo.FindAsync(id);
            if (tipoSuelo != null)
            {
                _context.TiposSuelo.Remove(tipoSuelo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TipoSueloExists(int id)
        {
            return _context.TiposSuelo.Any(e => e.Id == id);
        }
    }
}
