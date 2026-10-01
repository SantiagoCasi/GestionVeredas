using System.ComponentModel.DataAnnotations; // Importa atributos para validación de datos


namespace SistemaVeredas.Models.ViewModels // Define el espacio de nombres para modelos de vista
{
    public class RecoveryPasswordViewModel // Modelo para la vista de recuperación de contraseña con token y dos contraseñas
    {
        public string? token { get; set; } // Token enviado para validar la recuperación(puede ser nulo)


        // Nueva contraseña: de 8 a 50 caracteres (RN-16).
        [Required(ErrorMessage = "Escribí la nueva contraseña.")]
        [StringLength(50, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 50 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nueva contraseña")]
        public string? UsContrasena { get; set; }


        // Confirmación de la contraseña nueva.
        [Required(ErrorMessage = "Repetí la nueva contraseña.")]
        [Compare(nameof(UsContrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Repetí la contraseña")]
        public string? UsContrasena2 { get; set; }
    }
}
