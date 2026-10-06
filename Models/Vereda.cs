using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SistemaVeredas.Models.Enums;

namespace SistemaVeredas.Models
{
    public class Vereda
    {
        public int Id { get; set; }

        // Número de la vereda para el usuario final (1, 2, 3…). No es el Id interno y no puede repetirse.
        // El controlador lo valida a mano (único, entero mayor a 0) o lo asigna solo con "codigoAutomatico".
        [Display(Name = "Código")]
        public int Codigo { get; set; }

        [StringLength(100)]
        public string? Nombre { get; set; }

        [StringLength(100)]
        public string? Apellido { get; set; }

        [Required(ErrorMessage = "La calle es obligatoria.")]
        [StringLength(100)]
        public string Calle { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Entre calle")]
        public string? EntreCalle1 { get; set; }

        [StringLength(100)]
        [Display(Name = "Y calle")]
        public string? EntreCalle2 { get; set; }

        // Texto porque hay alturas como "53bis" o "1115 - A".
        [StringLength(20)]
        public string? Altura { get; set; }

        // Coordenadas elegidas en el mapa, con formato "latitud,longitud" (ej. "-33.8912,-61.1003").
        [StringLength(500)]
        [Display(Name = "Ubicación")]
        public string? Ubicacion { get; set; }

        [NotMapped]
        public (double Lat, double Lng)? Coordenadas
        {
            get
            {
                var partes = (Ubicacion ?? string.Empty).Split(',');
                var ci = System.Globalization.CultureInfo.InvariantCulture;
                if (partes.Length == 2
                    && double.TryParse(partes[0].Trim(), System.Globalization.NumberStyles.Float, ci, out var lat)
                    && double.TryParse(partes[1].Trim(), System.Globalization.NumberStyles.Float, ci, out var lng))
                {
                    return (lat, lng);
                }
                return null;
            }
        }

        // "lat,lng" con punto decimal, listo para usar en URLs.
        [NotMapped]
        public string? PuntoUrl => Coordenadas is { } c
            ? string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{c.Lat},{c.Lng}")
            : null;

        // Links de Google Maps (gratis, no necesitan API key).
        [NotMapped]
        public string? LinkGoogleMaps => PuntoUrl is { } p
            ? $"https://www.google.com/maps/search/?api=1&query={p}"
            : null;

        [NotMapped]
        public string? LinkStreetView => PuntoUrl is { } p
            ? $"https://www.google.com/maps/@?api=1&map_action=pano&viewpoint={p}"
            : null;

        [StringLength(1000)]
        [Display(Name = "Observación")]
        public string? Observacion { get; set; }

        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
        [Display(Name = "Fecha de reclamo")]
        public DateTime? FechaReclamo { get; set; }

        [DataType(DataType.Date)]
        [Column(TypeName = "date")]
        [Display(Name = "Fecha de relevo")]
        public DateTime? FechaRelevo { get; set; }

        // Rutas de las fotos separadas por ";" (las administra FotoService).
        [StringLength(1000)]
        public string? Fotos { get; set; }

        [NotMapped]
        public List<string> ListaFotos => SistemaVeredas.Services.FotoService.Separar(Fotos);

        [NotMapped]
        [Display(Name = "Dirección")]
        public string Direccion => string.IsNullOrWhiteSpace(Altura) ? Calle : $"{Calle} {Altura}";

        public Prioridad? Prioridad { get; set; }

        public Estado Estado { get; set; } = Estado.SinDefinir;

        // Cuando la arregla el frentista no se asigna proveedor.
        [Display(Name = "A cargo del frentista")]
        public bool ACargoFrentista { get; set; }

        // Se puede asignar antes de armar el paquete.
        [Display(Name = "Proveedor")]
        public int? ProveedorId { get; set; }
        public Proveedor? Proveedor { get; set; }

        [Display(Name = "Paquete")]
        public int? PaqueteId { get; set; }
        public Paquete? Paquete { get; set; }

        // Medición (SPEC-001). Los totales los calcula el servidor con MedicionService: nunca se bindean.
        // Fórmula de los pozos: "(2*3)+(5*9)". Null = sin medir.
        [StringLength(500, ErrorMessage = "La medición puede tener hasta 500 caracteres.")]
        [Display(Name = "Medición")]
        public string? Medicion { get; set; }

        // Suma de los subtotales de sus roturas.
        [Column(TypeName = "decimal(10,2)")]
        [Display(Name = "Total (m²)")]
        public decimal? TotalM2 { get; set; }

        [Display(Name = "Cordón")]
        public bool TieneCordon { get; set; }

        [StringLength(500, ErrorMessage = "La medición puede tener hasta 500 caracteres.")]
        [Display(Name = "Medición cordón")]
        public string? MedicionCordon { get; set; }

        [Column(TypeName = "decimal(10,3)")]
        [Display(Name = "Total cordón (m³)")]
        public decimal? TotalCordonM3 { get; set; }

        public ICollection<Rotura> Roturas { get; set; } = new List<Rotura>();

        // No se traduce a SQL: en las consultas usar "v.TotalM2 != null || v.TotalCordonM3 != null".
        [NotMapped]
        public bool EstaMedida => TotalM2 != null || TotalCordonM3 != null;
    }
}
