using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Models;

namespace SistemaVeredas.Tests.Integracion
{
    // Tabla Roturas y medición de la vereda por el formulario real (SPEC-001, SPEC-004 sección 4.8).
    [Collection("Base de pruebas")]
    public class RoturasTests
    {
        private readonly AppFactory _f;

        public RoturasTests(AppFactory f)
        {
            _f = f;
        }

        private static string CalleUnica() => $"Prueba {Guid.NewGuid():N}";

        private async Task<int> CrearTipoAsync()
        {
            await using var db = _f.CrearDbContextDirecto();
            var t = new TipoSuelo { Tipo = $"Tipo {Guid.NewGuid():N}", Medida = "40x40", Disponible = true };
            db.TiposSuelo.Add(t);
            await db.SaveChangesAsync();
            return t.Id;
        }

        private static List<KeyValuePair<string, string>> Campos(string calle, string? medicion, params int?[] tipos)
        {
            var campos = new List<KeyValuePair<string, string>>
            {
                new("Calle", calle),
                new("Estado", "SinDefinir")
            };
            if (medicion != null) campos.Add(new("Medicion", medicion));
            for (var i = 0; i < tipos.Length; i++)
                campos.Add(new($"tiposRotura[{i}]", tipos[i]?.ToString() ?? string.Empty));
            return campos;
        }

        private static Task<HttpResponseMessage> CrearVeredaAsync(HttpClient c, List<KeyValuePair<string, string>> campos) =>
            c.PostFormAsync("/Veredas/Create", "/Veredas/Create", campos);

        private async Task<Vereda?> BuscarAsync(string calle)
        {
            await using var db = _f.CrearDbContextDirecto();
            return await db.Veredas.AsNoTracking()
                .Include(v => v.Roturas)
                .SingleOrDefaultAsync(v => v.Calle == calle);
        }

        [Fact]
        public async Task Crear_GuardaUnaRoturaPorTermino()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipoA = await CrearTipoAsync();
            var tipoB = await CrearTipoAsync();
            var calle = CalleUnica();

            var r = await CrearVeredaAsync(c, Campos(calle, "(2*3)+(5*9)", tipoA, tipoB));

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var v = await BuscarAsync(calle);
            Assert.NotNull(v);
            Assert.Equal("(2*3)+(5*9)", v!.Medicion);
            Assert.Equal(51.00m, v.TotalM2);
            var roturas = v.Roturas.OrderBy(x => x.Orden).ToList();
            Assert.Equal(2, roturas.Count);
            Assert.Equal(new[] { 1, 2 }, roturas.Select(x => x.Orden));
            Assert.Equal(new[] { "2*3", "5*9" }, roturas.Select(x => x.Medidas));
            Assert.Equal(new[] { tipoA, tipoB }, roturas.Select(x => x.TipoSueloId));
            Assert.Equal(new[] { 6.00m, 45.00m }, roturas.Select(x => x.SubtotalM2));
        }

        [Fact]
        public async Task Crear_MismoTipoEnVariosPozos()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();
            var calle = CalleUnica();

            var r = await CrearVeredaAsync(c, Campos(calle, "(2*3)+(5*9)+(0,5*0,5)", tipo, tipo, tipo));

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var v = await BuscarAsync(calle);
            Assert.Equal(3, v!.Roturas.Count);
            Assert.All(v.Roturas, x => Assert.Equal(tipo, x.TipoSueloId));
            Assert.Equal(51.25m, v.TotalM2);
        }

        [Fact]
        public async Task Crear_TerminoSinTipoDeSuelo_NoGuarda()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();
            var calle = CalleUnica();

            var r = await CrearVeredaAsync(c, Campos(calle, "(2*3)+(5*9)", tipo, null));

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("Elegí el tipo de suelo del pozo 2.", await r.LeerHtmlAsync());
            Assert.Null(await BuscarAsync(calle));
        }

        [Fact]
        public async Task Crear_TipoInexistente_NoGuarda()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var calle = CalleUnica();

            var r = await CrearVeredaAsync(c, Campos(calle, "2*3", int.MaxValue));

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("El tipo de suelo del pozo 1 no existe. Elegí otro de la lista.", await r.LeerHtmlAsync());
            Assert.Null(await BuscarAsync(calle));
        }

        [Fact]
        public async Task Crear_FormulaConErrores_MuestraTodosLosTerminos()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var calle = CalleUnica();

            var r = await CrearVeredaAsync(c, Campos(calle, "59+2*3+7"));

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            var html = await r.LeerHtmlAsync();
            Assert.Contains("El término 1 («59») tiene 1 medida y lleva 2.", html);
            Assert.Contains("El término 3 («7») tiene 1 medida y lleva 2.", html);
            Assert.Null(await BuscarAsync(calle));
        }

        // O-01: una fórmula que entra en el maxlength pero normalizada pasa de 500 no da error 500.
        [Fact]
        public async Task Crear_FormulaNormalizadaDeMasDe500_NoGuarda()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();
            var calle = CalleUnica();
            var formula = string.Join("+", Enumerable.Repeat("1*1", 84));

            var r = await CrearVeredaAsync(c, Campos(calle, formula, Enumerable.Repeat<int?>(tipo, 84).ToArray()));

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("La medición puede tener hasta 500 caracteres.", await r.LeerHtmlAsync());
            Assert.Null(await BuscarAsync(calle));
        }

        // O-01 y O-07: en Edit, el error de largo no guarda nada y las fotos siguen en la vereda.
        [Fact]
        public async Task Editar_FormulaDeMasDe500_NoCambiaNada()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();
            var calle = CalleUnica();
            await CrearVeredaAsync(c, Campos(calle, "2*3", tipo));
            var id = (await BuscarAsync(calle))!.Id;
            const string foto = "uploads/veredas/prueba/foto.jpg";
            await using (var db = _f.CrearDbContextDirecto())
            {
                await db.Veredas.Where(v => v.Id == id).ExecuteUpdateAsync(s => s.SetProperty(v => v.Fotos, foto));
            }

            var campos = Campos(calle, string.Join("+", Enumerable.Repeat("1*1", 84)), Enumerable.Repeat<int?>(tipo, 84).ToArray());
            campos.Add(new("Id", id.ToString()));
            campos.Add(new("fotosAEliminar", foto));
            var r = await c.PostFormAsync($"/Veredas/Edit/{id}", $"/Veredas/Edit/{id}", campos);

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("La medición puede tener hasta 500 caracteres.", await r.LeerHtmlAsync());
            var v2 = await BuscarAsync(calle);
            Assert.Equal("(2*3)", v2!.Medicion);
            Assert.Equal(foto, v2.Fotos);
            Assert.Single(v2.Roturas);
        }

        [Fact]
        public async Task Editar_ReemplazaLasRoturas()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();
            var calle = CalleUnica();
            await CrearVeredaAsync(c, Campos(calle, "(2*3)+(5*9)", tipo, tipo));
            var id = (await BuscarAsync(calle))!.Id;

            var campos = Campos(calle, "(4*5)", tipo);
            campos.Add(new("Id", id.ToString()));
            var r = await c.PostFormAsync($"/Veredas/Edit/{id}", $"/Veredas/Edit/{id}", campos);

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            await using var db = _f.CrearDbContextDirecto();
            Assert.Equal(1, await db.Roturas.CountAsync(x => x.VeredaId == id));
            var v = await BuscarAsync(calle);
            Assert.Equal(20.00m, v!.TotalM2);
            Assert.Equal("(4*5)", v.Medicion);
            Assert.Equal("4*5", v.Roturas.Single().Medidas);
        }

        [Fact]
        public async Task Crear_SinMedicion()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();
            var calle = CalleUnica();

            var r = await CrearVeredaAsync(c, Campos(calle, "", tipo));

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var v = await BuscarAsync(calle);
            Assert.Empty(v!.Roturas);
            Assert.Null(v.TotalM2);
            Assert.Null(v.Medicion);
            Assert.False(v.EstaMedida);
        }

        [Fact]
        public async Task Cordon_SeGuardaEnVeredas()
        {
            var c = await _f.CrearClienteConSesionAsync();

            var conCordon = CalleUnica();
            var campos = Campos(conCordon, null);
            campos.Add(new("TieneCordon", "true"));
            campos.Add(new("MedicionCordon", "1*0,5*0,3"));
            Assert.Equal(HttpStatusCode.Redirect, (await CrearVeredaAsync(c, campos)).StatusCode);

            var v = await BuscarAsync(conCordon);
            Assert.True(v!.TieneCordon);
            Assert.Equal(0.150m, v.TotalCordonM3);
            Assert.Equal("(1*0,5*0,3)", v.MedicionCordon);
            Assert.Empty(v.Roturas);
            Assert.True(v.EstaMedida);

            var sinCasilla = CalleUnica();
            campos = Campos(sinCasilla, null);
            campos.Add(new("TieneCordon", "false"));
            campos.Add(new("MedicionCordon", "1*0,5*0,3"));
            Assert.Equal(HttpStatusCode.Redirect, (await CrearVeredaAsync(c, campos)).StatusCode);

            var w = await BuscarAsync(sinCasilla);
            Assert.False(w!.TieneCordon);
            Assert.Null(w.MedicionCordon);
            Assert.Null(w.TotalCordonM3);
        }

        [Fact]
        public async Task Eliminar_BorraSusRoturas()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();
            var calle = CalleUnica();
            await CrearVeredaAsync(c, Campos(calle, "(2*3)+(5*9)", tipo, tipo));
            var id = (await BuscarAsync(calle))!.Id;

            var r = await c.PostFormAsync($"/Veredas/Delete/{id}", $"/Veredas/Delete/{id}", new Dictionary<string, string>
            {
                ["Id"] = id.ToString()
            });

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            await using var db = _f.CrearDbContextDirecto();
            Assert.False(await db.Veredas.AnyAsync(x => x.Id == id));
            Assert.Equal(0, await db.Roturas.CountAsync(x => x.VeredaId == id));
        }

        [Fact]
        public async Task TipoSueloEnUso_NoSeBorra()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();
            await CrearVeredaAsync(c, Campos(CalleUnica(), "2*3", tipo));

            // RN-13: desde la app se marca como no disponible y no se borra.
            var r = await c.PostFormAsync($"/TipoSuelos/Delete/{tipo}", $"/TipoSuelos/Delete/{tipo}", new Dictionary<string, string>
            {
                ["Id"] = tipo.ToString()
            });
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);

            await using (var db = _f.CrearDbContextDirecto())
            {
                var t = await db.TiposSuelo.AsNoTracking().SingleOrDefaultAsync(x => x.Id == tipo);
                Assert.NotNull(t);
                Assert.False(t!.Disponible);
            }

            // Y la FK Restrict impide borrarlo directo en la base.
            await using (var db = _f.CrearDbContextDirecto())
            {
                db.TiposSuelo.Remove(await db.TiposSuelo.SingleAsync(x => x.Id == tipo));
                await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
            }
        }

        [Fact]
        public async Task TipoSueloSinUso_SeBorra()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();

            var r = await c.PostFormAsync($"/TipoSuelos/Delete/{tipo}", $"/TipoSuelos/Delete/{tipo}", new Dictionary<string, string>
            {
                ["Id"] = tipo.ToString()
            });

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            await using var db = _f.CrearDbContextDirecto();
            Assert.False(await db.TiposSuelo.AnyAsync(x => x.Id == tipo));
        }

        [Fact]
        public async Task ElTotalLoCalculaElServidor()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var tipo = await CrearTipoAsync();
            var calle = CalleUnica();
            var campos = Campos(calle, "2*3", tipo);
            campos.Add(new("TotalM2", "999"));
            campos.Add(new("TotalCordonM3", "999"));
            campos.Add(new("Roturas[0].SubtotalM2", "999"));
            campos.Add(new("SubtotalM2", "999"));

            var r = await CrearVeredaAsync(c, campos);

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var v = await BuscarAsync(calle);
            Assert.Equal(6.00m, v!.TotalM2);
            Assert.Null(v.TotalCordonM3);
            Assert.Equal(6.00m, v.Roturas.Single().SubtotalM2);
        }

        [Fact]
        public async Task AltaRapidaDeTipoDeSuelo()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var token = await c.ObtenerTokenAsync("/Veredas/Create");
            var nombre = $"Rápido {Guid.NewGuid():N}";

            async Task<HttpResponseMessage> Enviar(string tipo, string medida)
            {
                var pedido = new HttpRequestMessage(HttpMethod.Post, "/TipoSuelos/CrearRapido")
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["Tipo"] = tipo, ["Medida"] = medida })
                };
                pedido.Headers.Add("RequestVerificationToken", token);
                return await c.SendAsync(pedido);
            }

            // Se crea.
            var r1 = await Enviar("  " + nombre + "  ", "40x40");
            Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
            using var j1 = JsonDocument.Parse(await r1.Content.ReadAsStringAsync());
            var id = j1.RootElement.GetProperty("id").GetInt32();
            Assert.Equal($"{nombre} (40x40)", j1.RootElement.GetProperty("texto").GetString());

            // El mismo (sin distinguir mayúsculas) devuelve el existente.
            var r2 = await Enviar(nombre.ToUpperInvariant(), "40X40");
            Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
            using var j2 = JsonDocument.Parse(await r2.Content.ReadAsStringAsync());
            Assert.Equal(id, j2.RootElement.GetProperty("id").GetInt32());
            Assert.True(j2.RootElement.GetProperty("existente").GetBoolean());

            // Nombre vacío: 400 con el mensaje.
            var r3 = await Enviar("   ", "");
            Assert.Equal(HttpStatusCode.BadRequest, r3.StatusCode);
            using var j3 = JsonDocument.Parse(await r3.Content.ReadAsStringAsync());
            Assert.Contains("El tipo es obligatorio.",
                j3.RootElement.GetProperty("errores").EnumerateArray().Select(e => e.GetString()));
        }
    }
}
