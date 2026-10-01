using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVeredas.Models
{
    // Un pozo (término de la medición) de la vereda, con su tipo de suelo obligatorio.
    // Lo arma el servidor con MedicionService a partir de la fórmula.
    public class Rotura
    {
        public int Id { get; set; }

        public int VeredaId { get; set; }
        public Vereda? Vereda { get; set; }

        // Posición del término en la fórmula, desde 1.
        [Display(Name = "Pozo")]
        public int Orden { get; set; }

        // El término normalizado, sin paréntesis: "2*3" o "4,5*8".
        [Required, StringLength(500)]
        public string Medidas { get; set; } = string.Empty;

        [Display(Name = "Tipo de suelo")]
        public int TipoSueloId { get; set; }
        public TipoSuelo? TipoSuelo { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Subtotal (m²)")]
        public decimal SubtotalM2 { get; set; }
    }
}
