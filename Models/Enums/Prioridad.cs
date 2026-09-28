using System.ComponentModel.DataAnnotations;

namespace SistemaVeredas.Models.Enums
{
    public enum Prioridad
    {
        [Display(Name = "Baja")]
        Baja,

        [Display(Name = "Media")]
        Media,

        [Display(Name = "Alta")]
        Alta
    }
}
