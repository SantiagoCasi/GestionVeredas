using System.ComponentModel.DataAnnotations;

namespace SistemaVeredas.Models
{
    // Usuario del sistema. Solo se usa para iniciar sesión (un único tipo de usuario, sin roles).
    public class Usuario
    {
        [Key]
        public int UsId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Nombre")]
        public string UsNombre { get; set; } = null!;

        [Required]
        [StringLength(50)]
        [Display(Name = "Apellido")]
        public string UsApellido { get; set; } = null!;

        // Se usa para iniciar sesión y para recuperar la contraseña. Es único.
        [Required]
        [EmailAddress]
        [StringLength(100)]
        [Display(Name = "Email")]
        public string UsEmail { get; set; } = null!;

        // Siempre se guarda hasheada con PasswordService.
        [Required]
        [StringLength(100)]
        public string UsContrasena { get; set; } = null!;

        [Display(Name = "Activo")]
        public bool UsActivo { get; set; } = true;

        // Valor que indica que no hay un pedido de recuperación vigente. Nunca es un token válido:
        // AccessController solo acepta tokens de 32 caracteres hexadecimales.
        public const string TokenBloqueado = "tokenbloqueado";

        public string? token_recovery { get; set; } = TokenBloqueado;

        [Display(Name = "Fecha de creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Display(Name = "Último acceso")]
        public DateTime? UltimoAcceso { get; set; }
    }
}
