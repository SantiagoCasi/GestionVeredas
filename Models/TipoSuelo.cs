using System.ComponentModel.DataAnnotations;

namespace SistemaVeredas.Models
{
    public class TipoSuelo
    {
        public int Id { get; set; }

        // Ej.: "Vainilla 6", "Cemento alisado", "Cordón".
        [Required(ErrorMessage = "El tipo es obligatorio.")]
        [StringLength(100)]
        public string Tipo { get; set; } = string.Empty;

        // Ej.: "20x20", "40x40".
        [StringLength(20)]
        public string? Medida { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        public bool Disponible { get; set; } = true;

        public ICollection<Medicion> Mediciones { get; set; } = new List<Medicion>();
    }
}
