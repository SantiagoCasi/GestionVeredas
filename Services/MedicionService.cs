using System.Globalization;
using System.Text.RegularExpressions;
using SistemaVeredas.Models;

namespace SistemaVeredas.Services
{
    // Un término (pozo) de la medición ya calculado.
    public sealed record TerminoMedicion(int Orden, string Medidas, decimal Subtotal);

    // Resultado de calcular una medición: el total, la fórmula normalizada, los términos y los errores.
    public sealed record ResultadoMedicion(
        decimal? Total,
        string? TextoNormalizado,
        IReadOnlyList<TerminoMedicion> Terminos,
        IReadOnlyList<string> Errores)
    {
        public bool EsValido => Errores.Count == 0;
        public bool EstaVacia => EsValido && Total is null;
    }

    // Error para pasar al ModelState: el campo y el mensaje.
    public sealed record ErrorMedicion(string Campo, string Mensaje);

    // Cálculo y validación de la medición de una vereda (SPEC-001). No usa la base ni la cultura del servidor.
    public static class MedicionService
    {
        public const decimal MedidaMaxima = 9999.99m;

        // Veredas.Medicion y Veredas.MedicionCordon son nvarchar(500): se controla la fórmula ya normalizada,
        // que agrega dos paréntesis por término y puede ser más larga que lo que escribió el usuario.
        public const int LargoMaximoFormula = 500;

        // Máximos de las columnas decimal(10,2) y decimal(10,3).
        public const decimal TotalMaximoM2 = 99999999.99m;
        public const decimal TotalMaximoM3 = 9999999.999m;

        private static readonly Regex EspacioEntreCifras = new(@"[0-9.,]\s+[0-9.,]", RegexOptions.Compiled);
        private static readonly Regex Espacios = new(@"\s+", RegexOptions.Compiled);
        private static readonly Regex NumeroConEnteros = new(@"^[0-9]+([.,][0-9]+)?$", RegexOptions.Compiled);
        private static readonly Regex NumeroSoloDecimales = new(@"^[.,][0-9]+$", RegexOptions.Compiled);

        // Formato de los totales con coma decimal y punto de miles, como en es-AR,
        // armado a mano para no depender de la cultura instalada en el servidor.
        private static readonly NumberFormatInfo FormatoArgentino = new()
        {
            NumberDecimalSeparator = ",",
            NumberGroupSeparator = ".",
            NumberGroupSizes = new[] { 3 },
            NegativeSign = "-"
        };

        // ---- Mensajes (iguales en el servidor y en wwwroot/js/medicion-vereda.js) ----

        public static string MensajeTerminoVacio(int n) =>
            $"El término {n} está vacío: revisá que no haya dos «+» seguidos ni un «+» al principio o al final.";

        public static string MensajeEspacio(int n, string t) =>
            $"El término {n} («{t}») tiene un espacio dentro de una medida. Separá las medidas con «*».";

        public static string MensajeCaracter(int n, string t, char c) =>
            $"El término {n} («{t}») tiene un carácter que no se permite: «{c}». Usá solo números, «+», «*» (o «x») y paréntesis.";

        public static string MensajeParentesis(int n, string t) =>
            $"El término {n} («{t}») tiene los paréntesis mal puestos. Cada término puede ir entre paréntesis, así: (2*3).";

        public static string MensajeMedidaVacia(int n, string t) =>
            $"El término {n} («{t}») tiene una medida vacía: revisá que no haya dos «*» seguidos ni un «*» al principio o al final.";

        public static string MensajeNumeroInvalido(int n, string t, string m) =>
            $"El término {n} («{t}») tiene una medida que no es un número válido: «{m}». Usá la coma o el punto solo para los decimales (2,5 o 2.5).";

        public static string MensajeMedidaCero(int n, string t) =>
            $"El término {n} («{t}») tiene una medida en cero. Las medidas tienen que ser mayores a 0.";

        public static string MensajeMedidaGrande(int n, string t) =>
            $"El término {n} («{t}») tiene una medida mayor a 9.999,99 m.";

        public static string MensajeCantidadMedidas(int n, string t, int k, int e) =>
            $"El término {n} («{t}») tiene {k} {(k == 1 ? "medida" : "medidas")} y lleva {e}.";

        public const string MensajeTotalGrande = "El total es demasiado grande. Revisá la medición.";

        public const string MensajeLargoMaximo = "La medición puede tener hasta 500 caracteres.";

        public const string MensajePozosNoCoinciden =
            "La cantidad de pozos no coincide con los tipos de suelo elegidos. Revisá la medición y volvé a elegir los tipos.";

        public static string MensajePozoSinTipo(int n) => $"Elegí el tipo de suelo del pozo {n}.";

        public static string MensajeTipoInexistente(int n) => $"El tipo de suelo del pozo {n} no existe. Elegí otro de la lista.";

        // ---- Cálculo ----

        // medidasPorTermino: 2 (m², 2 decimales) o 3 (m³, 3 decimales).
        public static ResultadoMedicion Calcular(string? terminos, int medidasPorTermino)
        {
            if (medidasPorTermino != 2 && medidasPorTermino != 3)
                throw new ArgumentOutOfRangeException(nameof(medidasPorTermino), "Las medidas por término son 2 (m²) o 3 (m³).");

            if (string.IsNullOrWhiteSpace(terminos))
                return new ResultadoMedicion(null, null, Array.Empty<TerminoMedicion>(), Array.Empty<string>());

            var decimales = medidasPorTermino == 2 ? 2 : 3;
            var errores = new List<string>();
            var calculados = new List<(int Orden, string Medidas, List<decimal> Valores)>();

            var partes = terminos.Split('+');
            for (var i = 0; i < partes.Length; i++)
            {
                var n = i + 1;
                var t = partes[i].Trim();
                var error = RevisarTermino(n, t, medidasPorTermino, out var medidas, out var valores);
                if (error != null)
                    errores.Add(error);
                else
                    calculados.Add((n, medidas!, valores!));
            }

            if (errores.Count > 0)
                return ConErrores(errores);

            var lista = new List<TerminoMedicion>();
            decimal total = 0m;
            try
            {
                foreach (var (orden, medidas, valores) in calculados)
                {
                    var producto = 1m;
                    foreach (var v in valores) producto *= v;
                    var subtotal = Math.Round(producto, decimales, MidpointRounding.AwayFromZero);
                    lista.Add(new TerminoMedicion(orden, medidas, subtotal));
                    total += subtotal; // el total es la suma de los subtotales ya redondeados
                }
            }
            catch (OverflowException)
            {
                return ConErrores(new List<string> { MensajeTotalGrande });
            }

            var maximo = medidasPorTermino == 2 ? TotalMaximoM2 : TotalMaximoM3;
            if (total > maximo)
                return ConErrores(new List<string> { MensajeTotalGrande });

            var normalizado = string.Join("+", lista.Select(x => "(" + x.Medidas + ")"));
            if (normalizado.Length > LargoMaximoFormula)
                return ConErrores(new List<string> { MensajeLargoMaximo });

            return new ResultadoMedicion(total, normalizado, lista, Array.Empty<string>());
        }

        // Devuelve el primer error del término, o null si es válido (y entonces completa medidas y valores).
        private static string? RevisarTermino(int n, string t, int esperadas, out string? medidas, out List<decimal>? valores)
        {
            medidas = null;
            valores = null;

            if (t.Length == 0)
                return MensajeTerminoVacio(n);

            if (EspacioEntreCifras.IsMatch(t))
                return MensajeEspacio(n, t);

            var limpio = Espacios.Replace(t, string.Empty).Replace('x', '*').Replace('X', '*');

            foreach (var c in limpio)
            {
                var permitido = (c >= '0' && c <= '9') || c == ',' || c == '.' || c == '*' || c == '(' || c == ')';
                if (!permitido)
                    return MensajeCaracter(n, t, c);
            }

            if (limpio.Contains('(') || limpio.Contains(')'))
            {
                var envuelve = limpio.Length >= 2 && limpio[0] == '(' && limpio[^1] == ')';
                var interior = envuelve ? limpio.Substring(1, limpio.Length - 2) : string.Empty;
                if (!envuelve || interior.Contains('(') || interior.Contains(')'))
                    return MensajeParentesis(n, t);
                limpio = interior;
            }

            var textos = limpio.Split('*');
            if (textos.Any(m => m.Length == 0))
                return MensajeMedidaVacia(n, t);

            // Mismo orden que el JS: primero el formato de todas las medidas, después cero y máximo.
            foreach (var m in textos)
            {
                if (!NumeroConEnteros.IsMatch(m) && !NumeroSoloDecimales.IsMatch(m))
                    return MensajeNumeroInvalido(n, t, m);
            }

            // Cero: ninguna cifra distinta de 0 (se mira el texto, para no depender del redondeo de decimal).
            if (textos.Any(m => !m.Any(ch => ch >= '1' && ch <= '9')))
                return MensajeMedidaCero(n, t);

            var numeros = new List<decimal>();
            foreach (var m in textos)
            {
                // Siempre con InvariantCulture: con es-AR "4.5" se leería 45.
                // Si no entra en un decimal (un número enorme), es una medida muy grande.
                if (!decimal.TryParse(m.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var valor)
                    || valor > MedidaMaxima)
                    return MensajeMedidaGrande(n, t);
                numeros.Add(valor);
            }

            if (numeros.Count != esperadas)
                return MensajeCantidadMedidas(n, t, numeros.Count, esperadas);

            medidas = limpio;
            valores = numeros;
            return null;
        }

        private static ResultadoMedicion ConErrores(List<string> errores) =>
            new(null, null, Array.Empty<TerminoMedicion>(), errores);

        // Calcula la medición y el cordón, arma vereda.Roturas y valida los tipos por pozo.
        // No consulta la base: la existencia de los tipos la controla el controlador.
        public static IReadOnlyList<ErrorMedicion> AplicarAVereda(Vereda vereda, IReadOnlyList<int?> tiposRotura)
        {
            var errores = new List<ErrorMedicion>();

            // 1) Cordón (m³). Sin la casilla se descarta lo que se haya escrito.
            if (!vereda.TieneCordon)
            {
                vereda.MedicionCordon = null;
                vereda.TotalCordonM3 = null;
            }
            else
            {
                var cordon = Calcular(vereda.MedicionCordon, 3);
                if (cordon.EsValido)
                {
                    vereda.MedicionCordon = cordon.TextoNormalizado;
                    vereda.TotalCordonM3 = cordon.Total;
                }
                else
                {
                    errores.AddRange(cordon.Errores.Select(e => new ErrorMedicion(nameof(Vereda.MedicionCordon), e)));
                }
            }

            // 2) Medición de los pozos (m²).
            var medicion = Calcular(vereda.Medicion, 2);
            if (!medicion.EsValido)
            {
                errores.AddRange(medicion.Errores.Select(e => new ErrorMedicion(nameof(Vereda.Medicion), e)));
                return errores;
            }

            // 3) Sin medir: sin roturas y se ignoran los tipos.
            if (medicion.EstaVacia)
            {
                vereda.Medicion = null;
                vereda.TotalM2 = null;
                vereda.Roturas = new List<Rotura>();
                return errores;
            }

            // 4) Un tipo de suelo por pozo, en el mismo orden.
            var n = medicion.Terminos.Count;
            if (tiposRotura.Count != n)
            {
                errores.Add(new ErrorMedicion(nameof(Vereda.Medicion), MensajePozosNoCoinciden));
                return errores;
            }

            var sinTipo = false;
            for (var i = 0; i < n; i++)
            {
                if (tiposRotura[i] == null)
                {
                    errores.Add(new ErrorMedicion($"tiposRotura[{i}]", MensajePozoSinTipo(i + 1)));
                    sinTipo = true;
                }
            }
            if (sinTipo) return errores;

            vereda.Medicion = medicion.TextoNormalizado;
            vereda.TotalM2 = medicion.Total;
            vereda.Roturas = medicion.Terminos
                .Select((t, i) => new Rotura
                {
                    Id = 0,
                    Orden = t.Orden,
                    Medidas = t.Medidas,
                    TipoSueloId = tiposRotura[i]!.Value,
                    SubtotalM2 = t.Subtotal
                })
                .ToList();

            return errores;
        }

        // "51,25 m²" / "0,225 m³". null → null.
        public static string? FormatearM2(decimal? valor) =>
            valor is decimal v ? v.ToString("N2", FormatoArgentino) + " m²" : null;

        public static string? FormatearM3(decimal? valor) =>
            valor is decimal v ? v.ToString("N3", FormatoArgentino) + " m³" : null;

        // Solo el número con formato argentino (sin unidad), p. ej. para las tarjetas.
        public static string FormatearNumero(decimal valor, int decimales) =>
            valor.ToString("N" + decimales, FormatoArgentino);
    }
}
