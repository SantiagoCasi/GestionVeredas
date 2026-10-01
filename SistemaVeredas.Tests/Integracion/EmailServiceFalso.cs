using SistemaVeredas.Services;

namespace SistemaVeredas.Tests.Integracion
{
    // Reemplaza al envío real: nunca se manda un mail desde las pruebas (SPEC-003).
    public class EmailServiceFalso : IEmailService
    {
        private readonly ResultadoEnvio _resultado;

        public EmailServiceFalso(ResultadoEnvio resultado)
        {
            _resultado = resultado;
        }

        public List<(string Destino, string Enlace)> Enviados { get; } = new();

        public Task<ResultadoEnvio> EnviarRecuperacionAsync(string destino, string enlace)
        {
            lock (Enviados) Enviados.Add((destino, enlace));
            return Task.FromResult(_resultado);
        }
    }
}
