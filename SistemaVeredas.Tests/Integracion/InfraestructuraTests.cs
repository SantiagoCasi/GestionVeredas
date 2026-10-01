using Microsoft.EntityFrameworkCore;

namespace SistemaVeredas.Tests.Integracion
{
    [Collection("Base de pruebas")]
    public class InfraestructuraTests
    {
        private readonly AppFactory _f;

        public InfraestructuraTests(AppFactory f)
        {
            _f = f;
        }

        [Fact]
        public void UsaLaBaseDePruebas()
        {
            var db = _f.CrearDbContext();
            Assert.EndsWith("_Pruebas", db.Database.GetDbConnection().Database);
        }

        [Fact]
        public async Task NoQuedanMigracionesPendientes()
        {
            var db = _f.CrearDbContext();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        }

        // El snapshot y la migración escritos a mano coinciden con el modelo (R-05).
        [Fact]
        public void ElModeloNoTieneCambiosSinMigracion()
        {
            var db = _f.CrearDbContext();
            Assert.False(db.Database.HasPendingModelChanges());
        }

        [Fact]
        public async Task NoExisteLaTablaMediciones()
        {
            var db = _f.CrearDbContext();
            var id = await db.Database
                .SqlQueryRaw<int?>("SELECT OBJECT_ID('dbo.Mediciones') AS [Value]")
                .SingleAsync();
            Assert.Null(id);
        }

        [Fact]
        public async Task ExisteLaTablaRoturas()
        {
            var db = _f.CrearDbContext();
            var id = await db.Database
                .SqlQueryRaw<int?>("SELECT OBJECT_ID('dbo.Roturas') AS [Value]")
                .SingleAsync();
            Assert.NotNull(id);
        }

        [Theory]
        [InlineData("Server=x;Database=GestionVeredas;Trusted_Connection=True;")]
        [InlineData("Server=x;Database=GestionVeredas_Pruebas2;Trusted_Connection=True;")]
        [InlineData("Server=x;Trusted_Connection=True;")]
        public void Traba_RechazaBasesQueNoSonDePruebas(string cadena)
        {
            var ex = Assert.Throws<InvalidOperationException>(() => AppFactory.VerificarQueEsBaseDePruebas(cadena));
            Assert.Equal("La base de pruebas tiene que terminar en _Pruebas. Revisá SISTEMAVEREDAS_TEST_DB.", ex.Message);
        }
    }
}
