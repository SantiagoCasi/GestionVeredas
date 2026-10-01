using System.ComponentModel.DataAnnotations;

namespace SistemaVeredas.Models.ViewModels
{
    public class UsuarioCreateViewModel
    {
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

        // Largo de la contraseña que escribe el usuario (RN-16). La columna guarda el hash.
        [Required(ErrorMessage = "Escribí la contraseña.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 50 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string UsContrasena { get; set; } = string.Empty;

        [Required(ErrorMessage = "Repetí la contraseña.")]
        [Compare(nameof(UsContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar contraseña")]
        public string ConfirmarContrasena { get; set; } = string.Empty;

        [Display(Name = "Activo")]
        public bool UsActivo { get; set; } = true;
    }
}
