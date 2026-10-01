# SPEC-003 — Largo de la contraseña y credenciales fuera del código

> Borrador v0.1 · 30/09/2026 · Autor: agente `desarrollador` · Sprint 1 · Historias HU-13 a HU-15

## 1. Objetivo y requisitos que cubre

1. Que la contraseña que escribe el usuario tenga entre 8 y 50 caracteres en el alta, la edición y la recuperación.
2. Sacar del código la cuenta y la contraseña de aplicación de Gmail que usa la recuperación por mail, y la URL fija.
3. Que el inicio de sesión y el mail de recuperación digan «SistemaVeredas» en lugar de «ISFDyT N°124» y «WebTech».

| ID | Qué se cubre acá | Estado en Docs/03 y 05 |
|---|---|---|
| RN-16 | Contraseña de 8 a 50 caracteres. | Confirmado (máx. 50, D-21) · Vigente (mín. 8) |
| RNF-06 | Credenciales SMTP en User Secrets (desarrollo) y `appsettings.Production.json` (producción). | Confirmado (D-22) |
| RNF-05 | **Solo se menciona:** el hash con sal (`PasswordHasher`) no se implementa acá; la columna de 100 caracteres ya lo permite. | A confirmar |
| P-19 | Textos «ISFDyT N°124» y «WebTech» → «SistemaVeredas». | Abierta (propuesta: sí, en el sprint 1) |
| R-01 | Riesgo de la contraseña de Gmail publicada. Esta spec la saca del código; **no** la revoca. | Riesgo abierto |

Relacionados: RF-ACC-01, RF-ACC-04, RF-ACC-06 (la recuperación sigue sin andar en el hosting, D-09).

## 2. Modelo de datos y migración

**No hay migración.**

- `Usuario.UsContrasena` queda `nvarchar(100) NOT NULL`. Guarda el hash, no la contraseña: SHA-256 en Base64 mide siempre **44 caracteres**, sea cual sea el largo de la contraseña (8 o 50). Si más adelante se aprueba RNF-05, el formato de `PasswordHasher` (≈ 84 caracteres) también entra en 100.
- El largo de 8 a 50 se controla **solo en los ViewModels**, nunca en la entidad `Usuario`: `[StringLength(100)]` de la entidad se refiere al hash y no se cambia.

## 3. Cambios por archivo

### 3.1 Contraseñas (RN-16)

Regla común, con el mismo mensaje en los tres ViewModels:

```csharp
[StringLength(50, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 50 caracteres.")]
```

| Archivo | Propiedad | Cambio |
|---|---|---|
| `Models/ViewModels/UsuarioCreateViewModel.cs` | `UsContrasena` | `StringLength(100, MinimumLength = 8, …)` → la regla común. `Required`: "Escribí la contraseña." |
| | `ConfirmarContrasena` | `Required`: "Repetí la contraseña." · `Compare`: "Las contraseñas no coinciden." |
| `Models/ViewModels/UsuarioEditViewModel.cs` | `NuevaContrasena` | Sigue opcional (vacía = no se cambia). `StringLength(100, …)` → la regla común. |
| | `ConfirmarContrasena` | `Compare`: "Las contraseñas no coinciden." |
| `Models/ViewModels/RecoveryPasswordViewModel.cs` | `UsContrasena` | `[Required(ErrorMessage = "Escribí la nueva contraseña.")]` + la regla común + `[DataType(DataType.Password)]` + `[Display(Name = "Nueva contraseña")]`. |
| | `UsContrasena2` | `[Required(ErrorMessage = "Repetí la nueva contraseña.")]` + `[Compare(nameof(UsContrasena), ErrorMessage = "Las contraseñas no coinciden.")]` + `[Display(Name = "Repetí la contraseña")]`. |

- `Controllers/AccesController.cs`, `Recovery` (POST): el `if (model.UsContrasena != model.UsContrasena2)` manual se puede quitar, porque lo cubre `Compare`. Si se deja, usa el mismo mensaje.
- `LoginViewModel` **no** recibe la regla de largo: el login tiene que seguir aceptando las contraseñas que ya existen. Tampoco se recorta (`Trim`) la contraseña en ningún lado: los espacios cuentan.
- `Controllers/UsuariosController.cs`: sin cambios de lógica (ya hashea con `PasswordService`).
- **Vistas:** no se agrega `maxlength="50"` a los `<input>`. Si se agregara, el navegador cortaría en silencio lo que se pegue y el usuario no vería el aviso; se prefiere el mensaje de validación (cliente con jQuery Validation, que genera `data-val-length-*`, y servidor).
  - `Views/Access/Recovery.cshtml`: el recuadro "Requisitos de la contraseña" hoy anuncia reglas que no se controlan (mayúscula, número, carácter especial). Se reemplaza la lista por un solo ítem: **Entre 8 y 50 caracteres.** Se agrega `<partial name="_ValidationScriptsPartial" />` o los scripts de validación equivalentes (la vista tiene `Layout = null`) para que el aviso aparezca antes de enviar.
  - `Views/Usuarios/Create.cshtml` y `Edit.cshtml`: debajo del campo de contraseña, texto de ayuda `form-text`: "Entre 8 y 50 caracteres." En Edit: "Dejala vacía para no cambiarla. Entre 8 y 50 caracteres."

### 3.2 Credenciales SMTP fuera del código (RNF-06)

**Nuevo `Services/SmtpOptions.cs`:**

```csharp
public class SmtpOptions
{
    public const string Seccion = "Smtp";
    public string? Host { get; set; }
    public int Puerto { get; set; } = 587;
    public string? Usuario { get; set; }
    public string? Contrasena { get; set; }
    public string? Remitente { get; set; }   // dirección "De:"; si está vacía se usa Usuario

    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(Host) && Puerto > 0 &&
        !string.IsNullOrWhiteSpace(Usuario) && !string.IsNullOrWhiteSpace(Contrasena);
}
```

**Nuevo `Services/EmailService.cs`** (con interfaz, para poder reemplazarlo por uno falso en las pruebas de SPEC-004):

```csharp
public enum ResultadoEnvio { Enviado, NoConfigurado, Fallo }

public interface IEmailService
{
    Task<ResultadoEnvio> EnviarRecuperacionAsync(string destino, string enlace);
}

public class EmailService : IEmailService
{
    // Recibe IOptions<SmtpOptions> y ILogger<EmailService>.
    // Si !EstaConfigurado → LogWarning("SMTP sin configurar: falta la sección Smtp.") y devuelve NoConfigurado, sin intentar conectar.
    // Arma el MailMessage (asunto y cuerpo de la sección 3.3), usa SmtpClient(Host, Puerto) con EnableSsl = true
    // y NetworkCredential(Usuario, Contrasena), y SendMailAsync.
    // Atrapa SmtpException, InvalidOperationException y SocketException/IOException: LogError(ex, "No se pudo enviar el mail de recuperación a {Destino}.")
    // y devuelve Fallo. Nunca escribe en el log el Usuario ni la Contrasena.
}
```

**`Program.cs`:**

```csharp
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.Seccion));
builder.Services.AddScoped<IEmailService, EmailService>();
```

**`Controllers/AccesController.cs`:**

- El constructor recibe `IEmailService`.
- Se **elimina** el método `Sendemail` completo: la cuenta de Gmail escrita, la `NetworkCredential` con la contraseña y la variable `urlDomain = "https://localhost:7054/"` (que no se usaba: el enlace ya se arma con `Url.Action(..., Request.Scheme)`, que toma el dominio de la solicitud, así que funciona igual en `localhost:7243` y en el hosting).
- `StartRecovery` (POST), nuevo orden:
  1. `[ValidateAntiForgeryToken]` (hoy falta; el `<form>` con tag helper ya envía el token, así que no cambia nada visible — RNF-07).
  2. Validación del modelo y búsqueda del usuario como hoy.
  3. Genera el token, lo guarda y arma el enlace con `Url.Action("Recovery", "Access", new { token }, Request.Scheme)`.
  4. `var resultado = await _email.EnviarRecuperacionAsync(usuario.UsEmail, enlace);`
  5. `Enviado` → `TempData["MensajeExito"]` y redirige a Login (como hoy).
  6. `NoConfigurado` o `Fallo` → vuelve a poner `usuario.token_recovery = "tokenbloqueado"`, guarda, y muestra la vista con `ViewBag.Error` (mensajes en la sección 4). **No lanza excepción ni muestra la página de error.**

**Configuración** (no se escribe ningún valor secreto en archivos versionados):

| Dónde | Qué va | Versionado |
|---|---|---|
| `appsettings.json` | `"Smtp": { "Host": "smtp.gmail.com", "Puerto": 587 }` (no son secretos) | Sí |
| User Secrets (PC de Santiago) | `Smtp:Usuario`, `Smtp:Contrasena`, `Smtp:Remitente` | No (fuera del repo, `UserSecretsId` ya está en el csproj) |
| `appsettings.Production.json` (hosting) | Sección `Smtp` completa: `Usuario`, `Contrasena`, `Remitente` (y `Host`/`Puerto` si cambian) | No (ignorado por `.gitignore`; se publica igual porque está en la carpeta del proyecto) |

Comandos para Santiago (desde la carpeta del proyecto; cada `<…>` lo completa él, nunca en un archivo del repo):

```
dotnet user-secrets set "Smtp:Usuario" "<cuenta de Gmail>"
dotnet user-secrets set "Smtp:Contrasena" "<contraseña de aplicación NUEVA>"
dotnet user-secrets set "Smtp:Remitente" "<cuenta de Gmail>"
```

(En Visual Studio: clic derecho en el proyecto → *Administrar secretos de usuario*.)

En `appsettings.Production.json` se agrega el bloque con los mismos nombres de clave, completado a mano:

```json
"Smtp": {
  "Usuario": "<completar>",
  "Contrasena": "<completar>",
  "Remitente": "<completar>"
}
```

### 3.3 Textos «SistemaVeredas» (P-19)

Solo si el PM confirma la propuesta de P-19. Cambios:

| Archivo | Hoy | Queda |
|---|---|---|
| `Views/Access/Login.cshtml` | `<title>@ViewBag.Title - ISFDyT N°124</title>` | `<title>@ViewBag.Title - SistemaVeredas</title>` |
| `Views/Access/StartRecovery.cshtml` | `<title>… - ISFDyT N°124</title>` y `alt="Logo ISFDyT N°124"` (en un bloque comentado) | `SistemaVeredas`; el bloque comentado del logo se borra |
| `Views/Access/Recovery.cshtml` | `<title>… - WebTech</title>` y `<img src="~/css/image/logo.png" alt="Logo ISFDyT N°124">` | `SistemaVeredas`; el `<img>` se quita (el archivo `wwwroot/css/image/logo.png` no existe en el proyecto: hoy es una imagen rota) |
| `EmailService` (antes en `Sendemail`) | Asunto "Restablecimiento de contraseña – WebTech"; cuerpo de la "Plataforma de aprendizaje WebTech" y del "instituto" | Asunto y cuerpo de abajo |

Asunto: **Restablecé tu contraseña de SistemaVeredas**

Cuerpo (HTML simple, mismo estilo en línea que hoy, voseo):

> **Restablecer la contraseña**
>
> Hola:
>
> Recibimos un pedido para restablecer la contraseña de tu usuario en **SistemaVeredas**.
>
> Si lo pediste vos, hacé clic en el botón para elegir una contraseña nueva: **[Elegir contraseña nueva]** (enlace)
>
> El enlace sirve una sola vez.
>
> Si no lo pediste, ignorá este mail: tu contraseña no cambia.
>
> — SistemaVeredas

Se quita la frase "expirará en 30 minutos" porque el sistema no controla vencimiento (ver Q-03). El enlace se codifica con `HtmlEncoder` como hoy.

## 4. Validaciones y mensajes exactos

| Situación | Dónde | Mensaje |
|---|---|---|
| Contraseña de 7 o menos, o de 51 o más | Alta, edición (si se completa), recuperación | La contraseña debe tener entre 8 y 50 caracteres. |
| Contraseña vacía en el alta | Alta | Escribí la contraseña. |
| Confirmación vacía en el alta | Alta | Repetí la contraseña. |
| Nueva contraseña vacía | Recuperación | Escribí la nueva contraseña. |
| Confirmación vacía | Recuperación | Repetí la nueva contraseña. |
| No coinciden | Alta, edición, recuperación | Las contraseñas no coinciden. |
| SMTP sin configurar | StartRecovery (`ViewBag.Error`) | La recuperación por mail no está disponible en este momento. Pedile a otro usuario que te asigne una contraseña nueva desde Usuarios. |
| Falla el envío | StartRecovery (`ViewBag.Error`) | No pudimos enviar el mail. Probá de nuevo más tarde o pedile a otro usuario que te asigne una contraseña nueva desde Usuarios. |
| Mail enviado | Login (`TempData["MensajeExito"]`) | Te enviamos un mail con el enlace para elegir una contraseña nueva. |
| Contraseña cambiada | Login (`TempData["MensajeExito"]`) | Listo, cambiaste la contraseña. Ya podés iniciar sesión. |
| Token inválido (POST) | StartRecovery (`TempData["Error"]`) | El enlace no es válido. Pedí uno nuevo. |

Los mensajes de error de SMTP se muestran con SweetAlert dentro de un string de JavaScript (`text: '@Html.Raw(ViewBag.Error)'`). Por eso no llevan apóstrofos ni comillas. Sugerencia: cambiar a `text: @Json.Serialize(ViewBag.Error)` en las tres vistas de `Access`.

## 5. Casos borde

1. **Contraseña de exactamente 8 o 50 caracteres:** se acepta. **7 o 51:** se rechaza con el mensaje. Aplica en el alta, en la edición y en la recuperación.
2. **Usuario con contraseña de 50 caracteres:** inicia sesión (el hash es de 44 caracteres y entra en la columna de 100).
3. **Contraseña con espacios al principio o al final:** los espacios cuentan y se guardan; al iniciar sesión hay que escribirlos igual.
4. **Caracteres fuera del plano básico (p. ej., emojis):** `StringLength` cuenta unidades UTF-16, así que un emoji cuenta como 2. Se acepta así; no vale la pena controlarlo.
5. **Edición con `NuevaContrasena` vacía:** no se valida el largo y se mantiene la contraseña actual (como hoy).
6. **Usuarios existentes con contraseñas de menos de 8 caracteres** (si los hay): siguen entrando; la regla se aplica solo al escribir una contraseña nueva.
7. **Falta la sección `Smtp` o le falta `Usuario`/`Contrasena`:** la app arranca igual; al pedir la recuperación se ve el mensaje de "no disponible". No se genera un token válido.
8. **SMTP configurado pero Gmail rechaza** (contraseña revocada, hosting que bloquea el puerto 587 — D-09): mensaje de "No pudimos enviar el mail", el error va al log sin credenciales, y el token vuelve a `tokenbloqueado`.
9. **Enlace de recuperación en el hosting:** se arma con el dominio de la solicitud (ya no con `localhost:7054`).

## 6. Riesgo R-01: la contraseña vieja sigue en el historial de git

Sacar la contraseña de aplicación de `AccesController.cs` **no la elimina del repositorio**: sigue en todos los commits anteriores, y el repositorio es público. Cualquiera que lea el historial puede usarla hasta que se **revoque** en la cuenta de Google (Seguridad → Contraseñas de aplicación). Pasos, fuera de esta spec y a cargo de Santiago:

1. Revocar la contraseña de aplicación actual (quedó pendiente el 30/09 por una verificación de Google).
2. Generar una nueva y cargarla **solo** en User Secrets y en `appsettings.Production.json`.
3. Opcional: hacer privado el repositorio o reescribir el historial (`git filter-repo`). Reescribir no sirve si no se revoca antes, porque ya pudo haberse copiado.

Hasta que se revoque, R-01 sigue abierto aunque esta historia esté Hecha. La spec no escribe el valor de ninguna contraseña; el criterio de aceptación de HU-14 se verifica con `git grep` sobre los archivos versionados buscando la cuenta de Gmail y `NetworkCredential(` con un literal: no debe haber resultados.

## 7. Pruebas automatizadas que acompañan (SPEC-004)

- Unitarias de validación de los tres ViewModels con `Validator.TryValidateObject` (7, 8, 50 y 51 caracteres; vacía en edición).
- Integración: alta de usuario con 51 caracteres → 200 con el mensaje y sin fila nueva; con 50 → se crea y ese usuario inicia sesión.
- Integración: `StartRecovery` con la sección `Smtp` vacía → 200 con el mensaje de "no disponible", sin excepción, y `token_recovery` sigue en `tokenbloqueado`. Con un `IEmailService` falso que devuelve `Enviado` → redirige a Login y el token queda guardado.
- `EmailService` nunca se prueba contra Gmail real.

## 8. Fuera de alcance

- **RNF-05** (hash con sal, `PasswordHasher`): queda para otra spec cuando se confirme. Va a requerir migrar los hashes existentes al iniciar sesión.
- RF-ACC-05 (asignar contraseña nueva desde la edición): ya existe en `UsuarioEditViewModel`; no se toca más allá del largo.
- Vencimiento del token de recuperación, y el mensaje que revela si un email existe ("No se encontró una cuenta asociada a ese correo electrónico.").
- Pasar a voseo el resto de los textos de las vistas de `Access` (p. ej., "Ingrese su email registrado"), salvo los mensajes de la sección 4.
- Otras claves que hoy están en `appsettings.Production.json` (base y Google Maps): ya están fuera de git; no cambian.
- Revocar la contraseña y limpiar el historial (sección 6).

## 9. Preguntas abiertas para el PM

- **Q-01 (P-19):** ¿se aprueba cambiar «ISFDyT N°124» y «WebTech» por «SistemaVeredas»? La sección 3.3 depende de eso.
- **Q-02 (RN-16):** ¿se confirma el mínimo de 8 caracteres (hoy *Vigente*)?
- **Q-03:** el mail viejo decía que el enlace vence a los 30 minutos, pero el sistema no lo controla. La spec saca esa frase. ¿Se quiere el vencimiento (otra historia) o alcanza con que sirva una sola vez?
- **Q-04:** los mensajes de "no disponible" mandan a pedirle la contraseña nueva a otro usuario (RF-ACC-05, *A confirmar*). ¿Está bien ese texto?
