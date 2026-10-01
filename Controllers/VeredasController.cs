using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Data;
using SistemaVeredas.Models;
using SistemaVeredas.Models.ViewModels;
using SistemaVeredas.Services;

namespace SistemaVeredas.Controllers
{
    public class VeredasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FotoService _fotos;
        private readonly PaqueteService _paquetes;

        public VeredasController(AppDbContext context, FotoService fotos, PaqueteService paquetes)
        {
            _context = context;
            _fotos = fotos;
            _paquetes = paquetes;
        }

        // GET: Veredas
        // TotalM2 y TotalCordonM3 están en la vereda: no hace falta traer las roturas.
        public async Task<IActionResult> Index()
        {
            var veredas = _context.Veredas
                .Include(v => v.Paquete)
                .Include(v => v.Proveedor);

            // Paquetes para "Agregar al paquete…" (RF-PAQ-08), los más nuevos primero.
            ViewData["Paquetes"] = (await _context.Paquetes.AsNoTracking()
                    .OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id)
                    .Select(p => new { p.Id, p.Nombre, p.Fecha })
                    .ToListAsync())
                .Select(p => new SelectListItem($"{p.Nombre} ({p.Fecha:dd/MM/yyyy})", p.Id.ToString()))
                .ToList();

            return View(await veredas.ToListAsync());
        }

        // POST: Veredas/AgregarAPaquete
        // Selección múltiple desde el listado (RF-PAQ-08). Vuelve al listado con el resultado.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarAPaquete(int? paqueteId, List<int>? veredaIds)
        {
            var ids = veredaIds ?? new List<int>();
            if (ids.Count > PaqueteService.MaximoIdsPorPedido)
            {
                return BadRequest();
            }

            if (paqueteId == null)
            {
                TempData["Error"] = "Elegí el paquete al que querés agregar las veredas.";
                return RedirectToAction(nameof(Index));
            }

            if (ids.Count == 0)
            {
                TempData["Error"] = "Elegí al menos una vereda.";
                return RedirectToAction(nameof(Index));
            }

            var resultado = await _paquetes.AgregarVeredasAsync(paqueteId.Value, ids);
            if (!resultado.PaqueteExiste)
            {
                TempData["Error"] = "El paquete que elegiste ya no existe. Elegí otro.";
                return RedirectToAction(nameof(Index));
            }

            var (exito, aviso) = PaqueteService.ArmarMensajes(resultado);
            if (exito != null)
            {
                TempData["Exito"] = exito;
                TempData["ExitoEnlace"] = Url.Action("Details", "Paquetes", new { id = paqueteId.Value });
                TempData["ExitoEnlaceTexto"] = "Ver paquete";
            }
            if (aviso != null) TempData["Aviso"] = aviso;
            return RedirectToAction(nameof(Index));
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
                .Include(v => v.Roturas).ThenInclude(r => r.TipoSuelo)
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
            CargarListas(null, new List<RoturaFilaViewModel>());
            return View();
        }

        // POST: Veredas/Create
        // "Fotos" no se bindea: lo arma el servidor a partir de los archivos subidos.
        // TotalM2, TotalCordonM3 y Roturas tampoco: los calcula MedicionService.
        // "tiposRotura" son los desplegables de los pozos, en el mismo orden que los términos.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> Create([Bind("Id,Nombre,Apellido,Calle,EntreCalle1,EntreCalle2,Altura,Ubicacion,Observacion,FechaReclamo,FechaRelevo,Prioridad,Estado,ACargoFrentista,ProveedorId,PaqueteId,Medicion,TieneCordon,MedicionCordon")] Vereda vereda,
            List<IFormFile>? archivos, List<int?>? tiposRotura)
        {
            var errorFotos = FotoService.Validar(archivos);
            if (errorFotos != null) ModelState.AddModelError("Fotos", errorFotos);

            var tipos = tiposRotura ?? new List<int?>();
            foreach (var e in MedicionService.AplicarAVereda(vereda, tipos))
                ModelState.AddModelError(e.Campo, e.Mensaje);
            await ValidarTiposSueloAsync(vereda);

            if (ModelState.IsValid)
            {
                _context.Add(vereda); // también guarda las roturas nuevas
                await _context.SaveChangesAsync(); // primero se guarda para tener el Id

                var rutas = await _fotos.GuardarAsync(vereda.Id, archivos);
                if (rutas.Count > 0)
                {
                    vereda.Fotos = FotoService.Unir(rutas);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction(nameof(Index));
            }
            CargarListas(vereda, FilasDesdeFormulario(vereda, tipos));
            return View(vereda);
        }

        // GET: Veredas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var vereda = await _context.Veredas
                .Include(v => v.Roturas)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (vereda == null)
            {
                return NotFound();
            }
            var filas = vereda.Roturas
                .OrderBy(r => r.Orden)
                .Select(r => new RoturaFilaViewModel
                {
                    Orden = r.Orden,
                    Medidas = r.Medidas,
                    SubtotalM2 = r.SubtotalM2,
                    TipoSueloId = r.TipoSueloId
                })
                .ToList();
            CargarListas(vereda, filas);
            return View(vereda);
        }

        // POST: Veredas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Nombre,Apellido,Calle,EntreCalle1,EntreCalle2,Altura,Ubicacion,Observacion,FechaReclamo,FechaRelevo,Prioridad,Estado,ACargoFrentista,ProveedorId,PaqueteId,Medicion,TieneCordon,MedicionCordon")] Vereda vereda,
            List<IFormFile>? archivos, List<string>? fotosAEliminar, List<int?>? tiposRotura)
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

            var tipos = tiposRotura ?? new List<int?>();
            foreach (var e in MedicionService.AplicarAVereda(vereda, tipos))
                ModelState.AddModelError(e.Campo, e.Mensaje);
            await ValidarTiposSueloAsync(vereda);

            if (ModelState.IsValid)
            {
                // Las fotos marcadas se borran del disco recién después de guardar en la base:
                // si el SaveChanges falla, la vereda sigue teniendo sus fotos.
                var rutas = FotoService.Separar(fotosActuales);
                var aBorrar = (fotosAEliminar ?? new List<string>()).Where(rutas.Contains).Distinct().ToList();
                rutas.RemoveAll(aBorrar.Contains);
                var nuevas = await _fotos.GuardarAsync(vereda.Id, archivos);
                rutas.AddRange(nuevas);
                vereda.Fotos = FotoService.Unir(rutas);

                try
                {
                    // Se reemplazan las roturas de la vereda en un solo SaveChanges.
                    var actuales = await _context.Roturas.Where(r => r.VeredaId == vereda.Id).ToListAsync();
                    _context.Roturas.RemoveRange(actuales);
                    foreach (var r in vereda.Roturas) r.VeredaId = vereda.Id;
                    _context.Update(vereda); // las roturas con Id = 0 quedan como nuevas
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    foreach (var ruta in nuevas) _fotos.Eliminar(ruta);
                    if (!VeredaExists(vereda.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch
                {
                    // Falló la base: se quitan las fotos nuevas que ya se habían guardado.
                    foreach (var ruta in nuevas) _fotos.Eliminar(ruta);
                    throw;
                }

                foreach (var ruta in aBorrar) _fotos.Eliminar(ruta);
                return RedirectToAction(nameof(Index));
            }
            CargarListas(vereda, FilasDesdeFormulario(vereda, tipos));
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
        // Las roturas se borran en cascada.
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

        // Desplegables del formulario y renglones de pozos a mostrar.
        private void CargarListas(Vereda? vereda, List<RoturaFilaViewModel> filas)
        {
            ViewData["PaqueteId"] = new SelectList(_context.Paquetes, "Id", "Nombre", vereda?.PaqueteId);
            ViewData["ProveedorId"] = new SelectList(_context.Proveedores, "Id", "Nombre", vereda?.ProveedorId);

            // Tipos disponibles, más los que ya usan los pozos aunque hoy no estén disponibles.
            var enUso = filas.Where(f => f.TipoSueloId != null).Select(f => f.TipoSueloId!.Value).Distinct().ToList();
            var tipos = _context.TiposSuelo
                .Where(t => t.Disponible || enUso.Contains(t.Id))
                .OrderBy(t => t.Tipo).ThenBy(t => t.Medida)
                .ToList();
            ViewData["TiposSuelo"] = tipos
                .Select(t => new SelectListItem(
                    t.Disponible ? t.Descripcion : t.Descripcion + " (no disponible)",
                    t.Id.ToString()))
                .ToList();

            // Para los pozos nuevos solo se ofrecen los disponibles (SPEC-001 §5.3).
            ViewData["TiposSueloNuevos"] = tipos
                .Where(t => t.Disponible)
                .Select(t => new SelectListItem(t.Descripcion, t.Id.ToString()))
                .ToList();

            ViewData["Roturas"] = filas;
        }

        // Renglones para volver a mostrar el formulario después de un POST con errores:
        // de los términos de la fórmula (si es válida) con el tipo recibido en la misma posición,
        // o solo de los tipos recibidos si la fórmula tiene errores (el JS vuelve a armarlos al cargar).
        private static List<RoturaFilaViewModel> FilasDesdeFormulario(Vereda vereda, List<int?> tipos)
        {
            var calculo = MedicionService.Calcular(vereda.Medicion, 2);
            if (calculo.EsValido)
            {
                return calculo.Terminos
                    .Select((t, i) => new RoturaFilaViewModel
                    {
                        Orden = t.Orden,
                        Medidas = t.Medidas,
                        SubtotalM2 = t.Subtotal,
                        TipoSueloId = i < tipos.Count ? tipos[i] : null
                    })
                    .ToList();
            }

            return tipos
                .Select((t, i) => new RoturaFilaViewModel { Orden = i + 1, TipoSueloId = t })
                .ToList();
        }

        // Cada tipo de suelo de los pozos tiene que existir en el catálogo.
        private async Task ValidarTiposSueloAsync(Vereda vereda)
        {
            if (vereda.Roturas.Count == 0) return;

            var ids = vereda.Roturas.Select(r => r.TipoSueloId).Distinct().ToList();
            var existentes = await _context.TiposSuelo
                .Where(t => ids.Contains(t.Id))
                .Select(t => t.Id)
                .ToListAsync();

            foreach (var r in vereda.Roturas.Where(r => !existentes.Contains(r.TipoSueloId)))
                ModelState.AddModelError($"tiposRotura[{r.Orden - 1}]", MedicionService.MensajeTipoInexistente(r.Orden));
        }
    }
}
