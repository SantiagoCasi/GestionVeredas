using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SistemaVeredas.Data;

namespace SistemaVeredas.Tests.Integracion
{
    // Levanta la app en memoria contra la base de pruebas (SPEC-004, sección 4.6).
    // La base se borra al empezar la corrida y la app la vuelve a crear con las migraciones.
    public class AppFactory : WebApplicationFactory<Program>, IAsyncLifetime
    {
        public const string BaseDefault =
            "Server=DESKTOP-DTLN15N;Database=GestionVeredas_Pruebas;Trusted_Connection=True;Encrypt=False;";

        public string CadenaConexion { get; } =
            Environment.GetEnvironmentVariable("SISTEMAVEREDAS_TEST_DB") is { Length: > 0 } cs ? cs : BaseDefault;

        // Usuario de prueba: se siembra con UsuarioInicial (Program.cs lo crea si la tabla Usuarios está vacía).
        public string EmailPrueba => "pruebas@sistemaveredas.local";
        public string ContrasenaPrueba { get; } = Guid.NewGuid().ToString("N"); // 32 caracteres, distinta en cada corrida

        private readonly List<IServiceScope> _scopes = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Entorno Development (el default de WebApplicationFactory): así funcionan los archivos estáticos
            // de MapStaticAssets. Como en Development se leen los User Secrets de Santiago, TODO lo que importa
            // se pisa acá abajo.
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DBSV"] = CadenaConexion,
                    ["UsuarioInicial:Email"] = EmailPrueba,
                    ["UsuarioInicial:Contrasena"] = ContrasenaPrueba,
                    ["UsuarioInicial:Nombre"] = "Usuario",
                    ["UsuarioInicial:Apellido"] = "Pruebas",
                    ["Smtp:Usuario"] = "",       // sin credenciales reales (SPEC-003)
                    ["Smtp:Contrasena"] = "",
                });
            });

            builder.ConfigureTestServices(services =>
            {
                // Doble seguro: se reemplaza el DbContext por uno con la cadena de pruebas.
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>(); // EF Core 9 acumula configuraciones
                services.AddDbContext<AppDbContext>(o => o.UseSqlServer(CadenaConexion));
            });
        }

        public async Task InitializeAsync()
        {
            VerificarQueEsBaseDePruebas(CadenaConexion); // lanza si no termina en _Pruebas
            await using (var db = CrearDbContextDirecto())
            {
                await db.Database.EnsureDeletedAsync();
            }
            _ = Server; // arranca la app: Program.cs aplica las migraciones y siembra el usuario
        }

        // Implementación explícita: WebApplicationFactory ya tiene un DisposeAsync que devuelve ValueTask.
        // La base no se borra al terminar: queda para revisarla si algo falló.
        async Task IAsyncLifetime.DisposeAsync()
        {
            foreach (var s in _scopes) s.Dispose();
            _scopes.Clear();
            await base.DisposeAsync();
        }

        // DbContext de la app (el del contenedor de servicios): sirve para confirmar que usa la base de pruebas.
        public AppDbContext CrearDbContext()
        {
            var scope = Services.CreateScope();
            lock (_scopes) _scopes.Add(scope);
            return scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }

        // DbContext propio, independiente de la app (p. ej., para probar la concurrencia con dos contextos).
        public AppDbContext CrearDbContextDirecto()
        {
            var opciones = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(CadenaConexion).Options;
            return new AppDbContext(opciones);
        }

        public static void VerificarQueEsBaseDePruebas(string cadena)
        {
            var nombre = new SqlConnectionStringBuilder(cadena).InitialCatalog;
            if (string.IsNullOrEmpty(nombre) || !nombre.EndsWith("_Pruebas", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La base de pruebas tiene que terminar en _Pruebas. Revisá SISTEMAVEREDAS_TEST_DB.");
        }
    }
}
