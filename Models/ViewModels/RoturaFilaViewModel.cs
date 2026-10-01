namespace SistemaVeredas.Models.ViewModels
{
    // Un renglón de pozo en el formulario de Crear/Editar vereda, para dibujarlo desde el servidor.
    public class RoturaFilaViewModel
    {
        public int Orden { get; set; }            // 1, 2, 3…
        public string? Medidas { get; set; }      // "2*3"; null si la fórmula tiene errores
        public decimal? SubtotalM2 { get; set; }
        public int? TipoSueloId { get; set; }
    }
}
