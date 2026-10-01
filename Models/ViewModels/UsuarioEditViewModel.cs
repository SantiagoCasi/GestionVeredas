using System.ComponentModel.DataAnnotations;

namespace SistemaVeredas.Models.ViewModels
{
    public class UsuarioEditViewModel
    {
        public int UsId { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Nombre")]
        public string UsNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [StringLength(50)]
        [Display(Name = "Apellido")]
        public string UsApellido { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string UsEmail { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool UsActivo { get; set; }

        // Opcional: si se deja vacía, se mantiene la contraseña actual (RN-16: 8 a 50 caracteres).
        [StringLength(50, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 50 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña (opcional)")]
        public string? NuevaContrasena { get; set; }

        [Compare(nameof(NuevaContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar nueva contraseña")]
        public string? ConfirmarContrasena { get; set; }
    }
}
