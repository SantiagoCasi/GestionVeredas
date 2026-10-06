using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Data;
using SistemaVeredas.Models.Enums;
using SistemaVeredas.Models.ViewModels;

namespace SistemaVeredas.Services
{
    // Lo que necesita CalcularTotales de cada vereda.
    public sealed record VeredaTotales(Estado Estado, decimal? TotalM2, decimal? TotalCordonM3);

    // Asignación de veredas a paquetes y detalle del paquete (SPEC-002).
    // Los controladores solo llaman al servicio, arman el mensaje y redirigen.
    public class PaqueteService
    {
        // Límite defensivo de ids por POST (RNF-14 habla de 1.000 veredas).
        public const int MaximoIdsPorPedido = 1000;

        private readonly AppDbContext _db;

        public PaqueteService(AppDbContext db)
        {
            _db = db;
        }

        // Detalle del paquete con sus veredas y totales. null si no existe.
        public async Task<PaqueteDetalleViewModel?> ObtenerDetalleAsync(int paqueteId)
        {
            var paquete = await _db.Paquetes.AsNoTracking()
                .Where(p => p.Id == paqueteId)
                .Select(p => new
                {
                    p.Id,
                    p.Nombre,
                    p.Fecha,
                    p.Observacion,
                    ProveedorNombre = p.Proveedor != null ? p.Proveedor.Nombre : null,
                    ProveedorApellido = p.Proveedor != null ? p.Proveedor.Apellido : null
                })
                .FirstOrDefaultAsync();
            if (paquete == null) return null;

            // Solo los campos que se muestran (sin roturas: los totales están en la vereda).
            var filas = await _db.Veredas.AsNoTracking()
                .Where(v => v.PaqueteId == paqueteId)
                .OrderBy(v => v.Calle).ThenBy(v => v.Altura)
                .Select(v => new
                {
                    v.Id, v.Codigo, v.Calle, v.Altura, v.EntreCalle1, v.EntreCalle2,
                    v.Estado, v.Prioridad, v.TotalM2, v.TotalCordonM3, v.Fotos
                })
                .ToListAsync();

            var veredas = filas.Select(v => new VeredaEnPaqueteViewModel
            {
                Id = v.Id,
                Codigo = v.Codigo,
                Direccion = Direccion(v.Calle, v.Altura),
                EntreCalles = EntreCalles(v.EntreCalle1, v.EntreCalle2),
                Estado = v.Estado,
                Prioridad = v.Prioridad,
                EstaMedida = v.TotalM2 != null || v.TotalCordonM3 != null,
                TotalM2 = v.TotalM2,
                TotalCordonM3 = v.TotalCordonM3,
                FotoMiniatura = FotoService.Separar(v.Fotos).FirstOrDefault()
            }).ToList();

            return new PaqueteDetalleViewModel
            {
                Id = paquete.Id,
                Nombre = paquete.Nombre,
                Fecha = paquete.Fecha,
                ProveedorNombre = $"{paquete.ProveedorNombre} {paquete.ProveedorApellido}".Trim(),
                Observacion = paquete.Observacion,
                Veredas = veredas,
                Totales = CalcularTotales(veredas.Select(v => new VeredaTotales(v.Estado, v.TotalM2, v.TotalCordonM3)))
            };
        }

        // Datos de la pantalla "Agregar veredas". null si el paquete no existe.
        public async Task<AgregarVeredasViewModel?> ObtenerParaAgregarAsync(int paqueteId)
        {
            var nombre = await _db.Paquetes.AsNoTracking()
                .Where(p => p.Id == paqueteId)
                .Select(p => p.Nombre)
                .FirstOrDefaultAsync();
            if (nombre == null) return null;

            return new AgregarVeredasViewModel
            {
                PaqueteId = paqueteId,
                PaqueteNombre = nombre,
                Veredas = await VeredasSinPaqueteAsync()
            };
        }

        // Todas las veredas sin paquete, sin importar el estado (propuesta de P-13). Sin fotos (RNF-14).
        public async Task<List<VeredaSeleccionViewModel>> VeredasSinPaqueteAsync()
        {
            var filas = await _db.Veredas.AsNoTracking()
                .Where(v => v.PaqueteId == null)
                .OrderBy(v => v.Calle).ThenBy(v => v.Altura)
                .Select(v => new
                {
                    v.Id, v.Codigo, v.Calle, v.Altura, v.EntreCalle1, v.EntreCalle2,
                    v.Estado, v.Prioridad, v.TotalM2, v.TotalCordonM3, v.FechaReclamo
                })
                .ToListAsync();

            return filas.Select(v =>
            {
                var direccion = Direccion(v.Calle, v.Altura);
                var entre = EntreCalles(v.EntreCalle1, v.EntreCalle2);
                return new VeredaSeleccionViewModel
                {
                    Id = v.Id,
                    Codigo = v.Codigo,
                    Direccion = direccion,
                    EntreCalles = entre,
                    Estado = v.Estado,
                    Prioridad = v.Prioridad,
                    EstaMedida = v.TotalM2 != null || v.TotalCordonM3 != null,
                    TotalM2 = v.TotalM2,
                    TotalCordonM3 = v.TotalCordonM3,
                    FechaReclamo = v.FechaReclamo,
                    TextoBusqueda = $"{v.Codigo} {direccion} {entre}".Trim().ToLowerInvariant()
                };
            }).ToList();
        }

        // Asigna las veredas al paquete sin pisar nunca un PaqueteId que ya no sea NULL (RN-10).
        public async Task<ResultadoAsignacion> AgregarVeredasAsync(int paqueteId, IReadOnlyCollection<int> veredaIds)
        {
            var ids = veredaIds.Distinct().ToList();
            var paquete = await _db.Paquetes.AsNoTracking()
                .Where(p => p.Id == paqueteId)
                .Select(p => new { p.Id, p.Nombre })
                .FirstOrDefaultAsync();
            if (paquete == null) return ResultadoAsignacion.PaqueteInexistente();

            // 1) Las que ya estaban en ESTE paquete (no cuentan como agregadas ni como salteadas).
            var yaEstaban = await _db.Veredas.AsNoTracking()
                .Where(v => ids.Contains(v.Id) && v.PaqueteId == paqueteId)
                .Select(v => v.Id)
                .ToListAsync();

            // 2) UPDATE ... SET PaqueteId = @p WHERE Id IN (...) AND PaqueteId IS NULL (atómico por fila).
            try
            {
                await _db.Veredas
                    .Where(v => ids.Contains(v.Id) && v.PaqueteId == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(v => v.PaqueteId, (int?)paqueteId));
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is SqlException)
            {
                // El paquete se eliminó en el medio: la FK rechaza el UPDATE.
                var sigue = await _db.Paquetes.AsNoTracking().AnyAsync(p => p.Id == paqueteId);
                if (!sigue) return ResultadoAsignacion.PaqueteInexistente();
                throw;
            }

            // 3) Se relee el estado final: es la verdad, aunque otro usuario haya asignado en el medio.
            var final = await _db.Veredas.AsNoTracking()
                .Where(v => ids.Contains(v.Id))
                .Select(v => new
                {
                    v.Id,
                    v.Calle,
                    v.Altura,
                    v.PaqueteId,
                    PaqueteNombre = v.Paquete != null ? v.Paquete.Nombre : null
                })
                .ToListAsync();

            var resultado = new ResultadoAsignacion
            {
                PaqueteExiste = true,
                PaqueteNombre = paquete.Nombre,
                Agregadas = final.Count(v => v.PaqueteId == paqueteId && !yaEstaban.Contains(v.Id)),
                YaEstaban = yaEstaban.Count,
                DireccionesYaEstaban = final
                    .Where(v => yaEstaban.Contains(v.Id))
                    .Select(v => Direccion(v.Calle, v.Altura))
                    .ToList(),
                Salteadas = final
                    .Where(v => v.PaqueteId != null && v.PaqueteId != paqueteId)
                    .OrderBy(v => v.Calle).ThenBy(v => v.Altura)
                    .Select(v => (Direccion(v.Calle, v.Altura), v.PaqueteNombre ?? string.Empty))
                    .ToList(),
                Inexistentes = ids.Count - final.Count
            };
            return resultado;
        }

        // Quita la vereda del paquete solo si todavía está en él. false si ya no estaba.
        public async Task<bool> QuitarVeredaAsync(int paqueteId, int veredaId)
        {
            var filas = await _db.Veredas
                .Where(v => v.Id == veredaId && v.PaqueteId == paqueteId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.PaqueteId, (int?)null));
            return filas > 0;
        }

        // Totales del paquete: cantidad, m², m³, sin medir y % de finalizadas.
        public static TotalesPaquete CalcularTotales(IEnumerable<VeredaTotales> veredas)
        {
            var lista = veredas.ToList();
            var cantidad = lista.Count;
            var finalizadas = lista.Count(v => v.Estado == Estado.Finalizado);

            return new TotalesPaquete
            {
                Cantidad = cantidad,
                TotalM2 = lista.Sum(v => v.TotalM2 ?? 0m),
                TotalM3 = lista.Sum(v => v.TotalCordonM3 ?? 0m),
                SinMedir = lista.Count(v => v.TotalM2 == null && v.TotalCordonM3 == null),
                Finalizadas = finalizadas,
                PorcentajeAvance = cantidad == 0
                    ? 0
                    : (int)Math.Round(finalizadas * 100m / cantidad, MidpointRounding.AwayFromZero)
            };
        }

        // Mensajes para TempData a partir del resultado (SPEC-002, sección 5).
        public static (string? Exito, string? Aviso) ArmarMensajes(ResultadoAsignacion r)
        {
            string? exito = null;
            if (r.Agregadas == 1)
                exito = $"Se agregó 1 vereda al paquete «{r.PaqueteNombre}».";
            else if (r.Agregadas > 1)
                exito = $"Se agregaron {r.Agregadas} veredas al paquete «{r.PaqueteNombre}».";

            var avisos = new List<string>();

            if (r.Salteadas.Count == 1)
            {
                var (direccion, otro) = r.Salteadas[0];
                avisos.Add($"No se agregó {direccion} porque ya está en el paquete «{otro}».");
            }
            else if (r.Salteadas.Count > 1)
            {
                const int maximo = 10;
                var lista = string.Join(", ", r.Salteadas.Take(maximo).Select(s => $"{s.Direccion} («{s.Paquete}»)"));
                var resto = r.Salteadas.Count - maximo;
                if (resto > 0) lista += $" y {resto} más";
                avisos.Add($"No se agregaron {r.Salteadas.Count} veredas porque ya están en otro paquete: {lista}.");
            }

            if (r.YaEstaban == 1)
                avisos.Add($"{r.DireccionesYaEstaban.FirstOrDefault() ?? "Una vereda"} ya estaba en este paquete.");
            else if (r.YaEstaban > 1)
                avisos.Add($"{r.YaEstaban} veredas ya estaban en este paquete.");

            if (r.Inexistentes == 1)
                avisos.Add("Una de las veredas que elegiste ya no existe.");
            else if (r.Inexistentes > 1)
                avisos.Add($"{r.Inexistentes} de las veredas que elegiste ya no existen.");

            return (exito, avisos.Count == 0 ? null : string.Join(" ", avisos));
        }

        // "Calle 123" (como Vereda.Direccion, que no se puede usar en las consultas).
        public static string Direccion(string calle, string? altura) =>
            string.IsNullOrWhiteSpace(altura) ? calle : $"{calle} {altura}";

        // "entre X y Y", o null si no tiene entre calles.
        public static string? EntreCalles(string? calle1, string? calle2)
        {
            var calles = new[] { calle1, calle2 }.Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
            return calles.Count == 0 ? null : "entre " + string.Join(" y ", calles);
        }
    }
}
