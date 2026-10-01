using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;

namespace SistemaVeredas.Services
{
    public enum ResultadoEnvio { Enviado, NoConfigurado, Fallo }

    // Interfaz para poder reemplazar el envío real por uno falso en las pruebas.
    public interface IEmailService
    {
        Task<ResultadoEnvio> EnviarRecuperacionAsync(string destino, string enlace);
    }

    // Envía el mail de recuperación de contraseña con los datos de SmtpOptions.
    public class EmailService : IEmailService
    {
        public const string Asunto = "Restablecé tu contraseña de SistemaVeredas";

        private readonly SmtpOptions _opciones;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<SmtpOptions> opciones, ILogger<EmailService> logger)
        {
            _opciones = opciones.Value;
            _logger = logger;
        }

        public async Task<ResultadoEnvio> EnviarRecuperacionAsync(string destino, string enlace)
        {
            if (!_opciones.EstaConfigurado)
            {
                _logger.LogWarning("SMTP sin configurar: falta la sección Smtp.");
                return ResultadoEnvio.NoConfigurado;
            }

            try
            {
                var remitente = string.IsNullOrWhiteSpace(_opciones.Remitente) ? _opciones.Usuario! : _opciones.Remitente!;

                using var mensaje = new MailMessage(remitente, destino, Asunto, ArmarCuerpo(enlace))
                {
                    IsBodyHtml = true
                };

                using var cliente = new SmtpClient(_opciones.Host!, _opciones.Puerto)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Contrasena)
                };

                await cliente.SendMailAsync(mensaje);
                return ResultadoEnvio.Enviado;
            }
            catch (Exception ex) when (ex is SmtpException or InvalidOperationException or SocketException
                                           or IOException or FormatException)
            {
                // Nunca se escriben en el log el usuario ni la contraseña del SMTP.
                _logger.LogError(ex, "No se pudo enviar el mail de recuperación a {Destino}.", destino);
                return ResultadoEnvio.Fallo;
            }
        }

        // Cuerpo HTML del mail (SPEC-003, sección 3.3).
        public static string ArmarCuerpo(string enlace)
        {
            var enlaceEscapado = HtmlEncoder.Default.Encode(enlace);
            return $@"
<div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 650px; margin: 0 auto; padding: 20px;'>
    <h2 style='color: #004d80;'>Restablecer la contraseña</h2>
    <p>Hola:</p>
    <p>Recibimos un pedido para restablecer la contraseña de tu usuario en <strong>SistemaVeredas</strong>.</p>
    <p>Si lo pediste vos, hacé clic en el botón para elegir una contraseña nueva:</p>
    <div style='text-align: center; margin: 25px 0;'>
        <a href='{enlaceEscapado}'
           style='display: inline-block; padding: 12px 24px; background-color: #004d80; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'>
            Elegir contraseña nueva
        </a>
    </div>
    <p>El enlace sirve una sola vez.</p>
    <p>Si no lo pediste, ignorá este mail: tu contraseña no cambia.</p>
    <hr style='border: 0; border-top: 1px solid #eee; margin: 30px 0;' />
    <p style='font-size: 0.9em; color: #666;'>— SistemaVeredas</p>
</div>";
        }
    }
}
