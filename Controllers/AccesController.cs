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
using System.Net.Mail; // Importa clases para enviar correos electrónicos.

// Define el namespace o espacio de nombres de los controladores de la aplicación 
namespace SistemaVeredas.Controllers
{
    // Define el controlador AccessController que hereda de la clase base Controller de MVC
    [AllowAnonymous] // Login y recuperación de contraseña deben poder usarse sin haber iniciado sesión
    public class AccessController : Controller
    {
        // Instancia privada que guarda el contexto de la base de datos, para acceder y modificar datos
        private readonly AppDbContext _context;

        // Constructor del controlador donde se inyecta el contexto de la base de datos
        public AccessController(AppDbContext context)
        {
            _context = context; // Asigna el contexto recibido a la variable privada para usarla en métodos
        }

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

            // Genera un token único para la recuperación de contraseña 
            var token = Guid.NewGuid().ToString("N");

            // Asigna el token de recuperación al usuario 
            usuario.token_recovery = token;
            _context.Entry(usuario).State = EntityState.Modified; // Marca la entidad como modificada
            await _context.SaveChangesAsync(); // Guarda los cambios en la base de datos

            // Envía el correo con el token para recuperación 
            Sendemail(usuario.UsEmail, token);

            // Mensaje temporal para informar éxito 
            TempData["MensajeExito"] = "El enlace de recuperación se ha enviado a su correo registrado correctamente.";
            return RedirectToAction("Login", "Access"); // Redirige a la vista de login
        }

        [HttpGet] // Solicitud GET para acceder a la vista de recuperación con un token
        public async Task<IActionResult> Recovery(string token)
        {
            if (string.IsNullOrEmpty(token)) // Verifica que el token esté presente
            {
                TempData["Error"] = "Token no válido."; // Mensaje de error
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (model.UsContrasena != model.UsContrasena2)
            {
                ModelState.AddModelError(string.Empty, "Las contraseñas no coinciden.");
                return View(model);
            }

            // Busca al usuario con el token que se usó para la recuperación
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.token_recovery == model.token);

            if (usuario == null) // Si no existe el token o el usuario 
            {
                TempData["Error"] = "Token inválido. Solicite un nuevo enlace de recuperación.";
                return RedirectToAction("StartRecovery", "Access");
            }

            // Actualiza la contraseña del usuario con la nueva contraseña hasheada
            usuario.UsContrasena = PasswordService.HashPassword(model.UsContrasena!);
            usuario.token_recovery = "tokenbloqueado"; // Marca el token como usado para que no se reutilice


            _context.Entry(usuario).State = EntityState.Modified; // Marca entidad modificada
            await _context.SaveChangesAsync(); // Guarda cambios en la DB

            TempData["MensajeExito"] = "Contraseña modificada con éxito. Ya puede iniciar sesión.";
            return RedirectToAction("Login", "Access"); // Redirige a login
        }

        // Método privado para enviar un correo de restablecimiento de contraseña
        private void Sendemail(string EmailDestino, string token)
        {
            // Dirección base del sitio para construir el link de recuperación
            string urlDomain = "https://localhost:7054/";
            var url = Url.Action("Recovery", "Access", new { token = token }, Request.Scheme);
            // Opcional: encodear para HTML
            var urlEscaped = System.Text.Encodings.Web.HtmlEncoder.Default.Encode(url);

            // Construcción del mensaje con cuerpo HTML 
            var oMailMessage = new MailMessage(
                            "casisantiagopablo@gmail.com",
                            EmailDestino,
                            "Restablecimiento de contraseña – WebTech",
                            $@" 
                                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 650px; margin: 0 auto; padding: 20px;'> 
                                    <h2 style='color: #004d80;'>Solicitud de restablecimiento de contraseña</h2> 
                                    <p>Estimado/a usuario/a:</p> 
                                    <p>Recibimos una solicitud para restablecer la contraseña de su cuenta en el <strong>Plataforma de aprendizaje WebTech.</p> 
                                    <p>Si usted realizó esta solicitud, haga clic en el siguiente enlace para crear una nueva contraseña:</p> 
                                    <div style='text-align: center; margin: 25px 0;'> 
                                        <a href='{urlEscaped}'  
                                           style='display: inline-block; padding: 12px 24px; background-color: #004d80; color: white; text-decoration: none; border-radius: 5px; font-weight: bold;'> 
                                            Restablecer mi contraseña 
                                        </a> 
                                    </div> 
                                    <p>Este enlace es válido por una sola vez y expirará en 30 minutos.</p> 
                                    <p><strong>¿No solicitó este cambio?</strong> Si usted no ha solicitado restablecer su contraseña, por favor ignore este mensaje. Su cuenta permanecerá segura.</p> 
                                    <p>Para cualquier duda o asistencia adicional, no dude en contactar al Departamento de Desarrollo de Software del instituto.</p> 
                                    <hr style='border: 0; border-top: 1px solid #eee; margin: 30px 0;' /> 
                                    <p style='font-size: 0.9em; color: #666;'> 
                                        Escuela Online gratuita por y para la comunidad de informática<br> 
                                        <em>Formando profesionales desde siempre</em> 
                                    </p> 
                                </div>"
                            );

            oMailMessage.IsBodyHtml = true; // Especifica que el cuerpo es HTML

            // Configuración del cliente SMTP para enviar el correo vía Gmail(puerto 587, SSL)
            using var oSmtpClient = new SmtpClient("smtp.gmail.com")
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Port = 587,
                Credentials = new System.Net.NetworkCredential("casisantiagopablo@gmail.com", "lzohvefhtehybtbb")
            };

            oSmtpClient.Send(oMailMessage); // Enviar el correo 
        }

        // No hay registro público: los usuarios se crean desde UsuariosController,
        // que exige haber iniciado sesión.
    }
}