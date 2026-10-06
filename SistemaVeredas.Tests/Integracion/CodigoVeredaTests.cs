using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Models;

namespace SistemaVeredas.Tests.Integracion
{
    // Código numérico de la vereda para el usuario final (RF-VER-18, D-30).
    [Collection("Base de pruebas")]
    public class CodigoVeredaTests
    {
        private readonly AppFactory _f;

        public CodigoVeredaTests(AppFactory f)
        {
            _f = f;
        }

        private static string CalleUnica() => $"Prueba {Guid.NewGuid():N}";

        private async Task<int> SiguienteLibreAsync()
        {
            await using var db = _f.CrearDbContextDirecto();
            return (await db.Veredas.MaxAsync(v => (int?)v.Codigo) ?? 0) + 1;
        }

        private async Task<Vereda?> BuscarAsync(string calle)
        {
            await using var db = _f.CrearDbContextDirecto();
            return await db.Veredas.AsNoTracking().SingleOrDefaultAsync(v => v.Calle == calle);
        }

        private static Task<HttpResponseMessage> Crear(HttpClient c, string calle, string? codigo, bool automatico)
        {
            var campos = new List<KeyValuePair<string, string>>
            {
                new("Calle", calle),
                new("Estado", "SinDefinir")
            };
            if (codigo != null) campos.Add(new("Codigo", codigo));
            if (automatico) campos.Add(new("codigoAutomatico", "true"));
            return c.PostFormAsync("/Veredas/Create", "/Veredas/Create", campos);
        }

        [Fact]
        public async Task Crear_ConCodigoManual_LoGuarda()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var codigo = await SiguienteLibreAsync() + 1000;
            var calle = CalleUnica();

            var r = await Crear(c, calle, codigo.ToString(), false);

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal(codigo, (await BuscarAsync(calle))!.Codigo);
        }

        [Fact]
        public async Task Crear_Automatico_AsignaElSiguienteLibre()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var esperado = await SiguienteLibreAsync();
            var calle = CalleUnica();

            var r = await Crear(c, calle, null, true);

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var v = await BuscarAsync(calle);
            Assert.NotNull(v);
            Assert.True(v!.Codigo >= esperado);
        }

        [Fact]
        public async Task Crear_CodigoRepetido_NoDejaGuardar()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var codigo = await SiguienteLibreAsync() + 2000;
            await Crear(c, CalleUnica(), codigo.ToString(), false);
            var segunda = CalleUnica();

            var r = await Crear(c, segunda, codigo.ToString(), false);

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains($"Ya existe una vereda con el código {codigo}.", await r.LeerHtmlAsync());
            Assert.Null(await BuscarAsync(segunda));
        }

        [Theory]
        [InlineData("")]
        [InlineData("abc")]
        [InlineData("0")]
        [InlineData("-3")]
        [InlineData("1,5")]
        public async Task Crear_CodigoInvalido_NoDejaGuardar(string codigo)
        {
            var c = await _f.CrearClienteConSesionAsync();
            var calle = CalleUnica();

            var r = await Crear(c, calle, codigo, false);

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Null(await BuscarAsync(calle));
        }

        [Fact]
        public async Task Editar_PuedeCambiarElCodigo_SiEstaLibre()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var calle = CalleUnica();
            await Crear(c, calle, null, true);
            var v = (await BuscarAsync(calle))!;
            var nuevo = await SiguienteLibreAsync() + 3000;

            var r = await c.PostFormAsync($"/Veredas/Edit/{v.Id}", $"/Veredas/Edit/{v.Id}", new List<KeyValuePair<string, string>>
            {
                new("Id", v.Id.ToString()),
                new("Codigo", nuevo.ToString()),
                new("Calle", calle),
                new("Estado", "SinDefinir")
            });

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal(nuevo, (await BuscarAsync(calle))!.Codigo);
        }

        [Fact]
        public async Task Editar_CodigoDeOtraVereda_NoDejaGuardar()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var calleA = CalleUnica();
            var calleB = CalleUnica();
            await Crear(c, calleA, null, true);
            await Crear(c, calleB, null, true);
            var a = (await BuscarAsync(calleA))!;
            var b = (await BuscarAsync(calleB))!;

            var r = await c.PostFormAsync($"/Veredas/Edit/{b.Id}", $"/Veredas/Edit/{b.Id}", new List<KeyValuePair<string, string>>
            {
                new("Id", b.Id.ToString()),
                new("Codigo", a.Codigo.ToString()),
                new("Calle", calleB),
                new("Estado", "SinDefinir")
            });

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains($"Ya existe una vereda con el código {a.Codigo}.", await r.LeerHtmlAsync());
            Assert.Equal(b.Codigo, (await BuscarAsync(calleB))!.Codigo);
        }

        [Fact]
        public async Task Editar_ConservarSuPropioCodigo_Funciona()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var calle = CalleUnica();
            await Crear(c, calle, null, true);
            var v = (await BuscarAsync(calle))!;

            var r = await c.PostFormAsync($"/Veredas/Edit/{v.Id}", $"/Veredas/Edit/{v.Id}", new List<KeyValuePair<string, string>>
            {
                new("Id", v.Id.ToString()),
                new("Codigo", v.Codigo.ToString()),
                new("Calle", calle),
                new("Estado", "EnProceso")
            });

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal(v.Codigo, (await BuscarAsync(calle))!.Codigo);
        }

        [Fact]
        public async Task CodigoDisponible_AvisaSiYaExiste()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var calle = CalleUnica();
            await Crear(c, calle, null, true);
            var v = (await BuscarAsync(calle))!;

            using var ocupado = JsonDocument.Parse(await c.GetStringAsync($"/Veredas/CodigoDisponible?codigo={v.Codigo}&id=0"));
            Assert.False(ocupado.RootElement.GetProperty("disponible").GetBoolean());

            // La misma vereda editándose a sí misma: está disponible.
            using var propio = JsonDocument.Parse(await c.GetStringAsync($"/Veredas/CodigoDisponible?codigo={v.Codigo}&id={v.Id}"));
            Assert.True(propio.RootElement.GetProperty("disponible").GetBoolean());

            using var libre = JsonDocument.Parse(await c.GetStringAsync($"/Veredas/CodigoDisponible?codigo={await SiguienteLibreAsync() + 5000}&id=0"));
            Assert.True(libre.RootElement.GetProperty("disponible").GetBoolean());
        }

        [Fact]
        public async Task LaBase_RechazaCodigosRepetidos()
        {
            var codigo = await SiguienteLibreAsync() + 6000;
            await using var db = _f.CrearDbContextDirecto();
            db.Veredas.Add(new Vereda { Codigo = codigo, Calle = CalleUnica(), Altura = "1" });
            db.Veredas.Add(new Vereda { Codigo = codigo, Calle = CalleUnica(), Altura = "2" });

            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }
}
