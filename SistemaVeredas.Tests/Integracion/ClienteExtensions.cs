using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SistemaVeredas.Tests.Integracion
{
    // Login real y token antifalsificación (SPEC-004, sección 4.7).
    public static class ClienteExtensions
    {
        static readonly Regex Token = new(
            "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled);

        public static HttpClient CrearCliente(this WebApplicationFactory<Program> f) => f.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,              // para afirmar sobre los 302
            BaseAddress = new Uri("https://localhost")
        });

        public static async Task<string> ObtenerTokenAsync(this HttpClient c, string url)
        {
            var html = await c.GetStringAsync(url);
            var m = Token.Match(html);
            Assert.True(m.Success, $"No se encontró el token antifalsificación en {url}");
            return WebUtility.HtmlDecode(m.Groups[1].Value);
        }

        public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient c, string urlFormulario,
            string urlPost, IEnumerable<KeyValuePair<string, string>> campos)
        {
            var token = await c.ObtenerTokenAsync(urlFormulario);
            var datos = new List<KeyValuePair<string, string>>(campos)
            {
                new("__RequestVerificationToken", token)
            };
            return await c.PostAsync(urlPost, new FormUrlEncodedContent(datos));
        }

        public static Task<HttpClient> CrearClienteConSesionAsync(this AppFactory f) =>
            f.CrearClienteConSesionAsync(f.EmailPrueba, f.ContrasenaPrueba);

        public static async Task<HttpClient> CrearClienteConSesionAsync(this WebApplicationFactory<Program> f, string email, string contrasena)
        {
            var c = f.CrearCliente();
            var r = await c.PostFormAsync("/Access/Login", "/Access/Login", new Dictionary<string, string>
            {
                ["UsEmail"] = email,
                ["UsContrasena"] = contrasena,
                ["Recordarme"] = "false"
            });
            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            return c;
        }

        // Ruta del encabezado Location, sea absoluto (login de cookies) o relativo (RedirectToAction).
        public static string Destino(this HttpResponseMessage r)
        {
            var u = r.Headers.Location;
            Assert.NotNull(u);
            return u!.IsAbsoluteUri ? u.PathAndQuery : u.OriginalString;
        }

        // El HTML de Razor codifica los acentos (&#xED;): se decodifica para buscar los mensajes.
        public static async Task<string> LeerHtmlAsync(this HttpResponseMessage r) =>
            WebUtility.HtmlDecode(await r.Content.ReadAsStringAsync());
    }
}
