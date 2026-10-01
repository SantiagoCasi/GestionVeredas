# Entrega del programador — Sprint 1

> 30/09/2026 · Agente `programador` · Copia de trabajo `/home/claude/work/SistemaVeredas` (no se tocó la PC de Santiago).
> **No se compiló ni se corrieron las pruebas:** en este entorno no hay SDK de .NET. La lógica de cálculo se verificó con node sobre el JS espejo (`medicion-vereda.js`): los 24 casos de SPEC-001 §5.1 dan el resultado esperado.

## Commits (uno por spec, en este orden)

| Commit | Spec |
|---|---|
| `82376cf` | SPEC-003: contraseña 8-50 y SMTP en configuración |
| `22513a6` | SPEC-001: roturas (pozos) con tipo de suelo, medición por fórmula y cordón |
| `8b31d09` | SPEC-002: asignar veredas guardadas a paquetes |
| `e81b993` | SPEC-004: proyecto de pruebas SistemaVeredas.Tests (xUnit) |

`Docs/`, `Specs/`, `.claude/` y `CLAUDE.md` siguen sin versionar (no eran parte del pedido); este archivo tampoco está en ningún commit.

## Archivos por spec

### SPEC-003
- **Nuevos:** `Services/SmtpOptions.cs`, `Services/EmailService.cs` (`IEmailService`, `ResultadoEnvio`, asunto y cuerpo nuevos).
- **Modificados:** `Models/ViewModels/UsuarioCreateViewModel.cs`, `UsuarioEditViewModel.cs`, `RecoveryPasswordViewModel.cs` (regla 8-50 y mensajes); `Controllers/AccesController.cs` (se borró `Sendemail` con la cuenta y la contraseña de Gmail y `urlDomain`; `StartRecovery` POST con `[ValidateAntiForgeryToken]` y manejo de `NoConfigurado`/`Fallo`; se quitó la comparación manual de contraseñas); `Program.cs` (registro de `SmtpOptions` y `IEmailService`); `appsettings.json` (`Smtp.Host` y `Smtp.Puerto`); `Views/Access/Login.cshtml`, `StartRecovery.cshtml`, `Recovery.cshtml` (textos SistemaVeredas, `Json.Serialize` en SweetAlert, lista de requisitos, scripts de validación, `<img>` roto quitado); `Views/Usuarios/Create.cshtml` y `Edit.cshtml` (texto de ayuda).
- `git grep` de la cuenta de Gmail y de `NetworkCredential(` con literal: sin resultados en los archivos versionados.

### SPEC-001
- **Nuevos:** `Models/Rotura.cs`, `Models/ViewModels/RoturaFilaViewModel.cs`, `Services/MedicionService.cs`, `Views/Veredas/_Medicion.cshtml`, `Views/Veredas/_TipoSueloRapido.cshtml`, `wwwroot/js/medicion-vereda.js`, `Migrations/20260930120000_RoturasEnVeredas.cs` y `.Designer.cs` (escritos a mano).
- **Eliminados:** `Models/Medicion.cs`, `Controllers/MedicionesController.cs`, `Views/Mediciones/*`, `Models/ViewModels/MedicionItemViewModel.cs`, `Views/Shared/_MedicionesForm.cshtml`, `wwwroot/js/mediciones-form.js`.
- **Modificados:** `Models/Vereda.cs`, `Models/TipoSuelo.cs`, `Data/AppDbContext.cs`, `Migrations/AppDbContextModelSnapshot.cs`, `Models/ViewModels/HomeViewModel.cs`, `Controllers/VeredasController.cs`, `Controllers/TipoSuelosController.cs` (`CrearRapido`, RN-13), `Controllers/HomeController.cs`, `Views/Veredas/Create|Edit|Index|Details.cshtml`, `Views/Home/Index|Veredas|Paquetes.cshtml`, `Views/TipoSuelos/Delete|Index.cshtml`, `Views/_ViewImports.cshtml` (`@using SistemaVeredas.Services`).

### SPEC-002
- **Nuevos:** `Services/PaqueteService.cs` (con `VeredaTotales`), `Models/ViewModels/PaqueteDetalleViewModel.cs`, `VeredaEnPaqueteViewModel.cs`, `TotalesPaquete.cs`, `AgregarVeredasViewModel.cs`, `VeredaSeleccionViewModel.cs`, `ResultadoAsignacion.cs`, `Views/Paquetes/AgregarVeredas.cshtml`, `Views/Shared/_Mensajes.cshtml`, `wwwroot/js/seleccion-veredas.js`.
- **Modificados:** `Controllers/PaquetesController.cs`, `Controllers/VeredasController.cs` (`AgregarAPaquete`, lista de paquetes en `Index`), `Program.cs` (`AddScoped<PaqueteService>`), `Views/Paquetes/Details|Index|Delete|Edit.cshtml` (en español), `Views/Veredas/Index.cshtml` (selección múltiple), `Views/Home/Paquetes.cshtml` (usa `CalcularTotales`), `Views/Shared/_Layout.cshtml` (parcial `_Mensajes`).

### SPEC-004
- **Nuevos:** `SistemaVeredas.Tests/SistemaVeredas.Tests.csproj`, `xunit.runner.json`, `Unitarias/MedicionServiceTests.cs`, `PaqueteTotalesTests.cs`, `ContrasenaValidacionTests.cs`, `Integracion/AppFactory.cs`, `BaseDePruebasCollection.cs`, `ClienteExtensions.cs`, `EmailServiceFalso.cs`, `InfraestructuraTests.cs`, `AccesoTests.cs`, `RoturasTests.cs`, `PaquetesTests.cs`, `ContrasenasTests.cs`.
- **Modificados:** `SistemaVeredas.csproj` (excluye `SistemaVeredas.Tests\**` y `TestResults\**`), `SistemaVeredas.sln` (proyecto agregado), `.gitignore` (`TestResults/`), `Program.cs` (log «Base de datos: {Base} en {Servidor}» y `public partial class Program { }`).
- Paquetes: `Microsoft.NET.Test.Sdk 17.14.1`, `xunit 2.9.3`, `xunit.runner.visualstudio 3.1.4`, `Microsoft.AspNetCore.Mvc.Testing 9.0.9`. EF Core y SqlClient llegan por la referencia al proyecto web (SPEC-004 §4.2), por eso no se agregó `Microsoft.EntityFrameworkCore.SqlServer`.

## Decisiones que tomé

1. **Criterios del coordinador aplicados:** total = suma de subtotales redondeados; tipos por posición; máximo 9.999,99 m por medida; RN-13 marca como no disponible; la migración no toca el tipo «Cordón»; proveedor del paquete sin cambios (P-12); textos SistemaVeredas (P-19).
2. **Errores de la medición:** se muestran todos los mensajes de `Medicion` en una lista (no con `asp-validation-for`, que muestra solo el primero), para cumplir RNF-03.
3. **Número que no entra en un `decimal`** (p. ej., 32 cifras): se informa como «medida mayor a 9.999,99 m» en lugar de lanzar una excepción.
4. **Regex con `[0-9]`** en C# y JS (no `\d`), para que servidor y navegador den exactamente lo mismo.
5. **JS con BigInt:** el cálculo en vivo usa aritmética exacta, así el redondeo coincide con el servidor también en casos como `4.35*0.1` (0,44).
6. **Formato es-AR armado a mano** (`NumberFormatInfo` con coma decimal y punto de miles), sin depender de la cultura instalada en el servidor.
7. **«vereda(s)» con concordancia:** «1 vereda» / «3 veredas» en los mensajes de RN-13, y «1 seleccionada» / «n seleccionadas».
8. **Alta rápida:** duplicado considera `Medida` nula y vacía como iguales; se valida con los valores ya recortados (`ModelState.Clear()` + `TryValidateModel`). La URL sale de `Url.Action` (atributo `data-url` del modal).
9. **Sesión vencida en el alta rápida:** `response.redirected`, o respuesta 2xx que no es JSON → mensaje de sesión vencida; cualquier otra respuesta no JSON (p. ej., 400 del token) → «No se pudo agregar…».
10. **`StartRecovery` muestra `TempData["Error"]`:** antes no lo mostraba, y los enlaces inválidos redirigen ahí.
11. **`ResultadoAsignacion.DireccionesYaEstaban`:** lo agregué porque el mensaje de «1 ya estaba» necesita la dirección.
12. **`Paquetes/Index`:** la cantidad de veredas va en `ViewData["CantidadVeredas"]` (diccionario armado con `GroupBy`, sin traer las veredas).
13. **Parte B (Veredas/Index):** las casillas están fuera del formulario y se asocian con el atributo `form="formAgregarAPaquete"`.
14. **Delete de paquete con 1 vereda:** «Este paquete tiene 1 vereda. No se borra: queda sin paquete.»
15. **Pruebas extra** además de las pedidas: `ElModeloNoTieneCambiosSinMigracion` (`HasPendingModelChanges`), `ExisteLaTablaRoturas`, la traba `_Pruebas`, tipo inexistente, alta rápida, falla de envío y POST de recuperación sin token.

## Riesgos de compilación y ejecución que veo

1. **Migración y snapshot escritos a mano.** EF Core 9 lanza `PendingModelChangesWarning` como error en `Migrate()` si el snapshot no coincide con el modelo. Revisé las diferencias contra el Designer anterior, pero conviene correr `dotnet ef migrations has-pending-model-changes` antes de todo. La prueba `ElModeloNoTieneCambiosSinMigracion` también lo detecta.
2. **`IDbContextOptionsConfiguration<AppDbContext>`** (EF Core 9, namespace `Microsoft.EntityFrameworkCore.Infrastructure`): si el nombre no resuelve, quitar esa línea de `AppFactory` (el `RemoveAll<DbContextOptions<AppDbContext>>` y la cadena en memoria alcanzan).
3. **Razor:** revisé las transiciones (`@` dentro de bloques de código, `texto@(`), pero no hay compilador. Lo más delicado: `Views/Veredas/_Medicion.cshtml`, `Views/Veredas/Details.cshtml` (sección Medición) y `Views/Paquetes/Details.cshtml`.
4. **`ExecuteUpdateAsync` con el paquete borrado:** atrapo `DbUpdateException` y `SqlException`; si EF envuelve el error en otro tipo, el caso borde 4 de SPEC-002 daría 500.
5. **`SqlQueryRaw<int?>`** en `NoExisteLaTablaMediciones`: si EF no acepta `int?` como escalar, cambiar a `int` con `ISNULL(OBJECT_ID(...), 0)`.
6. **Versiones NuGet:** las elegí sin restaurar; si alguna no existe, usar la última de la misma familia.
7. **`wwwroot/lib` no está en la copia:** no afecta la compilación, pero las pruebas E2E necesitan la carpeta de la PC de Santiago.

## Hallazgos fuera de alcance (no los toqué)

- **Seguridad, grave:** `AccessController.Recovery` busca el usuario por `token_recovery == token`. Como el valor «bloqueado» es el texto fijo `tokenbloqueado`, cualquiera puede abrir `/Access/Recovery?token=tokenbloqueado` y cambiar la contraseña del primer usuario que tenga ese valor, sin iniciar sesión. Propuesta: rechazar `token == "tokenbloqueado"` (o guardar `null`) en `Recovery` GET y POST. Ya estaba antes de esta entrega; hay que decidirlo en `Docs/05`.
- **R-01 sigue abierto:** la contraseña de aplicación de Gmail sigue en el commit base de esta copia y en el historial del repositorio de Santiago. Hay que revocarla.

## Cómo compilar y correr las pruebas (PC de Santiago)

```
dotnet build SistemaVeredas.sln
dotnet ef migrations has-pending-model-changes        (tiene que decir que no hay cambios)
dotnet test SistemaVeredas.Tests --logger "trx;LogFileName=ultima.trx" --logger "console;verbosity=normal" --results-directory TestResults
```

- La base de pruebas es `GestionVeredas_Pruebas` en `DESKTOP-DTLN15N` (o la de `SISTEMAVEREDAS_TEST_DB`, que tiene que terminar en `_Pruebas`). Se borra y se recrea en cada corrida.
- Antes de publicar: configurar User Secrets `Smtp:Usuario`, `Smtp:Contrasena` y `Smtp:Remitente`, y el bloque `Smtp` en `appsettings.Production.json` (no lo modifiqué). Probar la migración sobre una copia de la base publicada (D-28, R-05).
