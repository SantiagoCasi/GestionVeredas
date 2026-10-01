using System.Net;

namespace SistemaVeredas.Tests.Integracion
{
    [Collection("Base de pruebas")]
    public class AccesoTests
    {
        private readonly AppFactory _f;

        public AccesoTests(AppFactory f)
        {
            _f = f;
        }

        [Fact]
        public async Task SinSesion_RedirigeAlLogin()
        {
            var c = _f.CrearCliente();

            var r = await c.GetAsync("/Veredas");

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.StartsWith("/Access/Login", r.Destino());
        }

        [Fact]
        public async Task LoginCorrecto_Entra()
        {
            var c = await _f.CrearClienteConSesionAsync();

            var r = await c.GetAsync("/Veredas");

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        [Fact]
        public async Task LoginIncorrecto_MuestraMensaje()
        {
            var c = _f.CrearCliente();

            var r = await c.PostFormAsync("/Access/Login", "/Access/Login", new Dictionary<string, string>
            {
                ["UsEmail"] = _f.EmailPrueba,
                ["UsContrasena"] = "contraseña-equivocada",
                ["Recordarme"] = "false"
            });

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("Email o contraseña incorrectos.", await r.LeerHtmlAsync());
        }

        [Fact]
        public async Task PostSinToken_Rechaza()
        {
            var c = _f.CrearCliente();

            var r = await c.PostAsync("/Access/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UsEmail"] = _f.EmailPrueba,
                ["UsContrasena"] = _f.ContrasenaPrueba
            }));

            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }
    }
}
