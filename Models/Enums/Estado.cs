using System.ComponentModel.DataAnnotations;

namespace SistemaVeredas.Models.Enums
{
    public enum Estado
    {
        [Display(Name = "Sin definir")]
        SinDefinir,

        [Display(Name = "Falta medir")]
        FaltaMedir,

        [Display(Name = "Falta foto")]
        FaltaFoto,

        [Display(Name = "Lista para arreglar")]
        ListaParaArreglar,

        [Display(Name = "Aún no se arregla")]
        AunNoSeArregla,

        [Display(Name = "En proceso")]
        EnProceso,

        [Display(Name = "Finalizado")]
        Finalizado,

        [Display(Name = "No corresponde")]
        NoCorresponde,

        [Display(Name = "No se encontró")]
        NoSeEncontro
    }
}
