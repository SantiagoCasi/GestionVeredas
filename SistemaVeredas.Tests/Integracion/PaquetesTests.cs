using System.Net;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Models;
using SistemaVeredas.Models.Enums;
using SistemaVeredas.Services;

namespace SistemaVeredas.Tests.Integracion
{
    // Asignación de veredas a paquetes (SPEC-002, sección 7).
    [Collection("Base de pruebas")]
    public class PaquetesTests
    {
        private readonly AppFactory _f;

        public PaquetesTests(AppFactory f)
        {
            _f = f;
        }

        private async Task<int> CrearPaqueteAsync()
        {
            await using var db = _f.CrearDbContextDirecto();
            var proveedor = new Proveedor { Nombre = $"Proveedor {Guid.NewGuid():N}" };
            var paquete = new Paquete { Nombre = $"Paquete {Guid.NewGuid():N}", Fecha = DateTime.Today, Proveedor = proveedor };
            db.Paquetes.Add(paquete);
            await db.SaveChangesAsync();
            return paquete.Id;
        }

        private async Task<int> CrearVeredaAsync(int? paqueteId = null, Estado estado = Estado.SinDefinir)
        {
            await using var db = _f.CrearDbContextDirecto();
            // El código de la vereda es único (RF-VER-18): se toma el siguiente libre.
            var codigo = (await db.Veredas.MaxAsync(x => (int?)x.Codigo) ?? 0) + 1;
            var v = new Vereda { Codigo = codigo, Calle = $"Prueba {Guid.NewGuid():N}", Altura = "100", Estado = estado, PaqueteId = paqueteId };
            db.Veredas.Add(v);
            await db.SaveChangesAsync();
            return v.Id;
        }

        private async Task<int?> PaqueteDeAsync(int veredaId)
        {
            await using var db = _f.CrearDbContextDirecto();
            return await db.Veredas.Where(v => v.Id == veredaId).Select(v => v.PaqueteId).SingleAsync();
        }

        private static List<KeyValuePair<string, string>> Ids(IEnumerable<int> ids) =>
            ids.Select(id => new KeyValuePair<string, string>("veredaIds", id.ToString())).ToList();

        [Fact]
        public async Task AgregarTresVeredasLibres()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var paquete = await CrearPaqueteAsync();
            var ids = new[] { await CrearVeredaAsync(), await CrearVeredaAsync(), await CrearVeredaAsync() };

            var r = await c.PostFormAsync($"/Paquetes/AgregarVeredas/{paquete}", $"/Paquetes/AgregarVeredas/{paquete}", Ids(ids));

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal($"/Paquetes/Details/{paquete}", r.Destino());
            foreach (var id in ids) Assert.Equal(paquete, await PaqueteDeAsync(id));
        }

        [Fact]
        public async Task AgregarSinVeredas_MuestraError()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var paquete = await CrearPaqueteAsync();

            var r = await c.PostFormAsync($"/Paquetes/AgregarVeredas/{paquete}", $"/Paquetes/AgregarVeredas/{paquete}",
                new List<KeyValuePair<string, string>>());

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("Elegí al menos una vereda.", await r.LeerHtmlAsync());
        }

        [Fact]
        public async Task VeredaEnOtroPaquete_SeSaltea()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var otro = await CrearPaqueteAsync();
            var paquete = await CrearPaqueteAsync();
            var ocupada = await CrearVeredaAsync(otro);
            var libre = await CrearVeredaAsync();

            var r = await c.PostFormAsync($"/Paquetes/AgregarVeredas/{paquete}", $"/Paquetes/AgregarVeredas/{paquete}", Ids(new[] { ocupada, libre }));

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal(otro, await PaqueteDeAsync(ocupada));
            Assert.Equal(paquete, await PaqueteDeAsync(libre));

            // El detalle muestra el aviso de la salteada.
            var detalle = await (await c.GetAsync(r.Destino())).LeerHtmlAsync();
            Assert.Contains("porque ya está en el paquete", detalle);
        }

        [Fact]
        public async Task Quitar_DejaLaVeredaSinPaquete()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var paquete = await CrearPaqueteAsync();
            var vereda = await CrearVeredaAsync(paquete);

            var r = await c.PostFormAsync($"/Paquetes/Details/{paquete}", $"/Paquetes/QuitarVereda/{paquete}", new Dictionary<string, string>
            {
                ["veredaId"] = vereda.ToString()
            });

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Null(await PaqueteDeAsync(vereda));

            // Quitar dos veces: la segunda avisa.
            var r2 = await c.PostFormAsync($"/Paquetes/Details/{paquete}", $"/Paquetes/QuitarVereda/{paquete}", new Dictionary<string, string>
            {
                ["veredaId"] = vereda.ToString()
            });
            var detalle = await (await c.GetAsync(r2.Destino())).LeerHtmlAsync();
            Assert.Contains("Esa vereda ya no estaba en este paquete.", detalle);
        }

        [Fact]
        public async Task EliminarPaquete_LasVeredasQuedanSinPaquete()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var paquete = await CrearPaqueteAsync();
            var v1 = await CrearVeredaAsync(paquete);
            var v2 = await CrearVeredaAsync(paquete);

            var r = await c.PostFormAsync($"/Paquetes/Delete/{paquete}", $"/Paquetes/Delete/{paquete}", new Dictionary<string, string>
            {
                ["Id"] = paquete.ToString()
            });

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Null(await PaqueteDeAsync(v1));
            Assert.Null(await PaqueteDeAsync(v2));
        }

        [Fact]
        public async Task DesdeElListado_AgregarAPaquete()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var paquete = await CrearPaqueteAsync();
            var vereda = await CrearVeredaAsync();
            var campos = Ids(new[] { vereda });
            campos.Add(new("paqueteId", paquete.ToString()));

            var r = await c.PostFormAsync("/Veredas", "/Veredas/AgregarAPaquete", campos);

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Equal(paquete, await PaqueteDeAsync(vereda));
        }

        [Fact]
        public async Task DesdeElListado_SinPaquete_MuestraError()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var vereda = await CrearVeredaAsync();

            var r = await c.PostFormAsync("/Veredas", "/Veredas/AgregarAPaquete", Ids(new[] { vereda }));

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.Null(await PaqueteDeAsync(vereda));
            var listado = await (await c.GetAsync(r.Destino())).LeerHtmlAsync();
            Assert.Contains("Elegí el paquete al que querés agregar las veredas.", listado);
        }

        [Fact]
        public async Task Detalle_TotalesYAvance()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var paquete = await CrearPaqueteAsync();
            await CrearVeredaAsync(paquete, Estado.Finalizado);
            await CrearVeredaAsync(paquete, Estado.EnProceso);
            await CrearVeredaAsync(paquete, Estado.EnProceso);
            await CrearVeredaAsync(paquete, Estado.NoCorresponde);

            var html = await (await c.GetAsync($"/Paquetes/Details/{paquete}")).LeerHtmlAsync();

            Assert.Contains("25 %", html);
            Assert.Contains("1 de 4 finalizadas", html);
        }

        [Fact]
        public async Task PostSinToken_Rechaza()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var paquete = await CrearPaqueteAsync();
            var vereda = await CrearVeredaAsync();

            var r = await c.PostAsync($"/Paquetes/AgregarVeredas/{paquete}", new FormUrlEncodedContent(Ids(new[] { vereda })));

            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
            Assert.Null(await PaqueteDeAsync(vereda));
        }

        [Fact]
        public async Task PostSinSesion_RedirigeAlLogin()
        {
            var c = _f.CrearCliente();
            var paquete = await CrearPaqueteAsync();
            var vereda = await CrearVeredaAsync();

            var r = await c.PostAsync($"/Paquetes/AgregarVeredas/{paquete}", new FormUrlEncodedContent(Ids(new[] { vereda })));

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.StartsWith("/Access/Login", r.Destino());
            Assert.Null(await PaqueteDeAsync(vereda));
        }

        // RN-10: dos usuarios asignan la misma vereda a paquetes distintos a la vez.
        [Fact]
        public async Task Concurrencia_LaVeredaQuedaEnUnSoloPaquete()
        {
            var paqueteA = await CrearPaqueteAsync();
            var paqueteB = await CrearPaqueteAsync();
            var vereda = await CrearVeredaAsync();

            await using var dbA = _f.CrearDbContextDirecto();
            await using var dbB = _f.CrearDbContextDirecto();
            var servicioA = new PaqueteService(dbA);
            var servicioB = new PaqueteService(dbB);

            var resultados = await Task.WhenAll(
                servicioA.AgregarVeredasAsync(paqueteA, new[] { vereda }),
                servicioB.AgregarVeredasAsync(paqueteB, new[] { vereda }));

            var final = await PaqueteDeAsync(vereda);
            Assert.True(final == paqueteA || final == paqueteB);
            Assert.Equal(1, resultados.Sum(r => r.Agregadas));
            Assert.Equal(1, resultados.Sum(r => r.Salteadas.Count));
        }
    }
}
