using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Data;
using SistemaVeredas.Models;
using SistemaVeredas.Models.Enums;
using SistemaVeredas.Models.ViewModels;

namespace SistemaVeredas.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        // Página principal: dos tarjetas (Veredas y Paquetes) con un resumen.
        public async Task<IActionResult> Index()
        {
            var estados = await _context.Veredas.Select(v => new { v.Estado, v.PaqueteId }).ToListAsync();

            var model = new HomeViewModel
            {
                TotalVeredas = estados.Count,
                VeredasFinalizadas = estados.Count(v => v.Estado == Estado.Finalizado),
                VeredasEnProceso = estados.Count(v => v.Estado == Estado.EnProceso),
                VeredasPendientes = estados.Count(v => v.Estado != Estado.Finalizado
                                                    && v.Estado != Estado.EnProceso
                                                    && v.Estado != Estado.NoCorresponde
                                                    && v.Estado != Estado.NoSeEncontro),
                TotalPaquetes = await _context.Paquetes.CountAsync(),
                VeredasEnPaquetes = estados.Count(v => v.PaqueteId != null),
                VeredasSinPaquete = estados.Count(v => v.PaqueteId == null),
                SuperficieTotal = await _context.Veredas.SumAsync(v => v.TotalM2 ?? 0),
                CordonTotalM3 = await _context.Veredas.SumAsync(v => v.TotalCordonM3 ?? 0)
            };

            return View(model);
        }

        // Todas las veredas cargadas, en tarjetas.
        public async Task<IActionResult> Veredas()
        {
            var veredas = await _context.Veredas
                .Include(v => v.Proveedor)
                .Include(v => v.Paquete)
                .Include(v => v.Roturas).ThenInclude(r => r.TipoSuelo)
                .OrderByDescending(v => v.FechaReclamo ?? v.FechaRelevo)
                .ThenByDescending(v => v.Id)
                .AsNoTracking()
                .ToListAsync();

            return View(veredas);
        }

        // Todos los paquetes cargados, en tarjetas, con sus veredas.
        public async Task<IActionResult> Paquetes()
        {
            var paquetes = await _context.Paquetes
                .Include(p => p.Proveedor)
                .Include(p => p.Veredas)
                .OrderByDescending(p => p.Fecha)
                .AsNoTracking()
                .ToListAsync();

            return View(paquetes);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
