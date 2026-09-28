namespace SistemaVeredas.Services
{
    // Guarda las fotos de las veredas en wwwroot/uploads/veredas/{id}/
    // En la base (campo Vereda.Fotos) solo se guardan las rutas separadas por ";".
    public class FotoService
    {
        public const long TamanioMaximo = 10 * 1024 * 1024; // 10 MB por foto
        private static readonly string[] ExtensionesPermitidas = { ".jpg", ".jpeg", ".png", ".webp" };

        private readonly IWebHostEnvironment _env;

        public FotoService(IWebHostEnvironment env)
        {
            _env = env;
        }

        // Devuelve un mensaje de error si algún archivo no es válido, o null si están todos bien.
        public static string? Validar(IEnumerable<IFormFile>? archivos)
        {
            if (archivos == null) return null;

            foreach (var archivo in archivos.Where(a => a.Length > 0))
            {
                var ext = Path.GetExtension(archivo.FileName).ToLowerInvariant();
                if (!ExtensionesPermitidas.Contains(ext))
                    return $"\"{archivo.FileName}\" no es una imagen válida (jpg, png o webp).";
                if (archivo.Length > TamanioMaximo)
                    return $"\"{archivo.FileName}\" supera los 10 MB.";
            }
            return null;
        }

        // Guarda los archivos y devuelve las rutas relativas (ej. "uploads/veredas/5/abc.jpg").
        public async Task<List<string>> GuardarAsync(int veredaId, IEnumerable<IFormFile>? archivos)
        {
            var rutas = new List<string>();
            if (archivos == null) return rutas;

            var carpeta = CarpetaDe(veredaId);
            Directory.CreateDirectory(carpeta);

            foreach (var archivo in archivos.Where(a => a.Length > 0))
            {
                // Nombre aleatorio: evita pisar archivos y que se use el nombre original para atacar rutas.
                var nombre = $"{Guid.NewGuid():N}{Path.GetExtension(archivo.FileName).ToLowerInvariant()}";
                using var stream = new FileStream(Path.Combine(carpeta, nombre), FileMode.Create);
                await archivo.CopyToAsync(stream);
                rutas.Add($"uploads/veredas/{veredaId}/{nombre}");
            }
            return rutas;
        }

        public void Eliminar(string rutaRelativa)
        {
            // Solo se borran archivos dentro de uploads/veredas.
            if (!rutaRelativa.StartsWith("uploads/veredas/")) return;

            var ruta = Path.GetFullPath(Path.Combine(_env.WebRootPath, rutaRelativa));
            var raiz = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads", "veredas"));
            if (ruta.StartsWith(raiz) && File.Exists(ruta)) File.Delete(ruta);
        }

        public void EliminarCarpeta(int veredaId)
        {
            var carpeta = CarpetaDe(veredaId);
            if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true);
        }

        private string CarpetaDe(int veredaId) =>
            Path.Combine(_env.WebRootPath, "uploads", "veredas", veredaId.ToString());

        public static List<string> Separar(string? fotos) =>
            string.IsNullOrWhiteSpace(fotos)
                ? new List<string>()
                : fotos.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        public static string? Unir(IEnumerable<string> rutas)
        {
            var texto = string.Join(";", rutas);
            return texto.Length == 0 ? null : texto;
        }
    }
}
