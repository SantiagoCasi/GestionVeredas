using System.ComponentModel.DataAnnotations;

namespace SistemaVeredas.Models
{
    // Albañil o contratista que repara las veredas.
    public class Proveedor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Apellido { get; set; }

        [Phone]
        [StringLength(30)]
        [Display(Name = "Teléfono")]
        public string? Telefono { get; set; }

        public ICollection<Paquete> Paquetes { get; set; } = new List<Paquete>();

        public ICollection<Vereda> Veredas { get; set; } = new List<Vereda>();
    }
}
