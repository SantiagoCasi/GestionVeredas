using System.ComponentModel.DataAnnotations;
using System.Reflection;
using SistemaVeredas.Models.Enums;

namespace SistemaVeredas.Extensions
{
    public static class EnumExtensions
    {
        // Devuelve el texto de [Display(Name = "...")] del valor del enum, o su nombre si no tiene.
        public static string GetDisplayName(this Enum? valor)
        {
            if (valor == null) return string.Empty;

            var miembro = valor.GetType().GetMember(valor.ToString()).FirstOrDefault();
            return miembro?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? valor.ToString();
        }

        // Color de Bootstrap para mostrar el estado como etiqueta.
        public static string BadgeClass(this Estado estado) => estado switch
        {
            Estado.Finalizado => "text-bg-success",
            Estado.EnProceso => "text-bg-primary",
            Estado.ListaParaArreglar => "text-bg-info",
            Estado.FaltaMedir or Estado.FaltaFoto => "text-bg-warning",
            Estado.AunNoSeArregla => "text-bg-secondary",
            Estado.NoCorresponde or Estado.NoSeEncontro => "text-bg-dark",
            _ => "text-bg-light border"
        };

        public static string BadgeClass(this Prioridad? prioridad) => prioridad switch
        {
            Prioridad.Alta => "text-bg-danger",
            Prioridad.Media => "text-bg-warning",
            Prioridad.Baja => "text-bg-success",
            _ => "text-bg-light border"
        };
    }
}
