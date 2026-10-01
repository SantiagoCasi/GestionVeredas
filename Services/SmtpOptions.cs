namespace SistemaVeredas.Services
{
    // Datos del servidor de correo (sección "Smtp" de la configuración).
    // Usuario, Contrasena y Remitente van en User Secrets (desarrollo) o en appsettings.Production.json (hosting),
    // nunca en archivos versionados.
    public class SmtpOptions
    {
        public const string Seccion = "Smtp";

        public string? Host { get; set; }
        public int Puerto { get; set; } = 587;
        public string? Usuario { get; set; }
        public string? Contrasena { get; set; }
        public string? Remitente { get; set; }   // dirección "De:"; si está vacía se usa Usuario

        public bool EstaConfigurado =>
            !string.IsNullOrWhiteSpace(Host) && Puerto > 0 &&
            !string.IsNullOrWhiteSpace(Usuario) && !string.IsNullOrWhiteSpace(Contrasena);
    }
}
