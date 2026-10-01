namespace SistemaVeredas.Models.ViewModels
{
    // Detalle del paquete: datos, veredas, totales y avance (RF-PAQ-04).
    public class PaqueteDetalleViewModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string ProveedorNombre { get; set; } = string.Empty;
        public string? Observacion { get; set; }
        public List<VeredaEnPaqueteViewModel> Veredas { get; set; } = new();
        public TotalesPaquete Totales { get; set; } = new();
    }
}
