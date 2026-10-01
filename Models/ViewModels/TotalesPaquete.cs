namespace SistemaVeredas.Models.ViewModels
{
    // Totales del paquete (RF-PAQ-04). Los calcula PaqueteService.CalcularTotales.
    public class TotalesPaquete
    {
        public int Cantidad { get; set; }
        public decimal TotalM2 { get; set; }
        public decimal TotalM3 { get; set; }
        public int SinMedir { get; set; }
        public int Finalizadas { get; set; }
        public int PorcentajeAvance { get; set; }
    }
}
