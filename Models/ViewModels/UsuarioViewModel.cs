using System.ComponentModel.DataAnnotations;

namespace SistemaVeredas.Models.ViewModels
{
    // Para mostrar un usuario (Index, Details, Delete). No expone contraseña ni token.
    public class UsuarioViewModel
    {
        public int UsId { get; set; }

        [Display(Name = "Nombre")]
        public string UsNombre { get; set; } = string.Empty;

        [Display(Name = "Apellido")]
        public string UsApellido { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string UsEmail { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool UsActivo { get; set; }

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; }

        [Display(Name = "Último acceso")]
        public DateTime? UltimoAcceso { get; set; }

        public static UsuarioViewModel Desde(Usuario u) => new()
        {
            UsId = u.UsId,
            UsNombre = u.UsNombre,
            UsApellido = u.UsApellido,
            UsEmail = u.UsEmail,
            UsActivo = u.UsActivo,
            FechaCreacion = u.FechaCreacion,
            UltimoAcceso = u.UltimoAcceso
        };
    }
}
