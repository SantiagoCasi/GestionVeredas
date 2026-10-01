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
        // Si hay TempData["Mensaje"] (p. ej., un tipo en uso que no se eliminó), la vista lo muestra.
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

        // POST: TipoSuelos/CrearRapido
        // Alta rápida desde el formulario de la vereda (RF-VER-06). Responde JSON:
        // 200 { id, texto } si se creó; 200 { id, texto, existente = true } si ya había uno disponible igual;
        // 400 { errores = [...] } si hay errores de validación.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearRapido([Bind("Tipo,Medida")] TipoSuelo tipoSuelo)
        {
            tipoSuelo.Tipo = (tipoSuelo.Tipo ?? string.Empty).Trim();
            tipoSuelo.Medida = string.IsNullOrWhiteSpace(tipoSuelo.Medida) ? null : tipoSuelo.Medida.Trim();
            tipoSuelo.Color = null;
            tipoSuelo.Disponible = true;

            // Se valida de nuevo con los valores ya recortados.
            ModelState.Clear();
            if (!TryValidateModel(tipoSuelo))
            {
                var errores = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m))
                    .Distinct()
                    .ToList();
                return BadRequest(new { errores });
            }

            // Duplicado: mismo Tipo y misma Medida, sin distinguir mayúsculas.
            var tipo = tipoSuelo.Tipo.ToLower();
            var medida = tipoSuelo.Medida?.ToLower();
            var consulta = _context.TiposSuelo.Where(t => t.Tipo.ToLower() == tipo);
            consulta = medida == null
                ? consulta.Where(t => t.Medida == null || t.Medida == "")
                : consulta.Where(t => t.Medida != null && t.Medida.ToLower() == medida);
            var existente = await consulta.OrderByDescending(t => t.Disponible).FirstOrDefaultAsync();

            if (existente != null)
            {
                if (existente.Disponible)
                    return Ok(new { id = existente.Id, texto = existente.Descripcion, existente = true });

                return BadRequest(new
                {
                    errores = new[] { $"«{existente.Descripcion}» ya existe pero está marcado como no disponible. Activalo desde Tipos de suelo." }
                });
            }

            _context.TiposSuelo.Add(tipoSuelo);
            await _context.SaveChangesAsync();
            return Ok(new { id = tipoSuelo.Id, texto = tipoSuelo.Descripcion });
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

            ViewData["VeredasQueLoUsan"] = await ContarVeredasQueLoUsanAsync(tipoSuelo.Id);
            return View(tipoSuelo);
        }

        // POST: TipoSuelos/Delete/5
        // RN-13: un tipo de suelo en uso no se borra: se marca como no disponible.
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var tipoSuelo = await _context.TiposSuelo.FindAsync(id);
            if (tipoSuelo != null)
            {
                var enUso = await ContarVeredasQueLoUsanAsync(tipoSuelo.Id);
                if (enUso > 0)
                {
                    tipoSuelo.Disponible = false;
                    await _context.SaveChangesAsync();
                    TempData["Mensaje"] = $"«{tipoSuelo.Descripcion}» lo usan {TextoVeredas(enUso)}: no se eliminó y quedó como no disponible.";
                    return RedirectToAction(nameof(Index));
                }

                _context.TiposSuelo.Remove(tipoSuelo);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Cantidad de veredas distintas que tienen algún pozo con este tipo de suelo.
        private Task<int> ContarVeredasQueLoUsanAsync(int tipoSueloId) =>
            _context.Roturas
                .Where(r => r.TipoSueloId == tipoSueloId)
                .Select(r => r.VeredaId)
                .Distinct()
                .CountAsync();

        // "1 vereda" / "3 veredas".
        public static string TextoVeredas(int n) => n == 1 ? "1 vereda" : $"{n} veredas";

        private bool TipoSueloExists(int id)
        {
            return _context.TiposSuelo.Any(e => e.Id == id);
        }
    }
}
