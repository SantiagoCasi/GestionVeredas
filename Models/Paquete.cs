using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaVeredas.Models
{
    // Un paquete pertenece a un solo proveedor.
    public class Paquete
    {
        public int Id { get; set; }

        // Ej.: "Marzo (02) 2026".
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
        public DateTime Fecha { get; set; } = DateTime.Today;

        [Display(Name = "Proveedor")]
        public int ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        [StringLength(1000)]
        [Display(Name = "Observación")]
        public string? Observacion { get; set; }

        public ICollection<Vereda> Veredas { get; set; } = new List<Vereda>();
    }
}
