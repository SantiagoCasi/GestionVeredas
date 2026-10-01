using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // Importa funcionalidades necesarias para controladores MVC y respuestas HTTP. 
using Microsoft.EntityFrameworkCore; // Importa funcionalidades para consultas y operaciones con Entity Framework Core. 
using SistemaVeredas.Data; // Importa el espacio de nombres para acceso a la base de datos a través de Entity Framework. 
using SistemaVeredas.Models; // Importa los modelos que representan las tablas y entidades de la base de datos. 
using SistemaVeredas.Models.ViewModels; // Importa modelos especializados para las vistas, ayudando a estructurar la información mostrada. 
using SistemaVeredas.Services; // Importa servicios auxiliares, como el servicio de hashing de contraseñas. 
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

// Define el namespace o espacio de nombres de los controladores de la aplicación 
namespace SistemaVeredas.Controllers
{
    // Define el controlador AccessController que hereda de la clase base Controller de MVC
    [AllowAnonymous] // Login y recuperación de contraseña deben poder usarse sin haber iniciado sesión
    public class AccessController : Controller
    {
        // Instancia privada que guarda el contexto de la base de datos, para acceder y modificar datos
        private readonly AppDbContext _context;

        // Envío del mail de recuperación (la configuración SMTP está fuera del código).
        private readonly IEmailService _email;

        // Constructor del controlador donde se inyecta el contexto de la base de datos
        public AccessController(AppDbContext context, IEmailService email)
        {
            _context = context; // Asigna el contexto recibido a la variable privada para usarla en métodos
            _email = email;
        }

        // Mensajes de la recuperación por mail (SPEC-003, sección 4).
        public const string MensajeSmtpNoDisponible =
            "La recuperación por mail no está disponible en este momento. Pedile a otro usuario que te asigne una contraseña nueva desde Usuarios.";
        public const string MensajeSmtpFallo =
            "No pudimos enviar el mail. Probá de nuevo más tarde o pedile a otro usuario que te asigne una contraseña nueva desde Usuarios.";
        public const string MensajeMailEnviado =
            "Te enviamos un mail con el enlace para elegir una contraseña nueva.";
        public const string MensajeContrasenaCambiada =
            "Listo, cambiaste la contraseña. Ya podés iniciar sesión.";
        public const string MensajeTokenInvalido =
            "El enlace no es válido. Pedí uno nuevo.";

        // StartRecovery genera Guid.NewGuid().ToString("N"): 32 caracteres hexadecimales en minúscula.
        // Cualquier otro valor (incluido "tokenbloqueado" en cualquier variante) se rechaza ANTES de consultar la base.
        private static readonly Regex FormatoToken = new("^[0-9a-f]{32}$", RegexOptions.Compiled);

        public static bool TokenValido([NotNullWhen(true)] string? token) => token != null && FormatoToken.IsMatch(token);

        // /Access redirige al login
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Login));
        }

        //Login

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var email = model.UsEmail.Trim();
            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.UsEmail == email);

            if (usuario == null || !PasswordService.VerifyPassword(model.UsContrasena, usuario.UsContrasena))
            {
                ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
                return View(model);
            }

            if (!usuario.UsActivo)
            {
                ModelState.AddModelError(string.Empty, "El usuario está inactivo.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsId.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.UsNombre} {usuario.UsApellido}"),
                new Claim(ClaimTypes.Email, usuario.UsEmail)
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = model.Recordarme });

            usuario.UltimoAcceso = DateTime.Now;
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        [HttpGet] // Indica que este método responde a solicitudes GET (mostrar formulario)
        public ActionResult StartRecovery()
        {
            RecoveryViewModel model = new RecoveryViewModel(); // Crea un modelo vacío para el formulario de recuperación
            return View(model); // Retorna la vista con el modelo para que se muestre el formulario de recuperación
        }

        [HttpPost] // Método que recibe datos del formulario (POST) para iniciar recuperación
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartRecovery(RecoveryViewModel model)
        {
            if (!ModelState.IsValid) // Valida la información recibida del formulario
            {
                return View(model); // Si hay errores en el modelo, regresa la vista con errores
            }

            // Busca en la base de datos un usuario con el correo electrónico ingresado
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.UsEmail == model.UsEmail);

            if (usuario == null) // Si no encontró usuario con ese correo
            {
                ViewBag.Error = "No se encontró una cuenta asociada a ese correo electrónico."; // Mensaje de error 
                return View(model); // Retorna la vista para que el usuario intente otra vez
            }

            // Genera un token único para la recuperación de contraseña y lo guarda
            var token = Guid.NewGuid().ToString("N");
            usuario.token_recovery = token;
            await _context.SaveChangesAsync();

            // El enlace toma el dominio de la solicitud: sirve igual en localhost y en el hosting.
            var enlace = Url.Action("Recovery", "Access", new { token }, Request.Scheme) ?? string.Empty;

            var resultado = await _email.EnviarRecuperacionAsync(usuario.UsEmail, enlace);
            if (resultado == ResultadoEnvio.Enviado)
            {
                TempData["MensajeExito"] = MensajeMailEnviado;
                return RedirectToAction("Login", "Access"); // Redirige a la vista de login
            }

            // No se pudo mandar: el token vuelve a quedar bloqueado para que el enlace no sirva.
            usuario.token_recovery = Usuario.TokenBloqueado;
            await _context.SaveChangesAsync();

            ViewBag.Error = resultado == ResultadoEnvio.NoConfigurado ? MensajeSmtpNoDisponible : MensajeSmtpFallo;
            return View(model);
        }

        [HttpGet] // Solicitud GET para acceder a la vista de recuperación con un token
        public async Task<IActionResult> Recovery(string token)
        {
            if (!TokenValido(token)) // El token tiene que tener el formato que genera StartRecovery
            {
                TempData["Error"] = MensajeTokenInvalido; // Mensaje de error
                return RedirectToAction("StartRecovery", "Access"); // Redirige a inicio de recuperación
            }

            // Busca en la base de datos un usuario que tenga el token proporcionado
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.token_recovery == token);

            if (usuario == null) // Si no se encontró el usuario o el token es inválido
            {
                TempData["Error"] = "El enlace de recuperación es inválido o ha expirado."; // Mensaje de error 
                return RedirectToAction("StartRecovery", "Access");
            }

            var model = new RecoveryPasswordViewModel
            {
                token = token
            }; // Crea modelo con el token para la vista 
            return View(model); // Muestra la vista para ingresar nueva contraseña
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Recovery(RecoveryPasswordViewModel model)
        {
            // Primero el formato del token, antes de validar el modelo y de consultar la base (SEG-01).
            if (!TokenValido(model.token))
            {
                TempData["Error"] = MensajeTokenInvalido;
                return RedirectToAction("StartRecovery", "Access");
            }

            // El largo (8 a 50) y que coincidan las dos contraseñas lo validan los atributos del modelo.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Busca al usuario con el token que se usó para la recuperación
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.token_recovery == model.token);

            if (usuario == null || string.IsNullOrEmpty(model.token)) // Si no existe el token o el usuario 
            {
                TempData["Error"] = MensajeTokenInvalido;
                return RedirectToAction("StartRecovery", "Access");
            }

            // Actualiza la contraseña del usuario con la nueva contraseña hasheada
            usuario.UsContrasena = PasswordService.HashPassword(model.UsContrasena!);
            usuario.token_recovery = Usuario.TokenBloqueado; // Marca el token como usado para que no se reutilice


            _context.Entry(usuario).State = EntityState.Modified; // Marca entidad modificada
            await _context.SaveChangesAsync(); // Guarda cambios en la DB

            TempData["MensajeExito"] = MensajeContrasenaCambiada;
            return RedirectToAction("Login", "Access"); // Redirige a login
        }

        // No hay registro público: los usuarios se crean desde UsuariosController,
        // que exige haber iniciado sesión.
    }
}