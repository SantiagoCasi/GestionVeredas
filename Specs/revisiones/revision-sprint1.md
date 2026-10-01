# Revisión de código — Sprint 1

> 01/10/2026 · Revisor: agente `desarrollador` · Copia de trabajo `/home/claude/work/SistemaVeredas`
> Alcance: commits `82376cf` (SPEC-003), `22513a6` (SPEC-001), `8b31d09` (SPEC-002) y `e81b993` (SPEC-004), diff contra `6587448` (Base). Se revisó contra `Specs/SPEC-001..004` y `Specs/pruebas/entrega-programador-sprint1.md`.
> **Método:** no hay SDK de .NET en este entorno. La revisión de C#, Razor, EF Core 9, ASP.NET Core 9 y xUnit 2.9 se hizo leyendo el código, como lo haría el compilador. El JS espejo se ejecutó con node para confirmar los casos de O-01 y O-04. No se modificó ningún archivo de código.

## Veredicto

**No se aprueba todavía:** hay **2 bloqueantes** (O-01 y SEG-01). Las otras observaciones son sugerencias. O-03 conviene hacerla junto con O-01, porque si no, el mensaje nuevo de O-01 no se llega a ver.

No encontré errores de compilación seguros. La migración escrita a mano y el snapshot coinciden con el modelo (sección 2). Lo que queda para confirmar en la primera compilación está en la sección 4.

| # | Tipo | Tema |
|---|---|---|
| O-01 | **Bloqueante** | `Veredas.Medicion` (y `MedicionCordon`) normalizada puede pasar de `nvarchar(500)` → error 500 |
| SEG-01 | **Bloqueante** | `/Access/Recovery?token=tokenbloqueado` cambia la contraseña sin sesión |
| O-03 | Sugerencia (hacerla con O-01) | El JS borra al cargar los errores del servidor de «Medición» y «Medición cordón» |
| O-04 | Sugerencia | Servidor y JS no dan el mismo error con números límite |
| O-05 | Sugerencia / PM | Se acepta un pozo con subtotal 0,00 m² |
| O-06 | Sugerencia | Para pozos nuevos se ofrecen y se aceptan tipos «no disponibles» |
| O-07 | Sugerencia | En Edit, las fotos se borran del disco antes del `SaveChanges` |
| O-08 | Sugerencia | «(0 pozos)» en las tarjetas de una vereda con solo cordón |
| O-09 | Sugerencia | Las fábricas de `WithWebHostBuilder` no se liberan |
| O-10 | Sugerencia | Faltan pruebas de regresión de O-01 y SEG-01 |
| O-11 | Sugerencia | `Contains` sobre listas usa `OPENJSON`: revisar el nivel de compatibilidad de la base del hosting |

---

## 1. Observaciones

### O-01 — Bloqueante — La fórmula normalizada puede superar `nvarchar(500)`

**Dónde:** `Services/MedicionService.cs:138` (arma `"(" + Medidas + ")"` por término), `Models/Vereda.cs:116` y `:128` (`[StringLength(500)]`), `Views/Veredas/_Medicion.cshtml:28` y `:85` (`maxlength="500"`), `Controllers/VeredasController.cs:131-138` y `:203-225`.

**Qué pasa:** `[StringLength(500)]` y `maxlength` controlan **lo que escribe el usuario**, durante el model binding. Después `AplicarAVereda` reemplaza `vereda.Medicion` por la fórmula normalizada, que agrega dos paréntesis por término, y nadie vuelve a validar el largo. EF Core no controla largos. Si un término viene sin paréntesis, crece 2 caracteres; en el peor caso la fórmula crece 1,5 veces.

**Caso concreto (verificado con el JS espejo):** `1*1+1*1+…` con 125 términos mide **499** caracteres (pasa el `maxlength` y el `StringLength`). La fórmula normalizada mide **749**. SQL Server responde «String or binary data would be truncated», `SaveChangesAsync` lanza `DbUpdateException` y el usuario ve la página de error (500). Pasa lo mismo con `MedicionCordon` (`(a*b*c)`). En **Edit** es peor: las fotos marcadas para borrar ya se borraron del disco (ver O-07). `Rotura.Medidas` no tiene el problema, porque es un término sin paréntesis y siempre es más corto que la entrada.

**Corrección** (sin migración: la columna sigue en 500, como dice la spec):

```csharp
// Services/MedicionService.cs
public const int LargoMaximoFormula = 500; // Veredas.Medicion y Veredas.MedicionCordon son nvarchar(500)
public const string MensajeLargoMaximo = "La medición puede tener hasta 500 caracteres."; // mismo texto que SPEC-001 §4.2

// en Calcular, reemplazando la línea 138:
var normalizado = string.Join("+", lista.Select(x => "(" + x.Medidas + ")"));
if (normalizado.Length > LargoMaximoFormula)
    return ConErrores(new List<string> { MensajeLargoMaximo });
```

- Como el control está en `Calcular`, cubre la medición y el cordón. El controlador ya pasa el error a `ModelState` en el campo `Medicion` o `MedicionCordon`.
- **JS espejo** (`wwwroot/js/medicion-vereda.js`, en `calcular` antes del `return` final): calcular `normalizado` y, si `normalizado.length > 500`, devolver `{ total: null, normalizado: null, terminos: [], errores: ['La medición puede tener hasta 500 caracteres.'] }`. Agregar `largo` a `msj`. Así el envío se bloquea en el navegador y no se pierden las fotos elegidas.
- Pruebas (O-10): `"1*1"` repetido 83 veces da 497 caracteres normalizados (válido) y repetido 84 veces da 503 (error). Una prueba de integración con 84 términos en Create debe devolver 200 con el mensaje y no crear la fila.
- Hay que hacer también O-03: si no, el JS borra este mensaje al cargar la página.

### SEG-01 — Bloqueante — Cambiar la contraseña sin sesión con `token=tokenbloqueado`

**Dónde:** `Models/Usuario.cs:36` (`token_recovery = "tokenbloqueado"` por defecto), `Controllers/AccesController.cs:169-192` (Recovery GET), `:194-224` (Recovery POST), `:161` y `:216` (vuelven a poner el literal).

**Qué pasa:** «bloqueado» es un texto fijo y conocido, y `Recovery` busca `u.token_recovery == token` sin controlar el valor. Todos los usuarios que nunca pidieron recuperar la contraseña, o que ya la recuperaron, o cuyo envío falló, tienen ese valor. Cualquiera puede abrir `/Access/Recovery?token=tokenbloqueado` y enviar una contraseña nueva: el POST le cambia la contraseña al **primer** usuario que devuelve `FirstOrDefaultAsync` (sin `OrderBy`; en la práctica, el de menor `UsId`, que suele ser el usuario inicial). Después puede iniciar sesión con ese usuario. `AccessController` es `[AllowAnonymous]`, así que no hace falta sesión ni hay otro control.

**Ojo:** con la intercalación por defecto de SQL Server (no distingue mayúsculas) y el `=` de SQL (no tiene en cuenta los espacios del final), `TOKENBLOQUEADO` o `tokenbloqueado%20` también coinciden. Por eso no alcanza con `if (token == "tokenbloqueado")`: hay que validar el **formato** del token.

El problema ya estaba en la Base, pero es bloqueante: esta entrega toca este controlador y la falla permite tomar una cuenta sin sesión. Si esta versión de `AccesController` está publicada, el sitio en línea ya es vulnerable: conviene publicar la corrección cuanto antes.

**Corrección** (sin migración):

```csharp
// Controllers/AccesController.cs
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

public const string TokenBloqueado = "tokenbloqueado";
// StartRecovery genera Guid.NewGuid().ToString("N"): 32 caracteres hexadecimales en minúscula.
private static readonly Regex FormatoToken = new("^[0-9a-f]{32}$", RegexOptions.Compiled);
private static bool TokenValido([NotNullWhen(true)] string? token) => token != null && FormatoToken.IsMatch(token);

// Recovery GET: reemplaza el if de la línea 171 y va ANTES de consultar la base
if (!TokenValido(token))
{
    TempData["Error"] = MensajeTokenInvalido;
    return RedirectToAction("StartRecovery", "Access");
}

// Recovery POST: primera instrucción del método, antes de ModelState y de la consulta (líneas 199-212)
if (!TokenValido(model.token))
{
    TempData["Error"] = MensajeTokenInvalido;
    return RedirectToAction("StartRecovery", "Access");
}
```

- Las líneas 161 y 216 y `Usuario.cs:36` usan la constante `AccessController.TokenBloqueado` (o una constante en `Usuario`) en lugar del literal. La regex rechaza el valor bloqueado en cualquier variante, porque tiene letras que no son hexadecimales.
- El control de la línea 208 (`string.IsNullOrEmpty(model.token)` **después** de la consulta) queda cubierto por el control nuevo, que va antes. Hay que mantener ese orden: si algún día el valor bloqueado pasa a ser `NULL`, EF traduce `token_recovery == null` como `IS NULL` y la consulta encontraría usuarios.
- Mejora opcional (decisión en `Docs/05`, no para este sprint): guardar `NULL` como «bloqueado» (requiere una migración de datos `UPDATE Usuarios SET token_recovery = NULL WHERE token_recovery = 'tokenbloqueado'`) y agregar vencimiento (SPEC-003 Q-03).
- Pruebas (O-10): GET `/Access/Recovery?token=tokenbloqueado` → 302 a `/Access/StartRecovery`. POST con `token=tokenbloqueado` (y con `TOKENBLOQUEADO`) y dos contraseñas válidas que coinciden → 302 a StartRecovery y el hash de `UsContrasena` del usuario de prueba **no cambia**.

### O-03 — Sugerencia (hacerla con O-01) — El JS borra los errores del servidor de la fórmula

**Dónde:** `wwwroot/js/medicion-vereda.js:259` (`mostrarErrores(erroresMedicion, [])`), `:270`, y la llamada inicial `:409-410`. `Views/Veredas/_Medicion.cshtml:30-35` y `:87-92` dibujan los errores del servidor.

**Qué pasa:** al cargar la página, `actualizarMedicion()` y `actualizarCordon()` vuelven a calcular la fórmula. Si en el navegador es válida, vacían `.medicion-errores` y `.cordon-errores`. Los errores que **solo** detecta el servidor desaparecen sin que el usuario los vea: «La cantidad de pozos no coincide…», el de largo de `StringLength` y, cuando se corrija O-01, «La medición puede tener hasta 500 caracteres.» si el JS no tiene el control. Los errores por renglón (`tiposRotura[i]`) sí se conservan con `erroresServidor`.

**Corrección:** igual que con los renglones. Al iniciar, guardar el texto de `.medicion-errores` y de `.cordon-errores`. Mientras el usuario no cambie el campo, mostrar la unión de esos errores y los del cálculo en vivo. En el evento `input` de cada campo, olvidarlos (como hoy se hace `erroresServidor = {}` en la línea 276).

### O-04 — Sugerencia — Servidor y JS no dan el mismo error con números límite

**Dónde:** `Services/MedicionService.cs:177-192` contra `wwwroot/js/medicion-vereda.js:112-122`. SPEC-001 §3.6.1 pide las mismas reglas y los mismos mensajes.

El servidor convierte cada medida mientras la valida (`decimal.TryParse`, máximo 28 decimales y unos 29 dígitos). El JS primero valida el formato de todas las medidas y después compara con BigInt, sin límite. Casos (JS verificado con node; servidor, por lectura del código):

| Entrada (2 medidas) | Servidor | JS |
|---|---|---|
| `0,0000000000000000000000000000001*5` | *medida en cero* (el `decimal` redondea a 0) | válida, total 0,00 m² |
| `999999999999999999999999999999*2,` | *medida mayor a 9.999,99 m* (falla el `TryParse` de la primera) | *número inválido* «2,» |
| `0*99999999999999999999999999999` | *medida mayor a 9.999,99 m* | *medida en cero* |

No hay riesgo de datos (el servidor siempre rechaza), pero el aviso en vivo no coincide con el del servidor.

**Corrección:** en `RevisarTermino`, primero validar el formato (regex) de **todas** las medidas y después convertirlas, igual que el JS. Y limitar la cantidad de decimales por medida en los dos lados (por ejemplo, regex `^[0-9]+([.,][0-9]{1,3})?$` / `^[.,][0-9]{1,3}$`). Con eso todos los valores entran en un `decimal`, el producto es exacto y desaparecen los tres casos. El límite de decimales es una regla nueva: **pregunta para el PM** (¿alcanza con milímetros, 3 decimales?). Hay que actualizar §4.1 si se aprueba.

### O-05 — Sugerencia / PM — Pozo con subtotal 0,00 m²

**Dónde:** `Services/MedicionService.cs:124`. `0,001*1` (o `0,004*1`) es válido y deja una rotura de 0,00 m² (verificado con el JS). La spec no lo prohíbe. **Pregunta para el PM:** ¿se rechaza un pozo cuyo subtotal redondeado es 0 (mensaje sugerido: «El término {n} («{t}») da 0,00 m². Revisá las medidas.»)? Si se agrega O-04 con 3 decimales, el caso sigue existiendo (`0,001*1`).

### O-06 — Sugerencia — Tipos «no disponibles» para pozos nuevos

**Dónde:** `Controllers/VeredasController.cs:295` y `Views/Veredas/_Medicion.cshtml:101-109` (la plantilla de los renglones nuevos usa la misma lista que incluye los no disponibles que ya están en uso), `Controllers/VeredasController.cs:331-343` (`ValidarTiposSueloAsync` solo controla que el tipo exista).

SPEC-001 §5.3: «Para pozos nuevos no se ofrece». Hoy un renglón nuevo puede elegir un tipo «(no disponible)» y el servidor lo acepta.

**Corrección:** en `<template class="plantilla-tipos">` dibujar solo los `SelectListItem` disponibles. Por ejemplo, `CargarListas` deja también `ViewData["TiposSueloNuevos"]`, o se marca cada opción con `data-disponible`. En el servidor, aceptar un tipo no disponible solo si la vereda ya lo usaba antes de editar (en Edit, leer los `TipoSueloId` de `actuales` antes de validar). En Create, rechazarlo con «El tipo de suelo del pozo {n} no está disponible. Elegí otro de la lista.» (mensaje nuevo: confirmarlo con el PM).

### O-07 — Sugerencia — En Edit las fotos se borran antes de guardar

**Dónde:** `Controllers/VeredasController.cs:209-216` (borra los archivos y guarda los nuevos) antes de `:225` (`SaveChangesAsync`).

Si el `SaveChanges` falla (O-01, una FK, la base caída), los archivos marcados para borrar ya se borraron del disco y la base todavía los referencia. El orden ya estaba en la Base, pero ahora el `SaveChanges` hace más cosas (borra y crea roturas) y tiene más motivos para fallar.

**Corrección:** guardar primero los archivos nuevos y la base, y borrar del disco los marcados **después** del `SaveChangesAsync` que salió bien. Si falla, borrar los archivos nuevos que se habían guardado.

### O-08 — Sugerencia — «(0 pozos)» en las tarjetas

**Dónde:** `Views/Home/Veredas.cshtml:90`. Una vereda con solo cordón (`EstaMedida` y sin roturas) muestra «0,150 m³ (0 pozos)». **Corrección:** dibujar el `<span>` de los pozos solo si `v.Roturas.Count > 0`.

### O-09 — Sugerencia — Fábricas de prueba sin liberar

**Dónde:** `SistemaVeredas.Tests/Integracion/ContrasenasTests.cs:104` y `:132`. Cada `WithWebHostBuilder` levanta otro servidor en memoria (y vuelve a correr el arranque de `Program.cs`), y nunca se libera. **Corrección:** `await using var fabrica = _f.WithWebHostBuilder(...);` (`WebApplicationFactory<T>` es `IAsyncDisposable`).

### O-10 — Sugerencia — Pruebas de regresión que faltan

Agregar a SPEC-004 y al proyecto de pruebas:

- `Unitarias/MedicionServiceTests`: largo de la fórmula normalizada, con 83 términos (válido) y 84 (error con el mensaje de O-01), con 2 y con 3 medidas.
- `Integracion/RoturasTests`: Create con 84 términos `1*1` y un tipo por pozo → 200, mensaje visible en el HTML y sin fila.
- `Integracion/ContrasenasTests`: los dos casos de SEG-01 (GET y POST con `tokenbloqueado` y `TOKENBLOQUEADO`), afirmando que el hash no cambia.
- Opcional: un caso de O-03 no se puede probar sin navegador; queda para la ejecución E2E de QC.

### O-11 — Sugerencia — `Contains` sobre listas y `OPENJSON`

**Dónde:** `Services/PaqueteService.cs:141`, `:149`, `:162`; `Controllers/VeredasController.cs:295`, `:337`. EF Core 8 y 9 traducen `lista.Contains(x)` con `OPENJSON`, que necesita nivel de compatibilidad **130 o más** en la base. En la PC de Santiago no hay problema; en MonsterASP hay que confirmarlo (`SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()`). Si da menos de 130: `options.UseSqlServer(cs, o => o.UseCompatibilityLevel(120))` en `Program.cs` (y en `AppFactory`). Si no, la asignación de paquetes y el formulario de veredas fallan solo en el hosting.

---

## 2. Migración `RoturasEnVeredas` y snapshot contra el modelo

Comparé `Migrations/20260930120000_RoturasEnVeredas.cs`, el `.Designer.cs` y `AppDbContextModelSnapshot.cs` con `Models/*.cs` y `Data/AppDbContext.cs`. **Coinciden:** no tendría que haber cambios de modelo pendientes y `Migrate()` no tendría que cortar.

| Control | Resultado |
|---|---|
| `Designer.BuildTargetModel` = `Snapshot.BuildModel` | Iguales línea por línea (solo cambia el nombre del método). |
| `ProductVersion` | `9.0.20` en los dos, igual que las migraciones anteriores y el paquete. |
| Id de la migración | `20260930120000` es posterior a `20260925145438_Usuarios`; `[Migration]` y `[DbContext]` correctos. |
| `Roturas` | `Id` identity; `VeredaId`, `Orden`, `TipoSueloId` `int`; `Medidas` `nvarchar(500)` requerido (`[Required, StringLength(500)]`); `SubtotalM2` `decimal(10,2)` (`[Column]`). |
| FKs | `FK_Roturas_Veredas_VeredaId` Cascade y `FK_Roturas_TiposSuelo_TipoSueloId` Restrict, `IsRequired()`, navegaciones `Vereda.Roturas` y `TipoSuelo.Roturas`, igual que `OnModelCreating`. |
| Índices | `IX_Roturas_VeredaId` e `IX_Roturas_TipoSueloId` (los de las FK, por convención); sin índice único `(VeredaId, Orden)`, como pide la spec. |
| `Veredas` | `Medicion` y `MedicionCordon` `nvarchar(500)` nulables; `TotalM2` `decimal(10,2)?`; `TotalCordonM3` `decimal(10,3)?`; `TieneCordon` `bit` no nulo con `defaultValue: false` solo en la migración (correcto: el modelo no tiene `HasDefaultValue`). |
| `[NotMapped]` | `Vereda.EstaMedida` y `TipoSuelo.Descripcion` no aparecen en el snapshot (correcto). |
| `Mediciones` | Se quita del snapshot (entidad y relaciones); `DropTable` en `Up`; `Down` la vuelve a crear igual que `Inicial` (columnas, tipos, FK e índices). |
| Migraciones anteriores | Siguen nombrando `SistemaVeredas.Models.Medicion` como texto; compilan aunque la clase ya no exista. |

Antes de publicar (D-28, R-05): `dotnet ef migrations has-pending-model-changes` y `dotnet ef migrations script 20260925145438_Usuarios 20260930120000_RoturasEnVeredas`, y probar el script sobre una copia de la base publicada.

## 3. Compilación (C#, Razor, EF Core 9, ASP.NET Core 9, xUnit 2.9): revisado sin hallazgos

- **Usings y namespaces:** `SistemaVeredas.Services` en `_ViewImports` (`MedicionService`, `PaqueteService`, `VeredaTotales`); `SelectListItem` llega por los imports por defecto de Razor; `Estado` y `Prioridad` por `SistemaVeredas.Models.Enums`; `GetDisplayName`/`BadgeClass` por `SistemaVeredas.Extensions`.
- **Nombres coherentes** entre modelo, controlador, vista, JS y pruebas: `Medicion`, `TieneCordon`, `MedicionCordon`, `TotalM2`, `TotalCordonM3`, `Roturas`, `tiposRotura[i]`, `veredaIds`, `paqueteId`, `veredaId`, `TempData` `Exito`/`Aviso`/`Error`/`ExitoEnlace`/`Mensaje`/`MensajeExito`, `ViewData` `TiposSuelo`/`Roturas`/`Paquetes`/`CantidadVeredas`/`VeredasQueLoUsan`.
- **Razor:** `selected="@(bool)"` en `<option>` (con tag helper, `false` quita el atributo); `@superficie` dentro de bloques de código; `foreach (var r in X.OrderBy(r => …))` (el ámbito de la variable de iteración es solo el cuerpo); `@Json.Serialize((string?)ViewBag.Error)`; `@model` correcto en `_Medicion`, `Paquetes/Details` (`PaqueteDetalleViewModel`) y `AgregarVeredas` (`AgregarVeredasViewModel`).
- **Nulabilidad:** sin errores; a lo sumo advertencias (`string?` en Razor, `Assert.Contains` con `string?`).
- **EF Core 9:** `ExecuteUpdateAsync` con `SetProperty(v => v.PaqueteId, (int?)…)`; `ToDictionaryAsync`; `HasPendingModelChanges()`; `IDbContextOptionsConfiguration<TContext>` existe en EF 9 (`Microsoft.EntityFrameworkCore.Infrastructure`), así que el riesgo 2 de la entrega no aplica; `ExecuteUpdate` lanza `SqlException` sin envolver, y el `catch` de `PaqueteService.cs:152` la atrapa (riesgo 4 cubierto).
- **xUnit 2.9:** `IAsyncLifetime` en el fixture de colección, con `DisposeAsync` explícito (no choca con el `ValueTask DisposeAsync` de `WebApplicationFactory`); `[CollectionDefinition]`/`ICollectionFixture`; `<Using Include="Xunit" />`.
- **Proyectos:** el web excluye `SistemaVeredas.Tests\**` (incluido su `obj`) de `Compile`, `Content`, `None` y `EmbeddedResource`; el de pruebas recibe EF Core, SqlClient y el framework de ASP.NET Core por la referencia al proyecto y por `Mvc.Testing`.

## 4. Para confirmar en la primera compilación y corrida (no son observaciones)

1. `SqlQueryRaw<int?>` en `InfraestructuraTests` (riesgo 5 de la entrega): debería andar. Si EF lo rechaza, usar `int` con `ISNULL(OBJECT_ID(...), 0)`.
2. `MapStaticAssets` dentro de `WebApplicationFactory`: si el arranque falla por el manifiesto de recursos estáticos, aplicar SPEC-004 §8 caso 7.
3. Versiones NuGet sin restaurar (`Microsoft.NET.Test.Sdk 17.14.1`, `xunit.runner.visualstudio 3.1.4`, `Mvc.Testing 9.0.9`): si alguna no existe, usar la última de la misma familia.
4. `InfraestructuraTests.ElModeloNoTieneCambiosSinMigracion` en verde confirma la sección 2.

## 5. Preguntas para el PM que salen de esta revisión

- **R-Q1 (O-04):** ¿máximo de 3 decimales por medida?
- **R-Q2 (O-05):** ¿se rechaza un pozo que da 0,00 m²?
- **R-Q3 (O-06):** texto del error para un tipo no disponible en un pozo nuevo.
- **R-Q4 (SEG-01):** ¿se registra en `Docs/05` el cambio a `NULL` como valor bloqueado y el vencimiento del enlace, para un sprint próximo?

---

## Respuesta del programador

> 01/10/2026 · Agente `programador` · Commits `0def84f` (Revisión: O-01 y O-03), `e093f8a` (Revisión: SEG-01), `39e5e95` (Revisión: sugerencias). Sigue sin haber SDK de .NET: no se compiló ni se corrieron las pruebas. El JS espejo se verificó con node (casos de O-01 y O-04).

| # | Qué hice |
|---|---|
| O-01 | **Corregido** (`0def84f`). `MedicionService.LargoMaximoFormula = 500` y `MensajeLargoMaximo`; `Calcular` rechaza la fórmula normalizada de más de 500 caracteres, así cubre `Medicion` y `MedicionCordon`. Mismo control y mensaje en `medicion-vereda.js` (bloquea el envío). Pruebas: unitarias con 83/84 términos `1*1` (497/503) y 62/63 términos `1*1*1` (495/503), cordón de más de 500; integración `Crear_FormulaNormalizadaDeMasDe500_NoGuarda` y `Editar_FormulaDeMasDe500_NoCambiaNada` (200, mensaje, sin cambios en la base y la foto sigue en `Fotos`). |
| SEG-01 | **Corregido** (`e093f8a`). `AccessController.TokenValido` con `^[0-9a-f]{32}$`; en Recovery GET reemplaza al `IsNullOrEmpty` y en el POST es la primera instrucción, antes de `ModelState` y de la consulta. Constante `Usuario.TokenBloqueado` en lugar del literal (en el modelo y en las dos asignaciones del controlador; no cambia el snapshot). Pruebas: GET con `tokenbloqueado`, `TOKENBLOQUEADO`, `tokenbloqueado ` y vacío → 302 a StartRecovery; POST con las tres variantes → 302 y el hash del usuario nuevo y del usuario de prueba no cambia; token válido → cambia la contraseña una sola vez. Conviene publicar esta corrección cuanto antes. |
| O-03 | **Corregido** (`0def84f`). El JS guarda al cargar los errores del servidor de `.medicion-errores` y `.cordon-errores` y los muestra unidos a los del cálculo en vivo hasta el primer `input` del campo. No tiene prueba automática (queda para E2E de QC). |
| O-04 | **Corregido sin regla nueva** (`39e5e95`). `RevisarTermino` valida primero el formato de todas las medidas, después cero (por texto: sin cifras de 1 a 9) y después el máximo (incluye el `TryParse` que falla). Los tres casos de la tabla dan lo mismo en servidor y JS; hay pruebas unitarias. El límite de 3 decimales (R-Q1) queda para el PM. |
| O-05 | **Sin cambio:** pregunta para el PM (R-Q2). |
| O-06 | **Parcial** (`39e5e95`). La plantilla de los renglones nuevos usa `ViewData["TiposSueloNuevos"]` (solo disponibles). El rechazo en el servidor necesita un mensaje nuevo (R-Q3): queda para cuando el PM lo confirme. |
| O-07 | **Corregido** (`0def84f`, como parte del bloqueante O-01). En Edit se guardan las fotos nuevas, después la base, y recién con el `SaveChanges` bien se borran del disco las marcadas. Si la base falla, se borran las fotos nuevas y se relanza. |
| O-08 | **Corregido** (`39e5e95`). «(n pozos)» solo si hay roturas. |
| O-09 | **Corregido** (`39e5e95`). `await using var fabrica = _f.WithWebHostBuilder(...)`. |
| O-10 | **Hecho** con las pruebas de O-01, SEG-01 y O-04 indicadas arriba. |
| O-11 | **Sin cambio de código:** hay que confirmar en MonsterASP `SELECT compatibility_level FROM sys.databases WHERE name = DB_NAME()`; si da menos de 130, agregar `o => o.UseCompatibilityLevel(120)` en `Program.cs` y `AppFactory`. |

Para confirmar en la primera compilación, además de la sección 4: que `decimal.TryParse` acepte medidas con más de 28 decimales (prueba `Calcular_MedidaMuyChica_NoEsCero`; en .NET Core 3.0+ redondea y no falla).
