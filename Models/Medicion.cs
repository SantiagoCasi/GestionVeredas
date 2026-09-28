using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVeredas.Models
{
    // Un rectángulo de la vereda con su tipo de suelo. Una vereda puede tener varios.
    public class Medicion
    {
        public int Id { get; set; }

        [Display(Name = "Vereda")]
        public int VeredaId { get; set; }
        public Vereda? Vereda { get; set; }

        [Display(Name = "Tipo de suelo")]
        public int TipoSueloId { get; set; }
        public TipoSuelo? TipoSuelo { get; set; }

        // Ej.: "A", "B", "Pozo", "Cordón".
        [StringLength(50)]
        public string? Sector { get; set; }

        [Range(0.01, 9999.99)]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Largo (m)")]
        public decimal Largo { get; set; }

        // Sin ancho, la medición es en metros lineales (ej. cordón).
        [Range(0.01, 9999.99)]
        [Column(TypeName = "decimal(6,2)")]
        [Display(Name = "Ancho (m)")]
        public decimal? Ancho { get; set; }

        [NotMapped]
        [Display(Name = "Superficie (m²)")]
        public decimal? Superficie => Ancho.HasValue ? Largo * Ancho.Value : null;
    }
}
