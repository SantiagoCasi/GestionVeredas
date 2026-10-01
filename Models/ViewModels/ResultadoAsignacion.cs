namespace SistemaVeredas.Models.ViewModels
{
    // Resultado de agregar veredas a un paquete (RN-10).
    public class ResultadoAsignacion
    {
        public bool PaqueteExiste { get; set; }
        public string PaqueteNombre { get; set; } = string.Empty;

        // Quedaron en este paquete y antes no estaban.
        public int Agregadas { get; set; }

        // Ya estaban en este paquete antes de asignar.
        public int YaEstaban { get; set; }
        public List<string> DireccionesYaEstaban { get; set; } = new();

        // Están en otro paquete: no se tocaron.
        public List<(string Direccion, string Paquete)> Salteadas { get; set; } = new();

        // Ids pedidos que ya no existen.
        public int Inexistentes { get; set; }

        public static ResultadoAsignacion PaqueteInexistente() => new() { PaqueteExiste = false };
    }
}
