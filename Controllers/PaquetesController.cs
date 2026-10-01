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
    public class PaquetesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PaqueteService _paquetes;

        public PaquetesController(AppDbContext context, PaqueteService paquetes)
        {
            _context = context;
            _paquetes = paquetes;
        }

        // GET: Paquetes
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Paquetes.Include(p => p.Proveedor);

            // Cantidad de veredas por paquete, sin traer las veredas.
            ViewData["CantidadVeredas"] = await _context.Veredas
                .Where(v => v.PaqueteId != null)
                .GroupBy(v => v.PaqueteId!.Value)
                .Select(g => new { PaqueteId = g.Key, Cantidad = g.Count() })
                .ToDictionaryAsync(x => x.PaqueteId, x => x.Cantidad);

            return View(await appDbContext.ToListAsync());
        }

        // GET: Paquetes/Details/5
        // Veredas del paquete, totales y avance (RF-PAQ-04).
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var detalle = await _paquetes.ObtenerDetalleAsync(id.Value);
            if (detalle == null)
            {
                return NotFound();
            }

            return View(detalle);
        }

        // GET: Paquetes/Create
        public IActionResult Create()
        {
            ViewData["ProveedorId"] = new SelectList(_context.Proveedores, "Id", "Nombre");
            return View();
        }

        // POST: Paquetes/Create
        // Al guardar lleva directo a "Agregar veredas" (RF-PAQ-02).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Fecha,ProveedorId,Observacion")] Paquete paquete)
        {
            if (ModelState.IsValid)
            {
                _context.Add(paquete);
                await _context.SaveChangesAsync();
                TempData["Exito"] = $"Se creó el paquete «{paquete.Nombre}». Ahora elegí las veredas que van en él.";
                return RedirectToAction(nameof(AgregarVeredas), new { id = paquete.Id });
            }
            ViewData["ProveedorId"] = new SelectList(_context.Proveedores, "Id", "Nombre", paquete.ProveedorId);
            return View(paquete);
        }

        // GET: Paquetes/AgregarVeredas/5
        public async Task<IActionResult> AgregarVeredas(int id)
        {
            var model = await _paquetes.ObtenerParaAgregarAsync(id);
            if (model == null)
            {
                return NotFound();
            }
            return View(model);
        }

        // POST: Paquetes/AgregarVeredas/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarVeredas(int id, List<int>? veredaIds)
        {
            var ids = veredaIds ?? new List<int>();
            if (ids.Count > PaqueteService.MaximoIdsPorPedido)
            {
                return BadRequest();
            }

            if (ids.Count == 0)
            {
                var model = await _paquetes.ObtenerParaAgregarAsync(id);
                if (model == null)
                {
                    return NotFound();
                }
                ModelState.AddModelError(string.Empty, "Elegí al menos una vereda.");
                return View(model);
            }

            var resultado = await _paquetes.AgregarVeredasAsync(id, ids);
            if (!resultado.PaqueteExiste)
            {
                return NotFound();
            }

            var (exito, aviso) = PaqueteService.ArmarMensajes(resultado);
            if (exito != null) TempData["Exito"] = exito;
            if (aviso != null) TempData["Aviso"] = aviso;
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Paquetes/QuitarVereda/5
        // La vereda no se borra: queda sin paquete (RF-PAQ-03).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuitarVereda(int id, int veredaId)
        {
            var vereda = await _context.Veredas.AsNoTracking()
                .Where(v => v.Id == veredaId)
                .Select(v => new { v.Calle, v.Altura })
                .FirstOrDefaultAsync();

            if (await _paquetes.QuitarVeredaAsync(id, veredaId) && vereda != null)
            {
                TempData["Exito"] = $"Se quitó {PaqueteService.Direccion(vereda.Calle, vereda.Altura)} del paquete. Ya podés asignarla a otro.";
            }
            else
            {
                TempData["Aviso"] = "Esa vereda ya no estaba en este paquete.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // GET: Paquetes/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paquete = await _context.Paquetes.FindAsync(id);
            if (paquete == null)
            {
                return NotFound();
            }
            ViewData["ProveedorId"] = new SelectList(_context.Proveedores, "Id", "Nombre", paquete.ProveedorId);
            return View(paquete);
        }

        // POST: Paquetes/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Fecha,ProveedorId,Observacion")] Paquete paquete)
        {
            if (id != paquete.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(paquete);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaqueteExists(paquete.Id))
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
            ViewData["ProveedorId"] = new SelectList(_context.Proveedores, "Id", "Nombre", paquete.ProveedorId);
            return View(paquete);
        }

        // GET: Paquetes/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var paquete = await _context.Paquetes
                .Include(p => p.Proveedor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (paquete == null)
            {
                return NotFound();
            }

            ViewData["CantidadVeredas"] = await _context.Veredas.CountAsync(v => v.PaqueteId == paquete.Id);
            return View(paquete);
        }

        // POST: Paquetes/Delete/5
        // La FK pone PaqueteId = NULL en sus veredas: no se borran (RN-11).
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var paquete = await _context.Paquetes.FindAsync(id);
            if (paquete != null)
            {
                _context.Paquetes.Remove(paquete);
                TempData["Exito"] = $"Se eliminó el paquete «{paquete.Nombre}». Sus veredas quedaron sin paquete.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaqueteExists(int id)
        {
            return _context.Paquetes.Any(e => e.Id == id);
        }
    }
}
