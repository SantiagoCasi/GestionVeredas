using System.ComponentModel.DataAnnotations;
using SistemaVeredas.Models.ViewModels;

namespace SistemaVeredas.Tests.Unitarias
{
    // RN-16: la contraseña que escribe el usuario tiene entre 8 y 50 caracteres (SPEC-003).
    public class ContrasenaValidacionTests
    {
        private const string MensajeLargo = "La contraseña debe tener entre 8 y 50 caracteres.";

        private static List<ValidationResult> Validar(object modelo)
        {
            var resultados = new List<ValidationResult>();
            Validator.TryValidateObject(modelo, new ValidationContext(modelo), resultados, validateAllProperties: true);
            return resultados;
        }

        private static bool TieneError(List<ValidationResult> r, string propiedad, string mensaje) =>
            r.Any(x => x.MemberNames.Contains(propiedad) && x.ErrorMessage == mensaje);

        [Theory]
        [InlineData(7, false)]
        [InlineData(8, true)]
        [InlineData(50, true)]
        [InlineData(51, false)]
        public void Alta(int largo, bool valida)
        {
            var clave = new string('a', largo);
            var m = new UsuarioCreateViewModel
            {
                UsNombre = "Ana", UsApellido = "Pérez", UsEmail = "ana@ejemplo.com",
                UsContrasena = clave, ConfirmarContrasena = clave
            };

            var r = Validar(m);

            Assert.Equal(!valida, TieneError(r, nameof(m.UsContrasena), MensajeLargo));
            Assert.Equal(valida, r.Count == 0);
        }

        [Fact]
        public void Alta_VaciaYSinConfirmar()
        {
            var r = Validar(new UsuarioCreateViewModel
            {
                UsNombre = "Ana", UsApellido = "Pérez", UsEmail = "ana@ejemplo.com",
                UsContrasena = "", ConfirmarContrasena = ""
            });

            Assert.True(TieneError(r, nameof(UsuarioCreateViewModel.UsContrasena), "Escribí la contraseña."));
            Assert.True(TieneError(r, nameof(UsuarioCreateViewModel.ConfirmarContrasena), "Repetí la contraseña."));
        }

        [Fact]
        public void Alta_NoCoinciden()
        {
            var r = Validar(new UsuarioCreateViewModel
            {
                UsNombre = "Ana", UsApellido = "Pérez", UsEmail = "ana@ejemplo.com",
                UsContrasena = "12345678", ConfirmarContrasena = "12345679"
            });

            Assert.True(TieneError(r, nameof(UsuarioCreateViewModel.ConfirmarContrasena), "Las contraseñas no coinciden."));
        }

        [Theory]
        [InlineData(7, false)]
        [InlineData(8, true)]
        [InlineData(50, true)]
        [InlineData(51, false)]
        public void Edicion(int largo, bool valida)
        {
            var clave = new string('b', largo);
            var m = new UsuarioEditViewModel
            {
                UsId = 1, UsNombre = "Ana", UsApellido = "Pérez", UsEmail = "ana@ejemplo.com",
                NuevaContrasena = clave, ConfirmarContrasena = clave
            };

            var r = Validar(m);

            Assert.Equal(!valida, TieneError(r, nameof(m.NuevaContrasena), MensajeLargo));
            Assert.Equal(valida, r.Count == 0);
        }

        [Fact]
        public void Edicion_Vacia_NoSeValidaElLargo()
        {
            var r = Validar(new UsuarioEditViewModel
            {
                UsId = 1, UsNombre = "Ana", UsApellido = "Pérez", UsEmail = "ana@ejemplo.com",
                NuevaContrasena = null, ConfirmarContrasena = null
            });

            Assert.Empty(r);
        }

        [Theory]
        [InlineData(7, false)]
        [InlineData(8, true)]
        [InlineData(50, true)]
        [InlineData(51, false)]
        public void Recuperacion(int largo, bool valida)
        {
            var clave = new string('c', largo);
            var m = new RecoveryPasswordViewModel { token = "x", UsContrasena = clave, UsContrasena2 = clave };

            var r = Validar(m);

            Assert.Equal(!valida, TieneError(r, nameof(m.UsContrasena), MensajeLargo));
            Assert.Equal(valida, r.Count == 0);
        }

        [Fact]
        public void Recuperacion_VaciaYNoCoinciden()
        {
            var vacia = Validar(new RecoveryPasswordViewModel { token = "x", UsContrasena = null, UsContrasena2 = null });
            Assert.True(TieneError(vacia, nameof(RecoveryPasswordViewModel.UsContrasena), "Escribí la nueva contraseña."));
            Assert.True(TieneError(vacia, nameof(RecoveryPasswordViewModel.UsContrasena2), "Repetí la nueva contraseña."));

            var distintas = Validar(new RecoveryPasswordViewModel { token = "x", UsContrasena = "12345678", UsContrasena2 = "87654321" });
            Assert.True(TieneError(distintas, nameof(RecoveryPasswordViewModel.UsContrasena2), "Las contraseñas no coinciden."));
        }

        [Fact]
        public void Login_NoTieneReglaDeLargo()
        {
            var r = Validar(new LoginViewModel { UsEmail = "ana@ejemplo.com", UsContrasena = "corta" });

            Assert.Empty(r);
        }
    }
}
