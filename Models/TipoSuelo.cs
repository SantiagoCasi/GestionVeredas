using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVeredas.Models
{
    // Catálogo de baldosas del mercado con su medida. No guarda mediciones.
    public class TipoSuelo
    {
        public int Id { get; set; }

        // Ej.: "Vainilla 6", "Cemento alisado".
        [Required(ErrorMessage = "El tipo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El tipo puede tener hasta 100 caracteres.")]
        public string Tipo { get; set; } = string.Empty;

        // Ej.: "20x20", "40x40".
        [StringLength(20, ErrorMessage = "La medida puede tener hasta 20 caracteres.")]
        public string? Medida { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        public bool Disponible { get; set; } = true;

        // Texto para los desplegables, el detalle y el alta rápida: "Vainilla (40x40)".
        [NotMapped]
        public string Descripcion => string.IsNullOrWhiteSpace(Medida) ? Tipo : $"{Tipo} ({Medida})";

        public ICollection<Rotura> Roturas { get; set; } = new List<Rotura>();
    }
}
