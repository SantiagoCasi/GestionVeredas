namespace SistemaVeredas.Models.ViewModels
{
    // Pantalla "Agregar veredas" del paquete (RF-PAQ-02).
    public class AgregarVeredasViewModel
    {
        public int PaqueteId { get; set; }
        public string PaqueteNombre { get; set; } = string.Empty;
        public List<VeredaSeleccionViewModel> Veredas { get; set; } = new();

        // Para el POST: las veredas marcadas.
        public List<int> VeredaIds { get; set; } = new();
    }
}
