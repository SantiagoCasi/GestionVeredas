using SistemaVeredas.Models.Enums;

namespace SistemaVeredas.Models.ViewModels
{
    // Una vereda en el detalle del paquete.
    public class VeredaEnPaqueteViewModel
    {
        public int Id { get; set; }
        public int Codigo { get; set; }
        public string Direccion { get; set; } = string.Empty;
        public string? EntreCalles { get; set; }      // "entre X y Y", o null
        public Estado Estado { get; set; }
        public Prioridad? Prioridad { get; set; }
        public bool EstaMedida { get; set; }
        public decimal? TotalM2 { get; set; }
        public decimal? TotalCordonM3 { get; set; }
        public string? FotoMiniatura { get; set; }    // primera de ListaFotos
    }
}
