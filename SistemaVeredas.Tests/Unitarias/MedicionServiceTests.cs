using System.Globalization;
using SistemaVeredas.Models;
using SistemaVeredas.Services;

namespace SistemaVeredas.Tests.Unitarias
{
    // Cálculo y validación de la medición (SPEC-001 sección 5, SPEC-004 sección 4.8).
    public class MedicionServiceTests
    {
        private static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

        // ---- Mediciones válidas: total y fórmula normalizada ----

        [Theory]
        [InlineData("3*2", 2, "6.00", "(3*2)")]
        [InlineData("(2*3)+(5*9)", 2, "51.00", "(2*3)+(5*9)")]
        [InlineData("2*3+5*9", 2, "51.00", "(2*3)+(5*9)")]
        [InlineData("(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)", 2, "130.35", "(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)")]
        [InlineData("(2x3)+(5X9)+(4.5x8)+(2,4x4)+(3,75x9)", 2, "130.35", "(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)")]
        [InlineData("(2*3)+(5*9)+(0,5*0,5)", 2, "51.25", "(2*3)+(5*9)+(0,5*0,5)")]
        [InlineData("2 x 3 + 5 * 9", 2, "51.00", "(2*3)+(5*9)")]
        [InlineData("(5*0,15*0,30)", 3, "0.225", "(5*0,15*0,30)")]
        [InlineData("4.5*2", 2, "9.00", "(4.5*2)")]
        [InlineData("0,005*1", 2, "0.01", "(0,005*1)")]
        [InlineData("0,005*1+0,005*1", 2, "0.02", "(0,005*1)+(0,005*1)")]
        [InlineData("2,4x3", 2, "7.20", "(2,4*3)")]
        [InlineData("2.4*3", 2, "7.20", "(2.4*3)")]
        [InlineData("1*0,5*0,3", 3, "0.150", "(1*0,5*0,3)")]
        [InlineData(" 2 * 3 ", 2, "6.00", "(2*3)")]
        public void Calcular_Valida(string texto, int medidas, string total, string normalizado)
        {
            var r = MedicionService.Calcular(texto, medidas);

            Assert.True(r.EsValido, string.Join(" | ", r.Errores));
            Assert.Equal(D(total), r.Total);
            Assert.Equal(normalizado, r.TextoNormalizado);
        }

        [Fact]
        public void Calcular_UnSubtotalPorPozo()
        {
            var r = MedicionService.Calcular("(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)", 2);

            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, r.Terminos.Select(t => t.Orden));
            Assert.Equal(new[] { 6m, 45m, 36m, 9.6m, 33.75m }, r.Terminos.Select(t => t.Subtotal));
            Assert.Equal("4.5*8", r.Terminos[2].Medidas); // separador decimal como lo escribió el usuario
            Assert.Equal("2,4*4", r.Terminos[3].Medidas);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Calcular_Vacia_NoEsError(string? texto)
        {
            var r = MedicionService.Calcular(texto, 2);

            Assert.True(r.EsValido);
            Assert.True(r.EstaVacia);
            Assert.Null(r.Total);
            Assert.Null(r.TextoNormalizado);
            Assert.Empty(r.Terminos);
        }

        // ---- Errores: mensaje exacto (sección 4.1) ----

        [Theory]
        [InlineData("(2*3)+59", 2, "El término 2 («59») tiene 1 medida y lleva 2.")]
        [InlineData("(5*0,15)", 3, "El término 1 («(5*0,15)») tiene 2 medidas y lleva 3.")]
        [InlineData("2*3*4", 2, "El término 1 («2*3*4») tiene 3 medidas y lleva 2.")]
        [InlineData("(2*3)+(4*5*6)", 3, "El término 1 («(2*3)») tiene 2 medidas y lleva 3.")]
        [InlineData("2-3", 2, "El término 1 («2-3») tiene un carácter que no se permite: «-». Usá solo números, «+», «*» (o «x») y paréntesis.")]
        [InlineData("2/3", 2, "El término 1 («2/3») tiene un carácter que no se permite: «/». Usá solo números, «+», «*» (o «x») y paréntesis.")]
        [InlineData("2*a", 2, "El término 1 («2*a») tiene un carácter que no se permite: «a». Usá solo números, «+», «*» (o «x») y paréntesis.")]
        [InlineData("abc", 2, "El término 1 («abc») tiene un carácter que no se permite: «a». Usá solo números, «+», «*» (o «x») y paréntesis.")]
        [InlineData("2*3++4*5", 2, "El término 2 está vacío: revisá que no haya dos «+» seguidos ni un «+» al principio o al final.")]
        [InlineData("+2*3", 2, "El término 1 está vacío: revisá que no haya dos «+» seguidos ni un «+» al principio o al final.")]
        [InlineData("(2*3)+", 2, "El término 2 está vacío: revisá que no haya dos «+» seguidos ni un «+» al principio o al final.")]
        [InlineData("2*3+", 2, "El término 2 está vacío: revisá que no haya dos «+» seguidos ni un «+» al principio o al final.")]
        [InlineData("(2*3", 2, "El término 1 («(2*3») tiene los paréntesis mal puestos. Cada término puede ir entre paréntesis, así: (2*3).")]
        [InlineData("2*(3)", 2, "El término 1 («2*(3)») tiene los paréntesis mal puestos. Cada término puede ir entre paréntesis, así: (2*3).")]
        [InlineData("((2*3))", 2, "El término 1 («((2*3))») tiene los paréntesis mal puestos. Cada término puede ir entre paréntesis, así: (2*3).")]
        [InlineData("2**3", 2, "El término 1 («2**3») tiene una medida vacía: revisá que no haya dos «*» seguidos ni un «*» al principio o al final.")]
        [InlineData("1.234,5*2", 2, "El término 1 («1.234,5*2») tiene una medida que no es un número válido: «1.234,5». Usá la coma o el punto solo para los decimales (2,5 o 2.5).")]
        [InlineData("2,*3", 2, "El término 1 («2,*3») tiene una medida que no es un número válido: «2,». Usá la coma o el punto solo para los decimales (2,5 o 2.5).")]
        [InlineData("2.5.3*1", 2, "El término 1 («2.5.3*1») tiene una medida que no es un número válido: «2.5.3». Usá la coma o el punto solo para los decimales (2,5 o 2.5).")]
        [InlineData("0*5", 2, "El término 1 («0*5») tiene una medida en cero. Las medidas tienen que ser mayores a 0.")]
        [InlineData("10000*1", 2, "El término 1 («10000*1») tiene una medida mayor a 9.999,99 m.")]
        [InlineData("4 5*2", 2, "El término 1 («4 5*2») tiene un espacio dentro de una medida. Separá las medidas con «*».")]
        public void Calcular_Error(string texto, int medidas, string mensaje)
        {
            var r = MedicionService.Calcular(texto, medidas);

            Assert.False(r.EsValido);
            Assert.Null(r.Total);
            Assert.Null(r.TextoNormalizado);
            Assert.Empty(r.Terminos);
            Assert.Contains(mensaje, r.Errores);
        }

        [Fact]
        public void Calcular_ParentesisQueCruzanTerminos_FallanLosDos()
        {
            var r = MedicionService.Calcular("(2+3)*4", 2);

            Assert.Equal(2, r.Errores.Count);
            Assert.StartsWith("El término 1 («(2»)", r.Errores[0]);
            Assert.StartsWith("El término 2 («3)*4»)", r.Errores[1]);
        }

        [Fact]
        public void Calcular_InformaTodosLosTerminosConError()
        {
            var r = MedicionService.Calcular("59+2*3+7", 2);

            Assert.Equal(new[]
            {
                "El término 1 («59») tiene 1 medida y lleva 2.",
                "El término 3 («7») tiene 1 medida y lleva 2."
            }, r.Errores);
        }

        // O-04: con números límite el servidor da el mismo resultado que el JS.
        [Fact]
        public void Calcular_MedidaMuyChica_NoEsCero()
        {
            var r = MedicionService.Calcular("0,0000000000000000000000000000001*5", 2);

            Assert.True(r.EsValido, string.Join(" | ", r.Errores));
            Assert.Equal(0m, r.Total);
        }

        [Fact]
        public void Calcular_PrimeroElFormatoDeTodasLasMedidas()
        {
            var r = MedicionService.Calcular("999999999999999999999999999999*2,", 2);

            Assert.Equal(new[] { MedicionService.MensajeNumeroInvalido(1, "999999999999999999999999999999*2,", "2,") }, r.Errores);
        }

        [Fact]
        public void Calcular_CeroAntesQueMuyGrande()
        {
            var r = MedicionService.Calcular("0*99999999999999999999999999999", 2);

            Assert.Equal(new[] { MedicionService.MensajeMedidaCero(1, "0*99999999999999999999999999999") }, r.Errores);
        }

        [Fact]
        public void Calcular_NumeroEnorme_EsMedidaMuyGrande()
        {
            var r = MedicionService.Calcular("99999999999999999999999999999999*1", 2);

            Assert.False(r.EsValido);
            Assert.Contains("mayor a 9.999,99 m", r.Errores[0]);
        }

        // O-01: la fórmula normalizada (con paréntesis) tiene que entrar en nvarchar(500).
        [Theory]
        [InlineData("1*1", 2, 83, 497)]
        [InlineData("1*1*1", 3, 62, 495)]
        public void Calcular_FormulaNormalizadaHasta500_EsValida(string termino, int medidas, int terminos, int largo)
        {
            var r = MedicionService.Calcular(string.Join("+", Enumerable.Repeat(termino, terminos)), medidas);

            Assert.True(r.EsValido, string.Join(" | ", r.Errores));
            Assert.Equal(largo, r.TextoNormalizado!.Length);
        }

        [Theory]
        [InlineData("1*1", 2, 84)]      // 503 caracteres normalizados (la entrada mide 335)
        [InlineData("1*1*1", 3, 63)]    // 503 caracteres normalizados
        public void Calcular_FormulaNormalizadaDeMasDe500_Error(string termino, int medidas, int terminos)
        {
            var r = MedicionService.Calcular(string.Join("+", Enumerable.Repeat(termino, terminos)), medidas);

            Assert.False(r.EsValido);
            Assert.Null(r.TextoNormalizado);
            Assert.Equal(new[] { "La medición puede tener hasta 500 caracteres." }, r.Errores);
        }

        [Fact]
        public void Aplicar_CordonDeMasDe500_ErrorEnMedicionCordon()
        {
            var v = new Vereda { TieneCordon = true, MedicionCordon = string.Join("+", Enumerable.Repeat("1*1*1", 63)) };

            var errores = MedicionService.AplicarAVereda(v, Array.Empty<int?>());

            var e = Assert.Single(errores);
            Assert.Equal("MedicionCordon", e.Campo);
            Assert.Equal(MedicionService.MensajeLargoMaximo, e.Mensaje);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(4)]
        public void Calcular_MedidasPorTerminoInvalidas_Lanza(int medidas)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => MedicionService.Calcular("2*3", medidas));
        }

        // ---- AplicarAVereda (sección 5.2) ----

        [Fact]
        public void Aplicar_ArmaUnaRoturaPorPozo_ConTipoRepetido()
        {
            var v = new Vereda { Medicion = "(2*3)+(5*9)+(0,5*0,5)" };

            var errores = MedicionService.AplicarAVereda(v, new int?[] { 1, 2, 1 });

            Assert.Empty(errores);
            Assert.Equal("(2*3)+(5*9)+(0,5*0,5)", v.Medicion);
            Assert.Equal(51.25m, v.TotalM2);
            Assert.Equal(new[] { 1, 2, 3 }, v.Roturas.Select(r => r.Orden));
            Assert.Equal(new[] { 6m, 45m, 0.25m }, v.Roturas.Select(r => r.SubtotalM2));
            Assert.Equal(new[] { 1, 2, 1 }, v.Roturas.Select(r => r.TipoSueloId));
            Assert.All(v.Roturas, r => Assert.Equal(0, r.Id));
        }

        [Fact]
        public void Aplicar_PozoSinTipo_Error()
        {
            var v = new Vereda { Medicion = "(2*3)+(5*9)+(0,5*0,5)" };

            var errores = MedicionService.AplicarAVereda(v, new int?[] { 1, null, 1 });

            var e = Assert.Single(errores);
            Assert.Equal("tiposRotura[1]", e.Campo);
            Assert.Equal("Elegí el tipo de suelo del pozo 2.", e.Mensaje);
            Assert.Empty(v.Roturas);
        }

        [Fact]
        public void Aplicar_CantidadDeTiposDistinta_Error()
        {
            var v = new Vereda { Medicion = "(2*3)+(5*9)+(0,5*0,5)" };

            var errores = MedicionService.AplicarAVereda(v, new int?[] { 1, 2 });

            var e = Assert.Single(errores);
            Assert.Equal("Medicion", e.Campo);
            Assert.Equal(MedicionService.MensajePozosNoCoinciden, e.Mensaje);
        }

        [Fact]
        public void Aplicar_SinMedicion_IgnoraLosTipos()
        {
            var v = new Vereda { Medicion = "  " };

            var errores = MedicionService.AplicarAVereda(v, new int?[] { 1, 2 });

            Assert.Empty(errores);
            Assert.Null(v.Medicion);
            Assert.Null(v.TotalM2);
            Assert.Empty(v.Roturas);
            Assert.False(v.EstaMedida);
        }

        [Fact]
        public void Aplicar_FormulaConErrores_NoRevisaLosTipos()
        {
            var v = new Vereda { Medicion = "(2*3)+59" };

            var errores = MedicionService.AplicarAVereda(v, new int?[] { null });

            var e = Assert.Single(errores);
            Assert.Equal("Medicion", e.Campo);
            Assert.Equal("El término 2 («59») tiene 1 medida y lleva 2.", e.Mensaje);
        }

        [Fact]
        public void Aplicar_ElTipoNoCambiaElCalculo()
        {
            var a = new Vereda { Medicion = "(2*3)+(5*9)" };
            var b = new Vereda { Medicion = "(2*3)+(5*9)" };

            MedicionService.AplicarAVereda(a, new int?[] { 1, 1 });
            MedicionService.AplicarAVereda(b, new int?[] { 7, 9 });

            Assert.Equal(a.TotalM2, b.TotalM2);
            Assert.Equal(a.Roturas.Select(r => r.SubtotalM2), b.Roturas.Select(r => r.SubtotalM2));
        }

        [Fact]
        public void Aplicar_CordonSinCasilla_SeDescarta()
        {
            var v = new Vereda { TieneCordon = false, MedicionCordon = "5*0,15*0,30", TotalCordonM3 = 99m };

            var errores = MedicionService.AplicarAVereda(v, Array.Empty<int?>());

            Assert.Empty(errores);
            Assert.Null(v.MedicionCordon);
            Assert.Null(v.TotalCordonM3);
        }

        [Fact]
        public void Aplicar_MedicionYCordon()
        {
            var v = new Vereda { Medicion = "(2*3)+(5*9)", TieneCordon = true, MedicionCordon = "5*0,15*0,30" };

            var errores = MedicionService.AplicarAVereda(v, new int?[] { 1, 2 });

            Assert.Empty(errores);
            Assert.Equal(51.00m, v.TotalM2);
            Assert.Equal(0.225m, v.TotalCordonM3);
            Assert.Equal("(5*0,15*0,30)", v.MedicionCordon);
            Assert.True(v.EstaMedida);
        }

        [Fact]
        public void Aplicar_SoloCordon_EstaMedida()
        {
            var v = new Vereda { TieneCordon = true, MedicionCordon = "1*0,5*0,3" };

            var errores = MedicionService.AplicarAVereda(v, Array.Empty<int?>());

            Assert.Empty(errores);
            Assert.Null(v.TotalM2);
            Assert.Equal(0.150m, v.TotalCordonM3);
            Assert.True(v.EstaMedida);
        }

        [Fact]
        public void Aplicar_CordonMarcadoSinMedicion_EsValido()
        {
            var v = new Vereda { TieneCordon = true, MedicionCordon = null };

            var errores = MedicionService.AplicarAVereda(v, Array.Empty<int?>());

            Assert.Empty(errores);
            Assert.True(v.TieneCordon);
            Assert.Null(v.TotalCordonM3);
        }

        [Fact]
        public void Aplicar_CordonConErrores_ErrorEnMedicionCordon()
        {
            var v = new Vereda { TieneCordon = true, MedicionCordon = "5*0,15" };

            var errores = MedicionService.AplicarAVereda(v, Array.Empty<int?>());

            var e = Assert.Single(errores);
            Assert.Equal("MedicionCordon", e.Campo);
        }

        // ---- Formato es-AR, sin depender de la cultura del servidor ----

        [Fact]
        public void Formatear_ConComaDecimal()
        {
            Assert.Equal("51,25 m²", MedicionService.FormatearM2(51.25m));
            Assert.Equal("1.234,50 m²", MedicionService.FormatearM2(1234.5m));
            Assert.Equal("0,225 m³", MedicionService.FormatearM3(0.225m));
            Assert.Null(MedicionService.FormatearM2(null));
            Assert.Null(MedicionService.FormatearM3(null));
        }
    }
}
