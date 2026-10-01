# SPEC-001 — Medición de roturas y tipos de suelo en la vereda

> Borrador v2 · 30/09/2026 · Autor: Desarrollador · Sprint 1
> v2: se reemplaza el esquema de «dos tipos de suelo» por roturas (pozos) sin límite, cada una con su tipo de suelo (D-26, D-29). La migración borra `Mediciones` sin convertir (D-27).

## 1. Objetivo y requisitos que cubre

La vereda guarda su medición como una fórmula de términos, por ejemplo `(2*3)+(5*9)`. Cada término es un **pozo** (rotura) y tiene su propio tipo de suelo, que es obligatorio. Cada rotura se guarda como una fila de la tabla nueva `Roturas`, con sus medidas, su tipo y su subtotal. La vereda guarda la fórmula, el total en m² y el cordón (opcional, en m³). Todos los totales los calcula el servidor con `MedicionService`, que tiene pruebas unitarias. `TipoSuelo` queda como catálogo de baldosas y se pueden agregar tipos rápido desde el formulario. El tipo de suelo no interviene en ningún cálculo.

| Tipo | IDs |
|---|---|
| Funcionales | RF-VER-01 (medición, tipos de suelo y cordón), RF-VER-05, RF-VER-06, RF-VER-07, RF-VER-08, RF-VER-09, RF-VER-10 (solo el filtro medidas / sin medir), RF-VER-16, RF-VER-17, RF-TSU-01 |
| Reglas de negocio | RN-04, RN-05, RN-06, RN-07, RN-08, RN-09, RN-13, RN-15, RN-17 |
| No funcionales | RNF-03, RNF-07, RNF-16, RNF-17, RNF-18 |
| Decisiones | D-04, D-05, D-06, D-15, D-17, D-18, D-19 (ajustada), D-26, D-27, D-29 |

Hay requisitos *A confirmar* que esta spec implementa con la propuesta vigente: RN-08, RN-13, RF-VER-17, RNF-07 y RNF-17. Si el PM los cambia, se ajusta la spec antes de programar.

En la UI la rotura se llama «Pozo», como en `Docs/01` sección 5. En el código se llama `Rotura`.

## 2. Modelo de datos

### 2.1 Tabla nueva `Roturas` (`Models/Rotura.cs`)

| Propiedad (C#) | Columna SQL | Nulable | Descripción |
|---|---|---|---|
| `Id` (`int`) | `int IDENTITY` PK | No | |
| `VeredaId` (`int`) | `int` | No | FK a `Veredas.Id`, **Cascade** (al borrar la vereda se borran sus roturas). |
| `Vereda` (`Vereda?`) | — | — | Navegación. |
| `Orden` (`int`) | `int` | No | Posición del término en la fórmula, desde 1. |
| `Medidas` (`string`) | `nvarchar(500)` | No | El término normalizado **sin paréntesis**, p. ej. `2*3` o `4,5*8`. |
| `TipoSueloId` (`int`) | `int` | No | FK a `TiposSuelo.Id`, **Restrict** (RN-13). |
| `TipoSuelo` (`TipoSuelo?`) | — | — | Navegación. |
| `SubtotalM2` (`decimal`) | `decimal(10,2)` | No | Producto de las medidas redondeado a 2 decimales. |

```csharp
public class Rotura
{
    public int Id { get; set; }

    public int VeredaId { get; set; }
    public Vereda? Vereda { get; set; }

    [Display(Name = "Pozo")]
    public int Orden { get; set; }

    [Required, StringLength(500)]
    public string Medidas { get; set; } = string.Empty;

    [Display(Name = "Tipo de suelo")]
    public int TipoSueloId { get; set; }
    public TipoSuelo? TipoSuelo { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Subtotal (m²)")]
    public decimal SubtotalM2 { get; set; }
}
```

Índices: `VeredaId` y `TipoSueloId`. No se crea el índice único `(VeredaId, Orden)`, para no tener conflictos de orden entre los borrados y las altas cuando se reemplazan las filas en un mismo `SaveChanges` (3.3). El orden lo asegura el servicio.

### 2.2 Tabla `Veredas` (columnas nuevas)

| Propiedad (C#) | Columna SQL | Nulable | Descripción |
|---|---|---|---|
| `Medicion` (`string?`) | `nvarchar(500)` | Sí | La fórmula que arma el servidor para mostrarla: `(2*3)+(5*9)+…`. |
| `TotalM2` (`decimal?`) | `decimal(10,2)` | Sí | Se guarda en la base. Es la suma de los `SubtotalM2` de sus roturas. |
| `TieneCordon` (`bool`) | `bit NOT NULL DEFAULT 0` | No | Casilla «Cordón». |
| `MedicionCordon` (`string?`) | `nvarchar(500)` | Sí | La fórmula del cordón, normalizada. |
| `TotalCordonM3` (`decimal?`) | `decimal(10,3)` | Sí | El total del cordón en m³. |
| `Roturas` (`ICollection<Rotura>`) | — | — | Colección. Se inicializa en `new List<Rotura>()`. |

```csharp
[StringLength(500, ErrorMessage = "La medición puede tener hasta 500 caracteres.")]
[Display(Name = "Medición")]           public string? Medicion { get; set; }

[Column(TypeName = "decimal(10,2)")]
[Display(Name = "Total (m²)")]         public decimal? TotalM2 { get; set; }

[Display(Name = "Cordón")]             public bool TieneCordon { get; set; }

[StringLength(500, ErrorMessage = "La medición puede tener hasta 500 caracteres.")]
[Display(Name = "Medición cordón")]    public string? MedicionCordon { get; set; }

[Column(TypeName = "decimal(10,3)")]
[Display(Name = "Total cordón (m³)")]  public decimal? TotalCordonM3 { get; set; }

public ICollection<Rotura> Roturas { get; set; } = new List<Rotura>();

[NotMapped]
public bool EstaMedida => TotalM2 != null || TotalCordonM3 != null;
```

Invariante: `Medicion == null` ⇔ la vereda no tiene roturas ⇔ `TotalM2 == null`. Cuando hay roturas, `TotalM2 = Σ SubtotalM2`.

> `EstaMedida` es `[NotMapped]` y no se traduce a SQL. En las consultas a la base hay que usar `v.TotalM2 != null || v.TotalCordonM3 != null`. `TotalM2` sí es una columna y se puede usar en `Where` y `Sum`.

Se eliminan de `Vereda`: la colección `Mediciones` y `SuperficieTotal`. La clase `Medicion` también se elimina (3.1): no puede quedar conviviendo con la propiedad `Vereda.Medicion`.

### 2.3 Tabla `TiposSuelo`

Las columnas no cambian (`Tipo`, `Medida`, `Color`, `Disponible`). En `TipoSuelo.cs`:

- `ICollection<Medicion> Mediciones` pasa a ser `ICollection<Rotura> Roturas`.
- Se agrega `[NotMapped] public string Descripcion => string.IsNullOrWhiteSpace(Medida) ? Tipo : $"{Tipo} ({Medida})";`. Se usa en los desplegables, en el detalle y en la respuesta del alta rápida.
- Mensajes: `[StringLength(100, ErrorMessage = "El tipo puede tener hasta 100 caracteres.")]` en `Tipo` y `[StringLength(20, ErrorMessage = "La medida puede tener hasta 20 caracteres.")]` en `Medida`. `Required` sigue con «El tipo es obligatorio.».

### 2.4 Relaciones (`Data/AppDbContext.cs`)

- Se eliminan `DbSet<Medicion> Mediciones` y las dos configuraciones de `Medicion`. Se agrega `DbSet<Rotura> Roturas`.
- Se agrega:

```csharp
// Vereda 1—N Rotura: al borrar una vereda se borran sus roturas.
modelBuilder.Entity<Rotura>()
    .HasOne(r => r.Vereda).WithMany(v => v.Roturas)
    .HasForeignKey(r => r.VeredaId)
    .OnDelete(DeleteBehavior.Cascade);

// TipoSuelo 1—N Rotura: no se puede borrar un tipo de suelo en uso (RN-13).
modelBuilder.Entity<Rotura>()
    .HasOne(r => r.TipoSuelo).WithMany(t => t.Roturas)
    .HasForeignKey(r => r.TipoSueloId)
    .OnDelete(DeleteBehavior.Restrict);
```

### 2.5 Migración `RoturasEnVeredas`

Se genera con `dotnet ef migrations add RoturasEnVeredas`:

1. `CreateTable("Roturas")` con las columnas de 2.1, los índices `IX_Roturas_VeredaId` e `IX_Roturas_TipoSueloId`, la FK a `Veredas` con `ReferentialAction.Cascade` y la FK a `TiposSuelo` con `ReferentialAction.Restrict`.
2. `AddColumn` en `Veredas` de `Medicion`, `TotalM2`, `TieneCordon` (`defaultValue: false`), `MedicionCordon` y `TotalCordonM3`.
3. `DropTable("Mediciones")`, **sin convertir datos**: son de prueba (D-27, respuesta a P-06).

`Down`: borra `Roturas` y las columnas nuevas y vuelve a crear `Mediciones` vacía, con su estructura anterior (índices y FK). No recupera datos.

`AppDbContextModelSnapshot.cs` y el `.Designer.cs` se actualizan solos. Las migraciones anteriores no se tocan. Antes de publicar hay que hacer una copia de la base publicada y probar la migración en `GestionVeredas_Pruebas` (D-28, R-05). La migración se aplica sola al arrancar el sitio.

## 3. Cambios por archivo

### 3.1 Se eliminan

| Archivo | Motivo |
|---|---|
| `Models/Medicion.cs` | La reemplaza `Rotura` (D-29). |
| `Controllers/MedicionesController.cs` | Idem. |
| `Views/Mediciones/` (Create, Delete, Details, Edit, Index) | Idem. El menú (`_Layout.cshtml`) ya no tiene enlace a Mediciones, así que no se toca. |
| `Models/ViewModels/MedicionItemViewModel.cs` | Lo reemplazan el servicio y `RoturaFilaViewModel`. |
| `Views/Shared/_MedicionesForm.cshtml` | Lo reemplaza `Views/Veredas/_Medicion.cshtml`. |
| `wwwroot/js/mediciones-form.js` | Lo reemplaza `wwwroot/js/medicion-vereda.js`. |

### 3.2 `Services/MedicionService.cs` (nuevo)

Es una clase **estática** y sin dependencias, así que no se registra en `Program.cs`. Tiene toda la lógica de cálculo y validación. El controlador solo consulta la base y pasa los errores a `ModelState`.

```csharp
namespace SistemaVeredas.Services
{
    public sealed record TerminoMedicion(int Orden, string Medidas, decimal Subtotal);

    public sealed record ResultadoMedicion(
        decimal? Total,
        string? TextoNormalizado,
        IReadOnlyList<TerminoMedicion> Terminos,
        IReadOnlyList<string> Errores)
    {
        public bool EsValido => Errores.Count == 0;
        public bool EstaVacia => EsValido && Total is null;
    }

    public sealed record ErrorMedicion(string Campo, string Mensaje);

    public static class MedicionService
    {
        public const decimal MedidaMaxima = 9999.99m;

        // medidasPorTermino: 2 (m², 2 decimales) o 3 (m³, 3 decimales). Otro valor → ArgumentOutOfRangeException.
        public static ResultadoMedicion Calcular(string? terminos, int medidasPorTermino);

        // Calcula la medición y el cordón, arma vereda.Roturas y valida los tipos por pozo (4.2).
        // No consulta la base: la existencia de los tipos la controla el controlador.
        public static IReadOnlyList<ErrorMedicion> AplicarAVereda(Vereda vereda, IReadOnlyList<int?> tiposRotura);

        // "51,25 m²" / "0,225 m³" con cultura es-AR (no depende de la cultura del servidor). null → null.
        public static string? FormatearM2(decimal? valor);
        public static string? FormatearM3(decimal? valor);
    }
}
```

#### Algoritmo de `Calcular`

1. Si el texto es `null`, vacío o solo espacios, devuelve `Total = null`, `TextoNormalizado = null`, `Terminos` vacío y ningún error. Es una vereda sin medir (RF-VER-09).
2. Parte el texto original por `+` en **términos** y los numera desde 1 en el orden en que aparecen. Los términos vacíos también cuentan.
3. Revisa cada término en este orden. Anota **el primer error que encuentra** en ese término y pasa al siguiente, así se informan todos los términos con error (RNF-03):
   1. Si recortado queda vacío → *término vacío*.
   2. Si tiene un espacio entre dos cifras (regex `[\d.,]\s+[\d.,]`) → *espacio dentro de una medida*. Así `4 5` no se lee como `45`.
   3. Quita los espacios y cambia `x` y `X` por `*`.
   4. Si tiene un carácter que no sea `0-9 , . * ( )` → *carácter no permitido*. Incluye `-`, `/` y letras.
   5. Solo se permite **un par de paréntesis que envuelva todo el término**: `(2*3)`. Cualquier otro uso (`(2*3`, `2*(3)`, `((2*3))`, `(2)*(3)`) → *paréntesis*. Después los quita.
   6. Parte el término por `*` en **medidas**. Si alguna queda vacía (`2**3`, `*2*3`) → *medida vacía*.
   7. Cada medida tiene que cumplir `^\d+([.,]\d+)?$` o `^[.,]\d+$`. Si no (`1.234,5`, `2,`, `2.5.3`) → *número inválido*. Para convertirla cambia `,` por `.` y usa `decimal.Parse(texto, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture)`. **Nunca** con la cultura actual (con es-AR, `4.5` se leería 45), ni con `eval` ni con `DataTable.Compute`.
   8. Si una medida es 0 → *medida en cero*. Si pasa `MedidaMaxima` → *medida muy grande*.
   9. Si la cantidad de medidas no es `medidasPorTermino` → *cantidad de medidas*.
4. Si hubo errores, devuelve `Total = null`, `TextoNormalizado = null`, `Terminos` vacío y los mensajes.
5. Si no hubo errores, el subtotal de cada término es el producto de sus medidas (`decimal`), redondeado a `d` decimales con `Math.Round(x, d, MidpointRounding.AwayFromZero)` (`d` es 2 en m² y 3 en m³). **El total es la suma de los subtotales redondeados.** Así el total de la vereda siempre coincide con la suma de sus pozos que se ve en el detalle (ver Q3).
6. Si el total pasa el máximo de la columna (99.999.999,99 m² o 9.999.999,999 m³) → *total muy grande*. Si hay `OverflowException` se informa con el mismo error.
7. `TerminoMedicion.Medidas` es el término sin espacios, con `x` cambiada por `*`, sin paréntesis y con el separador decimal **como lo escribió el usuario**: `4,5*8`.
8. `TextoNormalizado` es `"(" + Medidas + ")"` de cada término, unido con `+`. Por ejemplo, `2 x 3 + 5,5X2` da `(2*3)+(5,5*2)`. Es la fórmula que se guarda en `Veredas.Medicion` y en `MedicionCordon`.

#### Algoritmo de `AplicarAVereda`

1. Si `!vereda.TieneCordon`, pone `MedicionCordon` y `TotalCordonM3` en `null`. Si no, calcula el cordón con 3 medidas. Si es válido, asigna el texto normalizado y el total (si está vacío, `null`). Cada error va como `ErrorMedicion("MedicionCordon", mensaje)`.
2. Calcula `vereda.Medicion` con 2 medidas. Cada error va como `ErrorMedicion("Medicion", mensaje)`. Si hay errores, no sigue con los pasos 3 y 4.
3. Si la medición está vacía, pone `Medicion` y `TotalM2` en `null`, deja `Roturas` vacía e ignora `tiposRotura`.
4. Si la medición es válida y tiene `n` términos:
   - Si `tiposRotura.Count != n` → error en `"Medicion"` (*pozos y tipos no coinciden*, 4.2), sin revisar más.
   - Si algún `tiposRotura[i]` es `null` → error en `$"tiposRotura[{i}]"` (*pozo sin tipo*).
   - Si no hay errores: `vereda.Medicion = TextoNormalizado`, `vereda.TotalM2 = Total` y `vereda.Roturas` se reemplaza por `n` nuevas `Rotura { Orden, Medidas, TipoSueloId = tiposRotura[i]!.Value, SubtotalM2 = Subtotal }`, todas con `Id = 0`.
5. **Todo lo que manda el navegador como total o subtotal se ignora.** No hay campos para eso.

### 3.3 Controladores

**`VeredasController`**

- Se quitan `ValidarMediciones`, `GuardarMedicionesAsync`, los `Include(v => v.Mediciones)` y el parámetro `mediciones`.
- `[Bind]` de Create y Edit agrega `Medicion,TieneCordon,MedicionCordon`. **No** se bindean `TotalM2`, `TotalCordonM3` ni `Roturas` (RNF-07).
- Create y Edit (POST) reciben además `List<int?>? tiposRotura`. Son los desplegables de los pozos, `name="tiposRotura[i]"` con `i` desde 0, en el mismo orden que los términos. Antes de `ModelState.IsValid`:

```csharp
var tipos = tiposRotura ?? new List<int?>();
foreach (var e in MedicionService.AplicarAVereda(vereda, tipos))
    ModelState.AddModelError(e.Campo, e.Mensaje);
await ValidarTiposSueloAsync(vereda); // cada TipoSueloId de vereda.Roturas existe en TiposSuelo (mensaje en 4.2)
```

- En **Create**, `_context.Add(vereda)` también guarda las `Roturas` nuevas.
- En **Edit**, se reemplazan las filas de la vereda en un solo `SaveChanges`:

```csharp
var actuales = await _context.Roturas.Where(r => r.VeredaId == vereda.Id).ToListAsync();
_context.Roturas.RemoveRange(actuales);
foreach (var r in vereda.Roturas) r.VeredaId = vereda.Id;
_context.Update(vereda);          // las Roturas con Id = 0 quedan como nuevas
await _context.SaveChangesAsync();
```

- `CargarListas(Vereda? vereda, List<RoturaFilaViewModel> filas)`:
  - `ViewData["TiposSuelo"]`: `List<SelectListItem>` con los tipos `Disponible` más los que usan las filas aunque no estén disponibles. Se ordenan por `Tipo` y `Medida` y el texto es `Descripcion`. A los no disponibles se les agrega « (no disponible)».
  - `ViewData["Roturas"]`: las filas para mostrar el formulario. En Create GET va vacío. En Edit GET salen de `vereda.Roturas` ordenadas por `Orden`. Si un POST vuelve con errores, salen de los términos de `Calcular` (cuando la fórmula es válida) con el tipo recibido en la misma posición, o solo de los tipos recibidos cuando la fórmula tiene errores (el JS vuelve a armar los renglones al cargar).
- `Edit` GET: `Include(v => v.Roturas)`.
- `Index`: sin `Include` de roturas, porque `TotalM2` está en la vereda.
- `Details`: `Include(v => v.Roturas).ThenInclude(r => r.TipoSuelo)`.
- `DeleteConfirmed`: sin cambios. Las roturas se borran en cascada.

**`TipoSuelosController`**

- Acción nueva `CrearRapido`, para el alta rápida (RF-VER-06):

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CrearRapido([Bind("Tipo,Medida")] TipoSuelo tipoSuelo)
// 200 { id, texto }                        → se creó (Disponible = true)
// 200 { id, texto, existente = true }      → ya había uno disponible con el mismo Tipo y Medida
// 400 { errores = ["...", ...] }           → error de validación (mensajes de 2.3 y 4.3)
```

  - Recorta los espacios de `Tipo` y `Medida`. Si `Medida` queda vacía, pasa a `null`.
  - Duplicado = mismo `Tipo` y misma `Medida`, sin distinguir mayúsculas. Si existe y está disponible, devuelve ese con `existente = true` y no crea otro. Si existe y **no** está disponible, devuelve 400 con el mensaje de 4.3.
  - El modal no pide `Color`: queda en `null`.
- `Delete` (GET): `var enUso = await _context.Roturas.Where(r => r.TipoSueloId == id).Select(r => r.VeredaId).Distinct().CountAsync();` y se pasa en `ViewData["VeredasQueLoUsan"]`.
- `DeleteConfirmed` (RN-13, propuesta vigente): vuelve a contar. Si `enUso > 0`, **no borra**: pone `Disponible = false`, guarda y redirige a `Index` con `TempData["Mensaje"]` (texto en 4.3). Si `enUso == 0`, borra como hoy.
- `Index`: si hay `TempData["Mensaje"]`, lo muestra como alerta.

**`HomeController`**

- `Index`: `SuperficieTotal = await _context.Veredas.SumAsync(v => v.TotalM2 ?? 0)` y el nuevo `CordonTotalM3 = await _context.Veredas.SumAsync(v => v.TotalCordonM3 ?? 0)` (propuesta de P-16). Se quita la consulta a `Mediciones`.
- `Veredas`: `Include(v => v.Roturas).ThenInclude(r => r.TipoSuelo)`, en lugar de `Mediciones`.
- `Paquetes`: `Include(p => p.Veredas)` sin el `ThenInclude`.

### 3.4 ViewModels

- `HomeViewModel`: se agrega `public decimal CordonTotalM3 { get; set; }`. `SuperficieTotal` sigue en m².
- `Models/ViewModels/RoturaFilaViewModel.cs` (nuevo), para dibujar los renglones desde el servidor:

```csharp
public class RoturaFilaViewModel
{
    public int Orden { get; set; }            // 1, 2, 3…
    public string? Medidas { get; set; }      // "2*3"; null si la fórmula tiene errores
    public decimal? SubtotalM2 { get; set; }
    public int? TipoSueloId { get; set; }
}
```

- En esta spec no se crea un ViewModel para todo el formulario de vereda: hoy Create y Edit bindean la entidad (ver Fuera de alcance).

### 3.5 Vistas

**`Views/Veredas/_Medicion.cshtml` (parcial nuevo del formulario, `@model Vereda`)**

Reemplaza a `<partial name="_MedicionesForm" />` en `Create.cshtml` y `Edit.cshtml`. Estructura y etiquetas exactas:

```
Medición  [input asp-for=Medicion, placeholder "(2*3)+(5*9)", maxlength 500]
          <span asp-validation-for=Medicion> + errores en vivo

<div class="roturas">   (un renglón por término)
  Pozo 1  2 × 3      = 6,00 m²   [select name="tiposRotura[0]" "-- Elegí el tipo de suelo --"] [+ Nuevo]
  Pozo 2  5 × 9      = 45,00 m²  [select name="tiposRotura[1]"]                                [+ Nuevo]
</div>
Total     <output> "51,00 m²" | "—"

[ ] Cordón   (checkbox asp-for=TieneCordon)
  Medición cordón  [input asp-for=MedicionCordon, placeholder "(5*0,15*0,30)"]
  Total cordón     <output> "0,225 m³"
```

- Ayuda debajo de «Medición»: «Escribí cada pozo como largo × ancho en metros y sumalos: (2*3)+(5*9). Podés usar coma o punto decimal y «x» en lugar de «*». Después elegí el tipo de suelo de cada pozo.». Debajo del cordón: «Cada rotura lleva largo × ancho × alto, en metros.».
- Las medidas del renglón se muestran con « × » en lugar de `*`. Cada renglón tiene su `<span class="text-danger" data-valmsg-for="tiposRotura[i]">` para el error del servidor.
- El servidor dibuja los renglones de `ViewData["Roturas"]` con el tipo ya elegido. Después el JS los vuelve a armar a partir de la fórmula y conserva el tipo elegido en cada posición.
- El bloque del cordón se ve o no según `Model.TieneCordon` (clase `d-none`).
- Cada botón «+ Nuevo» lleva `data-destino="<índice>"`.

**`Views/Veredas/_TipoSueloRapido.cshtml` (modal Bootstrap nuevo)**

- Se incluye en `Create.cshtml` y `Edit.cshtml` **fuera** del `<form>` principal, porque no se pueden anidar formularios.
- Título «Nuevo tipo de suelo». Campos «Nombre» (`Tipo`, obligatorio, maxlength 100) y «Medida de la baldosa» (`Medida`, placeholder «40x40», maxlength 20). Botones «Cancelar» y «Agregar». Tiene una zona de errores `text-danger`.

**`Create.cshtml` / `Edit.cshtml`**

- Cambian el parcial, incluyen el modal y en `@section Scripts` agregan `<script src="~/js/medicion-vereda.js" asp-append-version="true">`.

**`Views/Veredas/Index.cshtml` (RF-VER-10, RN-09)**

- `cantMedidas = Model.Count(v => v.EstaMedida)` y `data-medida="@(item.EstaMedida ? "si" : "no")"`. El filtro JS no cambia.
- Columna «Medición»: si `EstaMedida`, muestra la insignia «Medida» y debajo `FormatearM2(item.TotalM2)` y, si hay, `FormatearM3(item.TotalCordonM3)`, separados por « · ». Si no, la insignia «Sin medir». Se quitan los m lineales.

**`Views/Veredas/Details.cshtml` (RF-VER-17)**

- Nueva sección «Medición» en el panel lateral, después de «Reparación». Se quita la fila «Superficie medida».
  - Si `!EstaMedida`: fila «Estado de medición» → «Sin medir» (clase `vacio`).
  - Si no, una fila por rotura, ordenadas por `Orden`: «Pozo {n}» a la izquierda. A la derecha, `SubtotalM2` formateado y debajo, en chico y gris, `2 × 3 · {TipoSuelo.Descripcion}`. Después, la fila «Total m²» → `FormatearM2(TotalM2)`, en negrita, o «Sin medir» si es `null`. Si `TieneCordon`, la fila «Cordón» → `FormatearM3(TotalCordonM3)` o «Sin medir», con `MedicionCordon` debajo.
- La foto sigue siendo la protagonista.

**`Views/Home/Index.cshtml`**

- Subtítulo: `veredas cargadas · {FormatearM2(SuperficieTotal)} medidos`, más ` · {FormatearM3(CordonTotalM3)} de cordón` solo si `CordonTotalM3 > 0`.

**`Views/Home/Veredas.cshtml`**

- «Superficie»: si `EstaMedida`, `FormatearM2(v.TotalM2)` (más los m³, si hay) y `(n pozos)` con `n = v.Roturas.Count` («1 pozo» en singular). Si no, «Sin medir».
- Insignias: los tipos distintos de `v.Roturas` (`r.TipoSuelo!.Tipo`).

**`Views/Home/Paquetes.cshtml`**

- `superficie = p.Veredas.Sum(v => v.TotalM2 ?? 0)`, con formato es-AR `N2`. El mini-stat ya dice «m²». Los m³ del paquete quedan para SPEC-002 (RF-PAQ-04).

**`Views/Paquetes/*`**: esta spec no los cambia (no usan mediciones). SPEC-002 va a usar `TotalM2` y `TotalCordonM3`.

**`Views/TipoSuelos/Delete.cshtml`**

- Si `VeredasQueLoUsan > 0`, muestra una alerta con el texto de 4.3 y el botón pasa a decir «Marcar como no disponible» (hace el mismo POST).

### 3.6 JavaScript: `wwwroot/js/medicion-vereda.js` (nuevo)

1. **Renglones en vivo (RNF-03, RF-VER-16):** en cada `input` de «Medición», aplica **las mismas reglas y los mismos mensajes** que `Calcular` (4.1) y vuelve a armar un renglón por término:
   - término válido: «Pozo n», las medidas con « × », «= subtotal m²» y el desplegable;
   - término con error: «Pozo n», el mensaje en rojo y el desplegable, para que la cantidad de desplegables siga igual a la cantidad de términos.
   - Si la fórmula queda vacía, no se muestra ningún renglón.
   - **El tipo elegido se conserva por posición.** Al volver a armar, el renglón `i` mantiene el valor que tenía el desplegable `i`. Los renglones nuevos empiezan sin elegir y los que sobran se quitan.
   - Los `name` se renumeran en orden (`tiposRotura[0..n-1]`).
2. **Total:** es la suma de los subtotales ya redondeados, igual que el servidor. Se muestra con `toLocaleString('es-AR', { minimumFractionDigits: d, maximumFractionDigits: d })` más « m²» o « m³». Si está vacío o hay errores, muestra «—». Para redondear usa `Number(Math.round(Number(x + 'e' + d)) + 'e-' + d)`. Sin `eval` ni `Function`. Es solo una vista previa: lo que se guarda es lo que calcula el servidor.
3. **Cordón:** la casilla muestra u oculta el bloque. Al ocultarlo **no se borra** el valor. El servidor lo descarta si la casilla llega sin marcar. El cordón se calcula en vivo igual, con 3 medidas y sin renglones.
4. **Bloqueo del envío:** en `submit`, si hay errores en la medición, algún pozo sin tipo o errores en el cordón (solo si está visible), cancela el envío. Muestra «Revisá la medición y los tipos de suelo marcados en rojo antes de guardar.», marca en rojo los pozos sin tipo («Elegí el tipo de suelo del pozo {n}.») y pone el foco en el primero. Esto es para no perder las fotos elegidas si el servidor rechaza el formulario. El servidor valida igual.
5. **Alta rápida (RF-VER-06):**
   - «+ Nuevo» abre el modal y recuerda el índice del renglón.
   - «Agregar» hace `fetch('/TipoSuelos/CrearRapido', { method: 'POST', body: FormData(Tipo, Medida), headers: { 'RequestVerificationToken': <__RequestVerificationToken del formulario principal> } })`. `ValidateAntiForgeryToken` lee ese encabezado por defecto.
   - Si responde 200: agrega `<option value=id>texto</option>` a **todos** los desplegables de pozos, en orden alfabético (`localeCompare(..., 'es')`) y solo si no existía. También la agrega a la lista que usa como plantilla para los renglones nuevos. La deja **elegida solo en el renglón que la pidió**. Cierra el modal y limpia sus campos. Si viene `existente = true`, además muestra en el renglón «Ese tipo de suelo ya estaba en la lista: quedó elegido.».
   - Si responde 400: muestra `errores` en el modal, sin cerrarlo.
   - Si la respuesta es una redirección al login (`response.redirected` o un `Content-Type` que no es JSON) u otro error, muestra los mensajes de 4.3. Nunca recarga la página ni toca el resto del formulario.
   - Mientras dura el `fetch`, «Agregar» queda deshabilitado.
6. Arranca en `DOMContentLoaded` y no usa jQuery.

### 3.7 `Program.cs`

Sin cambios.

## 4. Validaciones y mensajes exactos

`{n}` es el número del término o del pozo, desde 1. `{t}` es el término como lo escribió el usuario, recortado. `{k}` son las medidas encontradas y `{e}` las esperadas. «medida» o «medidas» concuerda con el número.

### 4.1 Mensajes de `Calcular` (iguales en el servidor y en el JS)

| Error | Mensaje |
|---|---|
| Término vacío | `El término {n} está vacío: revisá que no haya dos «+» seguidos ni un «+» al principio o al final.` |
| Espacio dentro de una medida | `El término {n} («{t}») tiene un espacio dentro de una medida. Separá las medidas con «*».` |
| Carácter no permitido | `El término {n} («{t}») tiene un carácter que no se permite: «{c}». Usá solo números, «+», «*» (o «x») y paréntesis.` |
| Paréntesis | `El término {n} («{t}») tiene los paréntesis mal puestos. Cada término puede ir entre paréntesis, así: (2*3).` |
| Medida vacía | `El término {n} («{t}») tiene una medida vacía: revisá que no haya dos «*» seguidos ni un «*» al principio o al final.` |
| Número inválido | `El término {n} («{t}») tiene una medida que no es un número válido: «{m}». Usá la coma o el punto solo para los decimales (2,5 o 2.5).` |
| Medida en cero | `El término {n} («{t}») tiene una medida en cero. Las medidas tienen que ser mayores a 0.` |
| Medida muy grande | `El término {n} («{t}») tiene una medida mayor a 9.999,99 m.` |
| Cantidad de medidas | `El término {n} («{t}») tiene {k} medida(s) y lleva {e}.` Por ejemplo: `El término 2 («59») tiene 1 medida y lleva 2.` |
| Total muy grande | `El total es demasiado grande. Revisá la medición.` |

### 4.2 Pozos y tipos de suelo

| Regla | Campo | Mensaje |
|---|---|---|
| Largo máximo | `Medicion`, `MedicionCordon` | `La medición puede tener hasta 500 caracteres.` |
| Pozo sin tipo de suelo (D-26: obligatorio) | `tiposRotura[i]` | `Elegí el tipo de suelo del pozo {n}.` |
| Cantidad de tipos ≠ cantidad de términos | `Medicion` | `La cantidad de pozos no coincide con los tipos de suelo elegidos. Revisá la medición y volvé a elegir los tipos.` |
| El tipo de un pozo no existe en `TiposSuelo` (controlador) | `tiposRotura[i]` | `El tipo de suelo del pozo {n} no existe. Elegí otro de la lista.` |

Es **válido**, sin error: que varios pozos tengan el mismo tipo; la casilla «Cordón» marcada sin medición (`TieneCordon = true` y total `null`); la vereda sin nada medido (RF-VER-09). Si no hay medición, los tipos que se hayan mandado se ignoran.

### 4.3 Alta rápida y catálogo

| Situación | Mensaje |
|---|---|
| Nombre vacío | `El tipo es obligatorio.` |
| Nombre de más de 100 caracteres | `El tipo puede tener hasta 100 caracteres.` |
| Medida de más de 20 caracteres | `La medida puede tener hasta 20 caracteres.` |
| Ya existe y está disponible (200, `existente`) | `Ese tipo de suelo ya estaba en la lista: quedó elegido.` |
| Ya existe y no está disponible (400) | `«{Descripcion}» ya existe pero está marcado como no disponible. Activalo desde Tipos de suelo.` |
| Sesión vencida (JS) | `No se pudo agregar el tipo de suelo porque se venció la sesión. Iniciá sesión en otra pestaña y volvé a probar; lo que cargaste acá no se pierde.` |
| Otro error (JS) | `No se pudo agregar el tipo de suelo. Probá de nuevo.` |
| Eliminar un tipo en uso: aviso en Delete (GET) | `Este tipo de suelo lo usan {n} vereda(s), así que no se puede eliminar. Si ya no se consigue, marcalo como no disponible.` |
| Eliminar un tipo en uso: resultado (POST) | `«{Descripcion}» lo usan {n} vereda(s): no se eliminó y quedó como no disponible.` |

## 5. Ejemplos de cálculo y casos borde

### 5.1 Pruebas unitarias de `Calcular` (RNF-16; las implementa SPEC-004)

| # | Entrada | Medidas | Subtotales | Total | Normalizado / error |
|---|---|---|---|---|---|
| 1 | `null`, `""`, `"   "` | 2 | — | `null` | `null`, sin errores (vacía) |
| 2 | `3*2` | 2 | 6,00 | 6,00 | `(3*2)` |
| 3 | `(2*3)+(5*9)` | 2 | 6,00 · 45,00 | 51,00 | `(2*3)+(5*9)` |
| 4 | `(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)` | 2 | 6,00 · 45,00 · 36,00 · 9,60 · 33,75 | **130,35** | igual a la entrada; `Medidas` del término 3 = `4.5*8` |
| 5 | `(2x3)+(5X9)+(4.5x8)+(2,4x4)+(3,75x9)` | 2 | igual que 4 | 130,35 | `(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)` |
| 6 | `(2*3)+(5*9)+(0,5*0,5)` | 2 | 6,00 · 45,00 · 0,25 | 51,25 | ejemplo de `Docs/01` |
| 7 | `2 x 3 + 5 * 9` | 2 | 6,00 · 45,00 | 51,00 | `(2*3)+(5*9)` |
| 8 | `(5*0,15*0,30)` | 3 | 0,225 | **0,225** | `(5*0,15*0,30)` |
| 9 | `4.5*2` | 2 | 9,00 | 9,00 (no 90: se lee con InvariantCulture) | `(4.5*2)` |
| 10 | `0,005*1` | 2 | 0,01 | 0,01 (en el punto medio se aleja de cero) | — |
| 11 | `0,005*1+0,005*1` | 2 | 0,01 · 0,01 | 0,02 (suma de subtotales redondeados; ver Q3) | — |
| 12 | `(2*3)+59` | 2 | — | `null` | `El término 2 («59») tiene 1 medida y lleva 2.` |
| 13 | `(5*0,15)` | 3 | — | `null` | `El término 1 («(5*0,15)») tiene 2 medidas y lleva 3.` |
| 14 | `2*3*4` | 2 | — | `null` | término 1: 3 medidas y lleva 2 |
| 15 | `2-3`, `2/3`, `2*a` | 2 | — | `null` | carácter no permitido `-` / `/` / `a` |
| 16 | `2*3++4*5`, `+2*3`, `(2*3)+` | 2 | — | `null` | término vacío (el 2, el 1 y el 2) |
| 17 | `(2*3`, `2*(3)`, `((2*3))`, `(2+3)*4` | 2 | — | `null` | paréntesis (en `(2+3)*4` fallan los términos 1 y 2) |
| 18 | `2**3` | 2 | — | `null` | medida vacía |
| 19 | `1.234,5*2`, `2,*3`, `2.5.3*1` | 2 | — | `null` | número inválido |
| 20 | `0*5` | 2 | — | `null` | medida en cero |
| 21 | `10000*1` | 2 | — | `null` | medida muy grande |
| 22 | `4 5*2` | 2 | — | `null` | espacio dentro de una medida |
| 23 | `59+2*3+7` | 2 | — | `null` | dos errores: términos 1 y 3 |
| 24 | cualquier texto | 4 | — | — | `ArgumentOutOfRangeException` |

### 5.2 Casos de `AplicarAVereda`

| Caso | Resultado |
|---|---|
| `(2*3)+(5*9)+(0,5*0,5)` con tipos `[Vainilla, Cemento, Vainilla]` | 3 roturas (Orden 1–3, subtotales 6,00 / 45,00 / 0,25), `TotalM2` 51,25 y `Medicion` `(2*3)+(5*9)+(0,5*0,5)`. Se permite repetir el tipo. |
| La misma fórmula con tipos `[Vainilla, null, Vainilla]` | Error `Elegí el tipo de suelo del pozo 2.` en `tiposRotura[1]`. |
| La misma fórmula con 2 tipos | Error en `Medicion`: los pozos no coinciden con los tipos. |
| Fórmula vacía con tipos mandados | Se guarda sin medir: sin roturas y con `TotalM2` en `null`. |
| Fórmula con errores | Solo los errores de `Calcular`. No se revisan los tipos. |
| La misma fórmula con otros tipos de suelo | Los mismos subtotales y el mismo total (RN-04, RN-05). |
| El navegador manda `TotalM2=999` | Se ignora porque no se bindea. |
| «Cordón» sin marcar y con medición cargada | `TieneCordon = false` y `MedicionCordon` y `TotalCordonM3` en `null`. |
| Medición `(2*3)+(5*9)` y cordón `(5*0,15*0,30)` | 51,00 m² y 0,225 m³. La vereda figura «Medida». |
| Solo cordón medido | `EstaMedida = true` y `TotalM2` en `null`. |
| Editar una vereda con 3 pozos y dejar 2 | Se borran las 3 filas anteriores y se crean 2 nuevas. |

### 5.3 Otros casos borde

- **Si se inserta un término en el medio de la fórmula, los tipos se corren una posición**, porque se conservan por posición. El renglón nuevo queda sin tipo, así que el envío se bloquea hasta completarlo, pero los que siguen quedan con el tipo del pozo anterior. La ayuda del formulario no lo advierte; está en Q4.
- Un tipo que después se marcó como no disponible sigue apareciendo en los pozos que lo usan, con « (no disponible)». Para pozos nuevos no se ofrece.
- Alta rápida de un tipo que ya está en los desplegables: la opción no se duplica.
- Si el servidor rechaza otro campo (por ejemplo, la calle vacía), la fórmula vuelve como se escribió, con los tipos en su posición y la casilla del cordón como estaba. Las fotos elegidas se pierden, como pasa hoy (fuera de alcance).
- Si se borra un tipo de suelo directo en la base, la FK Restrict lo impide.
- Borrar una vereda borra sus roturas (cascade).
- Estado «Falta medir» con la medición cargada: esta spec no lo controla (P-08).

## 6. Fuera de alcance

- ViewModel para todo el formulario de Create y Edit de vereda.
- Filtros por prioridad, tipo de suelo y paquete (RF-VER-11). El filtro por tipo de suelo va a tener que consultar `Roturas`.
- Totales, m³ y avance en el detalle del paquete (RF-PAQ-04, SPEC-002).
- Índice único en `TiposSuelo (Tipo, Medida)` y el cálculo de baldosas a partir de la medida (P-05).
- Aviso cuando el estado no coincide con la medición (P-08).
- Conservar las fotos elegidas cuando el servidor rechaza el formulario.
- Localización general de fechas y números (RNF-01). Acá solo se formatean los totales de medición con es-AR.
- El proyecto de pruebas (SPEC-004). Acá solo se listan los casos.

## 7. Preguntas abiertas para el PM

Ya respondidas y aplicadas en esta versión: P-06 (datos de prueba, se borran: D-27), medición sin tipo de suelo (no se permite: D-26), dos pozos con el mismo tipo (sí: D-26).

| ID | Pregunta | Propuesta |
|---|---|---|
| Q1 | RN-13: si se intenta eliminar un tipo en uso, ¿se marca solo como no disponible o solo se avisa? | Se marca como no disponible y se avisa (3.3). |
| Q2 | En el catálogo puede haber un tipo «Cordón», de cuando el cordón se cargaba como tipo de suelo. ¿Se marca como no disponible? | Sí, a mano después de migrar. La migración no lo toca. |
| Q3 | ¿El total es la suma de los subtotales redondeados o el redondeo de la suma exacta? Solo difieren en algún centavo en casos raros (ejemplo 11). | La suma de los subtotales redondeados, así el detalle cierra. |
| Q4 | Al insertar un término en el medio de la fórmula, los tipos elegidos se corren. ¿Alcanza con conservarlos por posición? | Sí para el sprint 1. Se revisa si molesta en el uso. |
| Q5 | ¿Alcanza un máximo de 9.999,99 m por medida? | Sí. Es el límite que ya tenía `Medicion`. |
| Q6 | En `Docs/07-backlog.md`, HU-03 («Segundo tipo de suelo») y los criterios de HU-01, HU-02 y HU-07 siguen con el esquema anterior. | El PM los actualiza a pozos con tipo (RF-VER-16 nuevo). La spec no depende de eso. |
