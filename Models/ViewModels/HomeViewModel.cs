namespace SistemaVeredas.Models.ViewModels
{
    // Resumen que se muestra en las dos tarjetas de la página principal.
    public class HomeViewModel
    {
        public int TotalVeredas { get; set; }
        public int VeredasPendientes { get; set; }
        public int VeredasEnProceso { get; set; }
        public int VeredasFinalizadas { get; set; }

        public int TotalPaquetes { get; set; }
        public int VeredasEnPaquetes { get; set; }
        public int VeredasSinPaquete { get; set; }

        public decimal SuperficieTotal { get; set; }
    }
}
