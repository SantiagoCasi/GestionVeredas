using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SistemaVeredas.Models;
using SistemaVeredas.Services;

namespace SistemaVeredas.Tests.Integracion
{
    // Largo de la contraseña y recuperación sin credenciales en el código (SPEC-003, sección 7).
    [Collection("Base de pruebas")]
    public class ContrasenasTests
    {
        private readonly AppFactory _f;

        public ContrasenasTests(AppFactory f)
        {
            _f = f;
        }

        private static string EmailUnico() => $"u{Guid.NewGuid():N}@pruebas.local";

        private static Dictionary<string, string> Alta(string email, string clave) => new()
        {
            ["UsNombre"] = "Ana",
            ["UsApellido"] = "Pruebas",
            ["UsEmail"] = email,
            ["UsContrasena"] = clave,
            ["ConfirmarContrasena"] = clave,
            ["UsActivo"] = "true"
        };

        private Task CrearUsuarioAsync(string email) => CrearUsuarioAsync(email, Usuario.TokenBloqueado);

        private async Task CrearUsuarioAsync(string email, string? token)
        {
            await using var db = _f.CrearDbContextDirecto();
            db.Usuarios.Add(new Usuario
            {
                UsNombre = "Recupera",
                UsApellido = "Pruebas",
                UsEmail = email,
                UsContrasena = PasswordService.HashPassword(Guid.NewGuid().ToString("N")),
                UsActivo = true,
                token_recovery = token
            });
            await db.SaveChangesAsync();
        }

        private async Task<string?> TokenDeAsync(string email)
        {
            await using var db = _f.CrearDbContextDirecto();
            return await db.Usuarios.Where(u => u.UsEmail == email).Select(u => u.token_recovery).SingleAsync();
        }

        [Fact]
        public async Task Alta_Con51Caracteres_NoSeCrea()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var email = EmailUnico();

            var r = await c.PostFormAsync("/Usuarios/Create", "/Usuarios/Create", Alta(email, new string('x', 51)));

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("La contraseña debe tener entre 8 y 50 caracteres.", await r.LeerHtmlAsync());
            await using var db = _f.CrearDbContextDirecto();
            Assert.False(await db.Usuarios.AnyAsync(u => u.UsEmail == email));
        }

        [Fact]
        public async Task Alta_Con50Caracteres_SeCreaEIniciaSesion()
        {
            var c = await _f.CrearClienteConSesionAsync();
            var email = EmailUnico();
            var clave = new string('y', 45) + "12345"; // 50 caracteres

            var r = await c.PostFormAsync("/Usuarios/Create", "/Usuarios/Create", Alta(email, clave));

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            var nuevo = await _f.CrearClienteConSesionAsync(email, clave); // afirma el 302 del login
            Assert.Equal(HttpStatusCode.OK, (await nuevo.GetAsync("/Veredas")).StatusCode);
        }

        [Fact]
        public async Task Recuperacion_SmtpSinConfigurar_MuestraMensajeYBloqueaElToken()
        {
            var email = EmailUnico();
            await CrearUsuarioAsync(email);
            var c = _f.CrearCliente();

            var r = await c.PostFormAsync("/Access/StartRecovery", "/Access/StartRecovery", new Dictionary<string, string>
            {
                ["UsEmail"] = email
            });

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("La recuperación por mail no está disponible en este momento.", await r.LeerHtmlAsync());
            Assert.Equal("tokenbloqueado", await TokenDeAsync(email));
        }

        [Fact]
        public async Task Recuperacion_MailEnviado_RedirigeAlLoginYGuardaElToken()
        {
            var falso = new EmailServiceFalso(ResultadoEnvio.Enviado);
            await using var fabrica = _f.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            {
                s.RemoveAll<IEmailService>();
                s.AddSingleton<IEmailService>(falso);
            }));
            var email = EmailUnico();
            await CrearUsuarioAsync(email);
            var c = fabrica.CrearCliente();

            var r = await c.PostFormAsync("/Access/StartRecovery", "/Access/StartRecovery", new Dictionary<string, string>
            {
                ["UsEmail"] = email
            });

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.StartsWith("/Access/Login", r.Destino());
            var token = await TokenDeAsync(email);
            Assert.NotEqual("tokenbloqueado", token);
            Assert.Equal(32, token!.Length);

            var (destino, enlace) = Assert.Single(falso.Enviados);
            Assert.Equal(email, destino);
            Assert.Contains($"/Access/Recovery?token={token}", enlace);
        }

        [Fact]
        public async Task Recuperacion_FallaElEnvio_MuestraMensajeYBloqueaElToken()
        {
            await using var fabrica = _f.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
            {
                s.RemoveAll<IEmailService>();
                s.AddSingleton<IEmailService>(new EmailServiceFalso(ResultadoEnvio.Fallo));
            }));
            var email = EmailUnico();
            await CrearUsuarioAsync(email);
            var c = fabrica.CrearCliente();

            var r = await c.PostFormAsync("/Access/StartRecovery", "/Access/StartRecovery", new Dictionary<string, string>
            {
                ["UsEmail"] = email
            });

            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            Assert.Contains("No pudimos enviar el mail.", await r.LeerHtmlAsync());
            Assert.Equal("tokenbloqueado", await TokenDeAsync(email));
        }

        private async Task<string> HashDeAsync(string email)
        {
            await using var db = _f.CrearDbContextDirecto();
            return await db.Usuarios.Where(u => u.UsEmail == email).Select(u => u.UsContrasena).SingleAsync();
        }

        // SEG-01: el valor "bloqueado" no sirve como enlace de recuperación.
        [Theory]
        [InlineData("tokenbloqueado")]
        [InlineData("TOKENBLOQUEADO")]
        [InlineData("tokenbloqueado ")]
        [InlineData("")]
        public async Task Recuperacion_GetConTokenBloqueado_VuelveAlInicio(string token)
        {
            var c = _f.CrearCliente();

            var r = await c.GetAsync($"/Access/Recovery?token={Uri.EscapeDataString(token)}");

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.StartsWith("/Access/StartRecovery", r.Destino());
        }

        [Theory]
        [InlineData("tokenbloqueado")]
        [InlineData("TOKENBLOQUEADO")]
        [InlineData("tokenbloqueado ")]
        public async Task Recuperacion_PostConTokenBloqueado_NoCambiaLaContrasena(string token)
        {
            var email = EmailUnico();
            await CrearUsuarioAsync(email);
            var hashAntes = await HashDeAsync(email);
            var hashUsuarioPrueba = await HashDeAsync(_f.EmailPrueba);
            var c = _f.CrearCliente();

            var r = await c.PostFormAsync("/Access/StartRecovery", "/Access/Recovery", new Dictionary<string, string>
            {
                ["token"] = token,
                ["UsContrasena"] = "contraseña-nueva-123",
                ["UsContrasena2"] = "contraseña-nueva-123"
            });

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.StartsWith("/Access/StartRecovery", r.Destino());
            Assert.Equal(hashAntes, await HashDeAsync(email));
            Assert.Equal(hashUsuarioPrueba, await HashDeAsync(_f.EmailPrueba));
        }

        [Fact]
        public async Task Recuperacion_TokenValido_CambiaLaContrasenaUnaSolaVez()
        {
            var email = EmailUnico();
            var token = Guid.NewGuid().ToString("N");
            await CrearUsuarioAsync(email, token);
            var c = _f.CrearCliente();

            Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/Access/Recovery?token={token}")).StatusCode);

            var campos = new Dictionary<string, string>
            {
                ["token"] = token,
                ["UsContrasena"] = "contraseña-nueva-123",
                ["UsContrasena2"] = "contraseña-nueva-123"
            };
            var r = await c.PostFormAsync($"/Access/Recovery?token={token}", "/Access/Recovery", campos);

            Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
            Assert.StartsWith("/Access/Login", r.Destino());
            Assert.Equal(PasswordService.HashPassword("contraseña-nueva-123"), await HashDeAsync(email));
            Assert.Equal(Usuario.TokenBloqueado, await TokenDeAsync(email));

            // El mismo enlace ya no sirve.
            var r2 = await c.GetAsync($"/Access/Recovery?token={token}");
            Assert.StartsWith("/Access/StartRecovery", r2.Destino());
        }

        [Fact]
        public async Task Recuperacion_SinToken_Rechaza()
        {
            var c = _f.CrearCliente();

            var r = await c.PostAsync("/Access/StartRecovery", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["UsEmail"] = _f.EmailPrueba
            }));

            Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        }
    }
}
