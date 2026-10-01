# SPEC-002 — Asignar veredas guardadas a paquetes

> Borrador v0.1 · 30/09/2026 · Autor: agente `desarrollador` · Sprint 1 · Historias HU-09 a HU-12

## 1. Objetivo y requisitos que cubre

Santiago: *"a veces no sé a qué paquete irá la vereda hasta juntar varias; debe ser cómodo agregarlas al paquete una vez guardadas"* (D-20).

El flujo queda así: se cargan las veredas sin paquete → se crea el paquete → el sistema lleva directo a **Agregar veredas** → se marcan varias y se confirman → el detalle del paquete muestra sus veredas, los totales y el avance. Además (parte B, *Debería*), desde el listado de veredas se pueden marcar varias y mandarlas a un paquete.

| ID | Qué se cubre acá | Estado en Docs/03 |
|---|---|---|
| RF-PAQ-02 | Pantalla **Agregar veredas** desde el paquete, con selección múltiple. | Confirmado (P-13 abierta) |
| RF-PAQ-03 | Botón **Quitar** en el detalle del paquete. | A confirmar |
| RF-PAQ-04 | Detalle del paquete: veredas, cantidad, m², m³ y % finalizadas. | A confirmar (P-14) |
| RF-PAQ-06 | Eliminar paquete: sus veredas quedan sin paquete (se aclara en la confirmación). Editar queda como está. | A confirmar |
| RF-PAQ-08 | **Parte B:** selección múltiple en `Veredas/Index` con **Agregar al paquete…**. | Debería · A confirmar |
| RN-10 | Una vereda está como máximo en un paquete (se controla la concurrencia). | Vigente |
| RN-11 | Al eliminar un paquete sus veredas no se borran. | Vigente |
| RN-18 | La vereda se guarda sin paquete y se asigna después. El campo **Paquete** opcional del formulario de vereda **se mantiene**. | Confirmado |

**Depende de SPEC-001** (se escribe en paralelo). Según D-26 a D-29, esta spec asume que `Veredas` tiene `TotalM2` (`decimal(10,2)?`, **persistido**), `Medicion` (fórmula), `TieneCordon`, `MedicionCordon`, `TotalCordonM3` (decimal?) y `[NotMapped] EstaMedida`; que existe la tabla `Roturas` (VeredaId, Orden, Medidas, TipoSueloId obligatorio, SubtotalM2) y que la tabla `Mediciones` ya no existe. SPEC-002 **no lee `Roturas`**: los totales del paquete salen solo de `Veredas.TotalM2` y `Veredas.TotalCordonM3`. Si SPEC-001 cambia esos nombres, se ajustan acá.

## 2. Modelo de datos y migración

**No hay migración.** La relación ya existe y alcanza:

- `Veredas.PaqueteId int NULL` → FK a `Paquetes.Id`, `ON DELETE SET NULL` (migración `Inicial`), con índice `IX_Veredas_PaqueteId`. Cumple RN-10 (una FK = un paquete como máximo) y RN-11.
- `Paquetes.ProveedorId int NOT NULL` **no se toca**: sigue siendo obligatorio hasta que se resuelva **P-12**. El alta de paquete sigue pidiendo proveedor. Ver sección 8.

Para el % de avance se usa `Estado.Finalizado` (enum existente).

## 3. Diseño general

### 3.1 Servicio `Services/PaqueteService.cs` (nuevo)

Toda la lógica va acá; los controladores solo llaman al servicio, arman el mensaje y redirigen. Se registra en `Program.cs`:

```csharp
builder.Services.AddScoped<PaqueteService>();
```

Métodos (firma orientativa):

```csharp
public class PaqueteService
{
    Task<PaqueteDetalleViewModel?> ObtenerDetalleAsync(int paqueteId);
    Task<AgregarVeredasViewModel?> ObtenerParaAgregarAsync(int paqueteId);       // null si el paquete no existe
    Task<List<VeredaSeleccionViewModel>> VeredasSinPaqueteAsync();
    Task<ResultadoAsignacion> AgregarVeredasAsync(int paqueteId, IReadOnlyCollection<int> veredaIds);
    Task<bool> QuitarVeredaAsync(int paqueteId, int veredaId);                  // false si ya no estaba
    static TotalesPaquete CalcularTotales(IEnumerable<VeredaTotales> veredas);  // puro, con prueba unitaria
}
```

### 3.2 Asignación con control de concurrencia (RN-10)

Dos usuarios pueden tener abierta a la vez la pantalla de asignación (o el listado) y elegir la misma vereda para paquetes distintos. La asignación **nunca pisa** un `PaqueteId` que ya no sea `NULL`. Se hace con un `UPDATE` condicional, atómico por fila en SQL Server:

```csharp
public async Task<ResultadoAsignacion> AgregarVeredasAsync(int paqueteId, IReadOnlyCollection<int> veredaIds)
{
    var ids = veredaIds.Distinct().ToList();
    var paquete = await _db.Paquetes.AsNoTracking()
        .Where(p => p.Id == paqueteId).Select(p => new { p.Id, p.Nombre }).FirstOrDefaultAsync();
    if (paquete == null) return ResultadoAsignacion.PaqueteInexistente();

    // 1) Cuáles ya estaban en ESTE paquete antes (no cuentan como agregadas ni como salteadas).
    var yaEstaban = await _db.Veredas.Where(v => ids.Contains(v.Id) && v.PaqueteId == paqueteId)
        .Select(v => v.Id).ToListAsync();

    // 2) UPDATE ... SET PaqueteId = @p WHERE Id IN (...) AND PaqueteId IS NULL
    await _db.Veredas
        .Where(v => ids.Contains(v.Id) && v.PaqueteId == null)
        .ExecuteUpdateAsync(s => s.SetProperty(v => v.PaqueteId, paqueteId));

    // 3) Se relee el estado final: es la verdad, aunque otro usuario haya asignado en el medio.
    var final = await _db.Veredas.AsNoTracking().Where(v => ids.Contains(v.Id))
        .Select(v => new { v.Id, v.Calle, v.Altura, v.PaqueteId, PaqueteNombre = v.Paquete!.Nombre })
        .ToListAsync();

    // agregadas  = en este paquete ahora y no estaban antes
    // salteadas  = están en OTRO paquete (con su dirección y el nombre del otro paquete)
    // inexistentes = ids pedidos que ya no existen (se borraron mientras tanto)
    ...
}
```

- `ExecuteUpdateAsync` (EF Core 7+) no pasa por el change tracker ni por el `DbContext` de la vista: no hace falta `rowversion`.
- Si el paquete se elimina entre el paso 1 y el 2, el `UPDATE` falla por la FK (`DbUpdateException`/`SqlException` 547): se atrapa y se devuelve `PaqueteInexistente`.
- `QuitarVeredaAsync` es igual de condicional: `WHERE Id = @v AND PaqueteId = @p` → `SET PaqueteId = NULL`. Si afecta 0 filas, devuelve `false` (ya la habían quitado o movido).
- Las ids repetidas o inválidas que vengan del formulario se ignoran (`Distinct`, y las que no existen caen en "inexistentes").
- Límite defensivo: si llegan más de 1.000 ids en un POST, se responde 400 (RNF-14 habla de 1.000 veredas; no es un caso real de uso).

### 3.3 Totales del paquete (RF-PAQ-04)

`CalcularTotales` recibe, por vereda, `Estado`, `TotalM2` (decimal?) y `TotalCordonM3` (decimal?) y devuelve:

| Total | Cálculo | Formato en pantalla |
|---|---|---|
| Cantidad | número de veredas del paquete | entero |
| m² | Σ `TotalM2` (null = 0) | `N2` con coma decimal: `51,00 m²` |
| m³ | Σ `TotalCordonM3` (null = 0) | `N3`: `0,450 m³` |
| Sin medir | cantidad con `EstaMedida == false` | "(2 sin medir)" junto a los totales, si hay |
| Avance | `Finalizado / Cantidad × 100`, redondeado a entero (`MidpointRounding.AwayFromZero`) | `25 %` + barra; con 0 veredas: `0 %` y la barra vacía |

El denominador incluye **todas** las veredas del paquete, también "No corresponde" y "No se encontró" (ver pregunta Q-02). `TotalM2` y `TotalCordonM3` están persistidos en `Veredas`, así que la proyección los trae directo (sin `Include` de `Roturas`) y la suma se hace en C# sobre esa proyección (son pocas veredas por paquete). Como `EstaMedida` es `[NotMapped]`, la proyección no la puede traducir a SQL: se proyectan los campos en los que se basa según SPEC-001 (p. ej., `TotalM2 != null || TotalCordonM3 != null`) o se materializa la proyección antes de usarla.

Formato: `ToString("N2", new CultureInfo("es-AR"))` (RNF-01). Si SPEC-001 define un helper de formato, se usa ese.

## 4. Cambios por archivo

### 4.1 ViewModels nuevos (`Models/ViewModels/`)

| Archivo | Propiedades |
|---|---|
| `PaqueteDetalleViewModel.cs` | `Id`, `Nombre`, `Fecha`, `ProveedorNombre` (string), `Observacion`, `List<VeredaEnPaqueteViewModel> Veredas`, `TotalesPaquete Totales` |
| `VeredaEnPaqueteViewModel.cs` | `Id`, `Direccion`, `EntreCalles` (texto "entre X y Y" o null), `Estado`, `Prioridad?`, `EstaMedida`, `TotalM2?`, `TotalCordonM3?`, `FotoMiniatura?` (primera de `ListaFotos`) |
| `TotalesPaquete.cs` | `Cantidad`, `TotalM2`, `TotalM3`, `SinMedir`, `Finalizadas`, `PorcentajeAvance` (int) |
| `AgregarVeredasViewModel.cs` | `PaqueteId`, `PaqueteNombre`, `List<VeredaSeleccionViewModel> Veredas`; para el POST: `List<int> VeredaIds` |
| `VeredaSeleccionViewModel.cs` | `Id`, `Direccion`, `EntreCalles`, `Estado`, `Prioridad?`, `EstaMedida`, `TotalM2?`, `TotalCordonM3?`, `FechaReclamo?`, `TextoBusqueda` (dirección + entre calles, en minúsculas) |
| `ResultadoAsignacion.cs` | `bool PaqueteExiste`, `string PaqueteNombre`, `int Agregadas`, `int YaEstaban`, `List<(string Direccion, string Paquete)> Salteadas`, `int Inexistentes` |

`Paquete` (entidad) no se usa más como modelo de `Details`.

### 4.2 `Controllers/PaquetesController.cs`

Recibe `PaqueteService` por constructor. Acciones:

| Acción | Verbo y ruta | Qué hace |
|---|---|---|
| `Details(int? id)` | GET `/Paquetes/Details/5` | `ObtenerDetalleAsync`; `null` → `NotFound()`. Vista nueva. |
| `Create` (POST) | POST `/Paquetes/Create` | Igual que hoy, pero al guardar: `TempData["Exito"] = "Se creó el paquete «{Nombre}». Ahora elegí las veredas que van en él."` y `RedirectToAction(nameof(AgregarVeredas), new { id = paquete.Id })`. |
| `AgregarVeredas(int id)` | GET `/Paquetes/AgregarVeredas/5` | `ObtenerParaAgregarAsync`; `null` → `NotFound()`. |
| `AgregarVeredas(int id, List<int>? veredaIds)` | POST, `[ValidateAntiForgeryToken]` | Si la lista está vacía → vuelve a la misma vista con el error "Elegí al menos una vereda." (la lista se recarga del servidor). Si no, llama al servicio, arma los mensajes (sección 5) y redirige a `Details/{id}`. |
| `QuitarVereda(int id, int veredaId)` | POST `/Paquetes/QuitarVereda/5`, `[ValidateAntiForgeryToken]` | `QuitarVeredaAsync`; mensaje en `TempData` y redirige a `Details/{id}`. |
| `Delete` (GET) | — | Además de lo actual, carga la cantidad de veredas del paquete para mostrar el aviso (sección 5). |
| `DeleteConfirmed` | — | Igual que hoy (la FK pone `NULL`); agrega `TempData["Exito"]` y redirige a `Index`. |

`Edit` no cambia. Los textos en inglés del scaffolding de las vistas de Paquetes (`Details`, `Edit`, `Delete`, `Index`, "Back to List") se pasan a español en las vistas que esta spec toca.

### 4.3 `Controllers/VeredasController.cs` (parte B)

- `Index`: además de lo que defina SPEC-001, carga `ViewData["Paquetes"]` = `SelectList` de paquetes ordenados por `Fecha` descendente (texto: `"{Nombre} ({Fecha:dd/MM/yyyy})"`).
- Nueva acción `AgregarAPaquete(int? paqueteId, List<int>? veredaIds)`, POST, `[ValidateAntiForgeryToken]`: valida (sección 5), llama a `PaqueteService.AgregarVeredasAsync`, deja los mensajes en `TempData` y **vuelve a `Veredas/Index`** (el usuario sigue revisando el listado). El mensaje de éxito incluye un enlace al detalle del paquete.
- `Create`/`Edit`: el desplegable **Paquete** se mantiene (RN-18). Cambio mínimo en el `POST` de `Edit`: si la vereda ya tenía paquete y el formulario trae otro, se acepta (es una edición explícita de esa vereda, no una asignación masiva). No se agrega control de concurrencia acá.

### 4.4 Vistas

**Nuevo parcial `Views/Shared/_Mensajes.cshtml`**, renderizado en `_Layout.cshtml` justo antes de `@RenderBody()`. Muestra `TempData["Exito"]` (alert-success), `TempData["Aviso"]` (alert-warning) y `TempData["Error"]` (alert-danger), con botón de cerrar. Se codifica con `@` (no `Html.Raw`); si el mensaje lleva enlace, se usa `TempData["ExitoEnlace"]` + `TempData["ExitoEnlaceTexto"]` y el parcial arma el `<a>`. (Las vistas de `Access` tienen `Layout = null` y no se ven afectadas.)

**`Views/Paquetes/Details.cshtml`** (reescrita, `@model PaqueteDetalleViewModel`):

- Encabezado: nombre, fecha `dd/MM/yyyy`, proveedor, observación. Botones: **Agregar veredas** (primario), **Editar**, **Eliminar**, enlace "← Paquetes".
- Fila de 4 tarjetas: **Veredas** (cantidad), **m²** (`N2`), **m³** (`N3`), **Avance** (`nn %` + barra `progress`, "{Finalizadas} de {Cantidad} finalizadas"). Si `SinMedir > 0`: texto chico "{n} sin medir: no suman a los totales".
- Tabla de veredas, ordenada por calle y altura: Foto (mini), Dirección (enlace a `Veredas/Details`) + entre calles, Estado (badge), Prioridad, Medición ("Medida" + `x,xx m²` / `x,xxx m³`, o "Sin medir"), y botón **Quitar**.
- **Quitar** es un `<form method="post" asp-action="QuitarVereda" asp-route-id="@Model.Id">` con `veredaId` oculto y `onsubmit` de confirmación (texto en sección 5).
- Sin veredas: estado vacío con el texto de la sección 5 y el botón **Agregar veredas**.

**`Views/Paquetes/AgregarVeredas.cshtml`** (nueva, `@model AgregarVeredasViewModel`):

- Título: "Agregar veredas a «{PaqueteNombre}»". Enlace "← Volver al paquete".
- Barra de filtros (igual estilo que `Veredas/Index`): buscador por dirección / entre calles (`type="search"`), desplegable de **estado** (todos los valores de `Estado`) y desplegable **Medidas y sin medir / Medidas (n) / Sin medir (n)**.
- Tabla: casilla, Dirección + entre calles, Estado, Prioridad, Medición, Fecha de reclamo. Toda la fila es clicable para marcar la casilla (con `<label>` o JS).
- Encabezado de la columna de casillas: casilla **Seleccionar todas** (marca/desmarca **solo las filas visibles** con los filtros actuales; queda en estado indeterminado si hay algunas marcadas).
- Pie fijo (`sticky-bottom`): "{n} seleccionadas" y botón **Agregar al paquete** (deshabilitado con 0). Las casillas marcadas que quedan ocultas por un filtro **siguen marcadas** y se envían; el contador muestra el total.
- Todo en un único `<form method="post">` con `<input type="checkbox" name="veredaIds" value="@v.Id">` y el token (lo agrega el tag helper).
- Sin veredas disponibles: estado vacío (sección 5) con enlaces a **+ Nueva vereda** y a **Volver al paquete**.
- Filtrado y selección 100 % en el cliente (hasta 1.000 filas livianas; sin fotos en esta pantalla para cumplir RNF-14).

**`Views/Paquetes/Delete.cshtml`**: en español y con el aviso de la sección 5.

**`Views/Paquetes/Index.cshtml`**: textos en español; columna **Veredas** (cantidad) y botón **Agregar veredas** por fila. El `Index` del controlador agrega un `Select` con `Veredas.Count()` (proyección, sin traer las veredas).

**`Views/Veredas/Index.cshtml`** (parte B):

- Nueva primera columna con casilla. Las veredas **que ya tienen paquete** muestran la casilla deshabilitada con `title="Ya está en «{Paquete}». Quitala de ese paquete para moverla."`.
- Casilla **Seleccionar todas** en el encabezado (visibles y habilitadas).
- Barra de acción que aparece cuando hay al menos una marcada: "{n} seleccionadas", desplegable de paquetes (primera opción vacía "Elegí un paquete…") y botón **Agregar al paquete…**. Se envía por POST a `Veredas/AgregarAPaquete`.
- Si no hay paquetes creados, en lugar del desplegable: "Todavía no hay paquetes. [Creá uno]" (enlace a `Paquetes/Create`).
- El filtro existente (texto, estado, medición) sigue igual; se le suma el filtro **Con / sin paquete** solo si RF-VER-11 se confirma (fuera de alcance acá).

**`Views/Home/Paquetes.cshtml`**: solo se adapta al cálculo nuevo reutilizando `PaqueteService.CalcularTotales` (m² y avance). Agregar m³ a las tarjetas depende de **P-16** (fuera de alcance).

### 4.5 JavaScript `wwwroot/js/seleccion-veredas.js` (nuevo)

Un solo módulo para las dos pantallas (se incluye en `@section Scripts`):

- `filtrar()`: texto (`data-texto`), estado (`data-estado`) y medición (`data-medida="si|no"`); muestra "No hay veredas que coincidan con la búsqueda." si no queda ninguna visible. Reemplaza el script en línea de `Veredas/Index`.
- `seleccionarTodas(checked)`: solo filas visibles y casillas habilitadas.
- `actualizarContador()`: texto "{n} seleccionada(s)", habilita/deshabilita el botón y actualiza el estado indeterminado de **Seleccionar todas**.
- En `Veredas/Index`, antes de enviar, valida en el cliente que haya paquete elegido (el servidor valida igual).

## 5. Validaciones y mensajes exactos

Todas las validaciones se repiten en el servidor (RNF-07). `{n}` y `{k}` usan singular/plural.

| Situación | Dónde | Mensaje |
|---|---|---|
| Paquete creado | TempData Exito → AgregarVeredas | Se creó el paquete «{Nombre}». Ahora elegí las veredas que van en él. |
| POST sin veredas marcadas | Error en la vista / TempData Error | Elegí al menos una vereda. |
| Parte B: sin paquete elegido | TempData Error → Veredas/Index | Elegí el paquete al que querés agregar las veredas. |
| Paquete inexistente (se borró) | TempData Error | El paquete que elegiste ya no existe. Elegí otro. (desde Veredas/Index) · desde `Paquetes/AgregarVeredas` → 404 |
| Agregadas | TempData Exito | 1: Se agregó 1 vereda al paquete «{Nombre}». · n: Se agregaron {n} veredas al paquete «{Nombre}». (parte B + enlace "Ver paquete") |
| Salteadas por estar en otro paquete | TempData Aviso | 1: No se agregó {Dirección} porque ya está en el paquete «{Otro}». · k: No se agregaron {k} veredas porque ya están en otro paquete: {Dirección} («{Otro}»), {Dirección} («{Otro}»)… (se listan hasta 10 y después "y {r} más") |
| Ya estaban en este paquete | se suma al Aviso | 1: {Dirección} ya estaba en este paquete. · k: {k} veredas ya estaban en este paquete. |
| Veredas que ya no existen | se suma al Aviso | 1: Una de las veredas que elegiste ya no existe. · k: {k} de las veredas que elegiste ya no existen. |
| Ninguna agregada (todas salteadas) | solo el Aviso, sin Exito | — |
| Confirmación de Quitar (JS) | `confirm()` | ¿Quitar {Dirección} de este paquete? La vereda no se borra: queda sin paquete. |
| Quitada | TempData Exito | Se quitó {Dirección} del paquete. Ya podés asignarla a otro. |
| Quitar sobre una que ya no estaba | TempData Aviso | Esa vereda ya no estaba en este paquete. |
| Paquete sin veredas (Details) | estado vacío | Este paquete todavía no tiene veredas. Agregá las que van en él. |
| No hay veredas sin paquete (AgregarVeredas) | estado vacío | No hay veredas sin paquete. Podés cargar una nueva desde Veredas. |
| Filtro sin resultados | estado vacío | No hay veredas que coincidan con la búsqueda. |
| Delete (GET) | aviso en la confirmación | Este paquete tiene {n} veredas. No se borran: quedan sin paquete. (con 0: Este paquete no tiene veredas.) · Pregunta: ¿Seguro que querés eliminar el paquete «{Nombre}»? |
| Paquete eliminado | TempData Exito | Se eliminó el paquete «{Nombre}». Sus veredas quedaron sin paquete. |

## 6. Casos borde

1. **Dos usuarios asignan la misma vereda a paquetes distintos a la vez:** gana el primer `UPDATE`; el segundo la ve en "salteadas" con el nombre del otro paquete. Nunca queda en dos paquetes ni se pisa (RN-10).
2. **La vereda se asignó a este mismo paquete desde otra pestaña:** cuenta como "ya estaba"; no es error.
3. **La vereda se borró mientras la lista estaba abierta:** cuenta como "ya no existe".
4. **El paquete se borró mientras se asignaba:** desde el paquete, 404; desde el listado, mensaje de paquete inexistente. No se asigna nada.
5. **Quitar dos veces (doble clic o dos pestañas):** el segundo POST muestra "Esa vereda ya no estaba en este paquete."
6. **POST con ids manipulados** (repetidos, negativos, de otra vereda ya asignada): se ignoran o caen en salteadas/inexistentes; no hay error 500.
7. **Seleccionar todas con filtro activo:** marca solo las visibles; las marcadas antes que quedaron ocultas se mantienen.
8. **Paquete con 0 veredas:** totales en 0, avance `0 %`, sin división por cero.
9. **Veredas sin medir en el paquete:** suman 0 a m² y m³ y se informa cuántas son.
10. **Cambio de estado de una vereda** (desde su edición): el avance se recalcula al abrir el detalle (no se guarda ningún total del paquete).
11. **Vereda editada con otro paquete desde su formulario:** se permite (edición explícita, RN-18). La vereda sale del paquete anterior.
12. **Parte B, vereda con paquete marcada a mano en el HTML** (casilla deshabilitada forzada): el servidor la saltea con el aviso.
13. **Eliminar el paquete:** la FK `ON DELETE SET NULL` deja las veredas sin paquete (RN-11), y vuelven a aparecer en **Agregar veredas**.
14. **Muchas veredas** (1.000): la pantalla de agregar no carga fotos ni mapas; el POST acepta hasta 1.000 ids.

## 7. Pruebas automatizadas que acompañan (SPEC-004)

- Unitarias: `PaqueteService.CalcularTotales` — 4 veredas con 1 finalizada → 25 %; 0 veredas → 0 %; veredas sin medir suman 0; 1 de 3 → 33 %; 2 de 3 → 67 %.
- Integración (base de pruebas): asignar 3 veredas libres → las 3 quedan en el paquete; asignar una que ya está en otro paquete → se saltea y no cambia su `PaqueteId`; quitar → `PaqueteId = NULL`; eliminar paquete → veredas con `PaqueteId = NULL`; POST sin token → 400; POST sin sesión → redirección al login.
- Concurrencia: dos `AgregarVeredasAsync` con la misma vereda y paquetes distintos, con dos `DbContext` → la vereda queda en uno solo y el otro resultado la informa como salteada.

## 8. Fuera de alcance

- **P-12 (proveedor):** no se decide si el proveedor va en el paquete o en la vereda. `Paquete.ProveedorId` sigue obligatorio y `Vereda.ProveedorId` sigue existiendo. Crear un paquete sigue exigiendo que exista al menos un proveedor.
- **P-13 (qué veredas se pueden asignar):** se implementa la **propuesta** (cualquiera sin paquete, mostrando estado y si está medida). No se filtran por estado (p. ej., no se ocultan las "Finalizado" ni las "No corresponde"). Si la respuesta de P-13 cambia, se agrega un `Where` en `VeredasSinPaqueteAsync`.
- **P-14:** el paquete no tiene estado propio; solo el avance.
- Exportar el paquete (P-15), sector o barrio (P-10), filtro con / sin paquete en el listado (RF-VER-11), auditoría (P-17), m³ en el Home (P-16).
- Mover una vereda de un paquete a otro en un solo paso desde el paquete (hoy: quitar y agregar).
- Paginación del listado (RNF-14 se revisa aparte).

## 9. Preguntas abiertas para el PM

- **Q-01 (P-13):** ¿se aprueba la propuesta de mostrar **todas** las veredas sin paquete, sin importar el estado? En particular, ¿se ofrecen las "Finalizado", "No corresponde" y "No se encontró"?
- **Q-02 (P-14 / HU-11):** para el % de avance, ¿las veredas "No corresponde" y "No se encontró" cuentan en el total? La spec las cuenta (4 veredas con 1 finalizada = 25 %, sin excepciones). La alternativa es excluirlas del denominador.
- **Q-03 (RF-PAQ-08):** la parte B es *Debería*. ¿Entra en el sprint 1 o queda para después si falta tiempo?
- **Q-04 (P-12):** mientras siga abierta, el alta de paquete exige proveedor. ¿Está bien para el sprint 1?
- **Q-05 (RN-18):** ¿se mantiene que desde el formulario de la vereda se pueda cambiarla de un paquete a otro directamente (hoy se puede)? La spec lo mantiene.
