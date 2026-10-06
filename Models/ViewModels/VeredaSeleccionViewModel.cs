using SistemaVeredas.Models.Enums;

namespace SistemaVeredas.Models.ViewModels
{
    // Una vereda sin paquete en la pantalla "Agregar veredas".
    public class VeredaSeleccionViewModel
    {
        public int Id { get; set; }
        public int Codigo { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string? EntreCalles { get; set; }
        public Estado Estado { get; set; }
        public Prioridad? Prioridad { get; set; }
        public bool EstaMedida { get; set; }
        public decimal? TotalM2 { get; set; }
        public decimal? TotalCordonM3 { get; set; }
        public DateTime? FechaReclamo { get; set; }
        public string TextoBusqueda { get; set; } = string.Empty;   // dirección + entre calles, en minúsculas
    }
}
