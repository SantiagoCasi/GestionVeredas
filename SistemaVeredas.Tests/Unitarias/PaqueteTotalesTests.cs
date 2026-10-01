using SistemaVeredas.Models.Enums;
using SistemaVeredas.Models.ViewModels;
using SistemaVeredas.Services;

namespace SistemaVeredas.Tests.Unitarias
{
    // Totales y avance del paquete, y mensajes de la asignación (SPEC-002).
    public class PaqueteTotalesTests
    {
        private static VeredaTotales V(Estado estado, decimal? m2 = null, decimal? m3 = null) => new(estado, m2, m3);

        [Fact]
        public void CuatroVeredasUnaFinalizada_25PorCiento()
        {
            var t = PaqueteService.CalcularTotales(new[]
            {
                V(Estado.Finalizado, 10m), V(Estado.EnProceso, 5.5m), V(Estado.NoCorresponde), V(Estado.NoSeEncontro)
            });

            Assert.Equal(4, t.Cantidad);
            Assert.Equal(1, t.Finalizadas);
            Assert.Equal(25, t.PorcentajeAvance);
        }

        [Fact]
        public void SinVeredas_CeroPorCiento()
        {
            var t = PaqueteService.CalcularTotales(Array.Empty<VeredaTotales>());

            Assert.Equal(0, t.Cantidad);
            Assert.Equal(0, t.PorcentajeAvance);
            Assert.Equal(0m, t.TotalM2);
            Assert.Equal(0m, t.TotalM3);
        }

        [Fact]
        public void SinMedir_SumanCero()
        {
            var t = PaqueteService.CalcularTotales(new[]
            {
                V(Estado.SinDefinir, 51m, 0.225m), V(Estado.SinDefinir), V(Estado.SinDefinir, null, 0.225m)
            });

            Assert.Equal(51m, t.TotalM2);
            Assert.Equal(0.450m, t.TotalM3);
            Assert.Equal(1, t.SinMedir);
        }

        [Theory]
        [InlineData(1, 3, 33)]
        [InlineData(2, 3, 67)]
        [InlineData(1, 8, 13)]   // 12,5 → 13 (se aleja de cero)
        [InlineData(3, 3, 100)]
        public void Avance_Redondeado(int finalizadas, int total, int esperado)
        {
            var veredas = Enumerable.Range(0, total)
                .Select(i => V(i < finalizadas ? Estado.Finalizado : Estado.EnProceso))
                .ToList();

            Assert.Equal(esperado, PaqueteService.CalcularTotales(veredas).PorcentajeAvance);
        }

        [Fact]
        public void Mensajes_UnaAgregada()
        {
            var (exito, aviso) = PaqueteService.ArmarMensajes(new ResultadoAsignacion
            {
                PaqueteExiste = true, PaqueteNombre = "Marzo", Agregadas = 1
            });

            Assert.Equal("Se agregó 1 vereda al paquete «Marzo».", exito);
            Assert.Null(aviso);
        }

        [Fact]
        public void Mensajes_TodasSalteadas_SoloAviso()
        {
            var (exito, aviso) = PaqueteService.ArmarMensajes(new ResultadoAsignacion
            {
                PaqueteExiste = true,
                PaqueteNombre = "Marzo",
                Salteadas = new List<(string Direccion, string Paquete)> { ("Mitre 100", "Abril") }
            });

            Assert.Null(exito);
            Assert.Equal("No se agregó Mitre 100 porque ya está en el paquete «Abril».", aviso);
        }

        [Fact]
        public void Mensajes_MasDeDiezSalteadas_SeResumen()
        {
            var salteadas = Enumerable.Range(1, 12).Select(i => ($"Calle {i}", "Otro")).ToList();

            var (_, aviso) = PaqueteService.ArmarMensajes(new ResultadoAsignacion
            {
                PaqueteExiste = true, PaqueteNombre = "Marzo", Agregadas = 3, Salteadas = salteadas,
                YaEstaban = 2, Inexistentes = 1
            });

            Assert.StartsWith("No se agregaron 12 veredas porque ya están en otro paquete: Calle 1 («Otro»)", aviso);
            Assert.Contains(" y 2 más.", aviso);
            Assert.Contains("2 veredas ya estaban en este paquete.", aviso);
            Assert.Contains("Una de las veredas que elegiste ya no existe.", aviso);
        }
    }
}
