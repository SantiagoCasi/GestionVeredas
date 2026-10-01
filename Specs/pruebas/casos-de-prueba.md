# Casos de prueba — Sprint 1

> v0.1 · 30/09/2026 · Autor: agente `tester-qa`
> Fuentes: `Docs/01`, `Docs/03` (v0.2), `Docs/05` (v0.2), `Docs/06`, `Docs/07` (v0.2), SPEC-001 (v2), SPEC-002, SPEC-003 y SPEC-004 (v0.1).
> Los casos se escribieron desde los requisitos y las specs, no desde el código. No se ejecutaron: los ejecuta QC (`Specs/pruebas/ejecuciones/`).

## 1. Estrategia

### 1.1 Niveles

| Nivel | Qué prueba | Herramienta | Quién lo corre |
|---|---|---|---|
| **Unitaria** | `MedicionService` (`Calcular`, `AplicarAVereda`, `FormatearM2/M3`), `PaqueteService.CalcularTotales` y la validación de los ViewModels de contraseña. Sin base ni HTTP. | xUnit en `SistemaVeredas.Tests/Unitarias` | `dotnet test` |
| **Integración** | Controladores, base y migraciones por HTTP real: `WebApplicationFactory<Program>` contra `GestionVeredas_Pruebas`, con login real y token antifalsificación (SPEC-004 §4.6–4.7). Se afirma sobre la respuesta y sobre la base. | xUnit en `SistemaVeredas.Tests/Integracion` | `dotnet test` |
| **E2E** | Lo que ve y hace el usuario: JS en vivo (renglones, totales, bloqueo del envío, modal), vistas y flujos completos. | Navegador integrado de la app de Claude contra `https://localhost:7243` | QC |
| **Manual** | Lo que no se automatiza: migración sobre una base con datos, revisión del repositorio (`git grep`), configuración, mail real. | Terminal, SSMS/`sqlcmd`, VS | QC (con Santiago cuando hace falta) |

### 1.2 Entorno (Docs/06 §6 y SPEC-004 §2, §5 y §6)

- Todo corre en la PC de Santiago (D-25). Mientras dura una ronda, Santiago no usa la PC.
- Base: **`GestionVeredas_Pruebas`** en `DESKTOP-DTLN15N` (D-28). Nunca `GestionVeredas` ni el sitio publicado.
- Automatizadas: `dotnet build SistemaVeredas.sln` y `dotnet test SistemaVeredas.Tests --logger "trx;LogFileName=ultima.trx" --results-directory TestResults`.
- E2E: la app se levanta con las variables de SPEC-004 §6 (base de pruebas, usuario `qc@sistemaveredas.local`, `Smtp__Host=localhost`, `Smtp__Puerto=2525`). Antes de probar, QC confirma en la consola `Base de datos: GestionVeredas_Pruebas en DESKTOP-DTLN15N`.
- No se corre `dotnet test` durante una ronda E2E (borra y recrea la base).
- Navegador: el integrado de la app de Claude (Chromium). RNF-15 (Edge y Firefox) queda fuera del sprint.

### 1.3 Datos base (DB-QA)

Los crea QC por la interfaz (E2E) o cada prueba de integración con valores únicos (SPEC-004 §4.6).

| Clave | Dato |
|---|---|
| TS-V | Tipo de suelo `Vainilla`, medida `0,20x0,20`, disponible → se muestra «Vainilla (0,20x0,20)» |
| TS-C | Tipo de suelo `Cemento alisado`, sin medida, disponible → «Cemento alisado» |
| TS-G | Tipo de suelo `Granítico`, medida `40x40`, disponible → «Granítico (40x40)» |
| PR | Proveedor `Proveedor QA` (el alta de paquete lo exige mientras siga P-12) |
| Vereda base | Calle `QA <letra>` y altura `100`, más los demás campos obligatorios del formulario completos. Sin paquete salvo que el caso diga otra cosa. |
| {Dirección} | La dirección de la vereda tal como la muestra el sistema (calle y altura). |

### 1.4 Criterios de entrada

- La spec de la historia está aprobada y sus requisitos están `Confirmado` o con criterio provisorio registrado (1.5).
- Compila sin errores (`dotnet build SistemaVeredas.sln`).
- HU-12 hecha: el proyecto de pruebas corre contra `GestionVeredas_Pruebas` (CP-001 a CP-004 pasan).
- La app de E2E muestra la línea de la base de pruebas al arrancar (CP-006).

### 1.5 Criterios de salida

- Se ejecutaron todos los casos de prioridad Alta y Media de las historias del sprint.
- Pasan el 100 % de los Alta. Los Media y Baja que fallen tienen defecto registrado y Santiago acepta los que queden (Docs/06 §4).
- No queda ningún defecto Crítico ni Alto abierto.
- Las automatizadas están en verde en `TestResults/ultima.trx`.
- Todo requisito "Debe" del sprint tiene al menos un caso ejecutado (matriz, sección 3).

### 1.6 Criterios provisorios usados como esperado

Dados por el coordinador para dudas abiertas de SPEC-001. Si el PM decide otra cosa, se ajustan los casos marcados.

| Duda | Criterio | Casos |
|---|---|---|
| Q3 | El total en m² es la **suma de los subtotales ya redondeados**. | CP-034, CP-098 |
| Q4 | Los tipos de suelo se **conservan por posición** al editar la fórmula. | CP-102 a CP-104 |
| Q5 | Máximo **9.999,99 m por medida**. | CP-037, CP-055 |
| Q1 / RN-13 | Un tipo en uso **no se borra: se marca no disponible**. | CP-134 a CP-138 |

### 1.7 Convenciones

- Los mensajes van entre `«»` o en `código` y se comparan **exactos** (mayúsculas, tildes, comillas « », puntos).
- «Alta» = bloquea la historia si falla; «Media» = función con error o rodeo; «Baja» = detalle.
- Los decimales en los datos se escriben como los tipea el usuario; los resultados, con formato es-AR (`51,25 m²`). En las afirmaciones sobre la base, como valor decimal (`51.25`).

---

## 2. Casos

### 2.1 SPEC-004 · HU-12 — Proyecto y base de pruebas; acceso

#### CP-001 — La traba impide usar una base que no termina en `_Pruebas`
- **Verifica:** HU-12 (crit. 3) · RNF-19 · SPEC-004 §2, §8.1
- **Tipo / prioridad:** Manual · Alta
- **Precondiciones:** La base `GestionVeredas` existe. Se anota `SELECT COUNT(*) FROM Veredas` en `GestionVeredas`.
- **Pasos:** 1. En una terminal: `$env:SISTEMAVEREDAS_TEST_DB = "Server=DESKTOP-DTLN15N;Database=GestionVeredas;Trusted_Connection=True;Encrypt=False;"`. 2. `dotnet test SistemaVeredas.Tests`. 3. Volver a contar las veredas de `GestionVeredas`.
- **Datos:** la cadena del paso 1.
- **Resultado esperado:** Las pruebas de integración fallan con `InvalidOperationException` y el mensaje `La base de pruebas tiene que terminar en _Pruebas. Revisá SISTEMAVEREDAS_TEST_DB.` La base `GestionVeredas` sigue existiendo y la cantidad de veredas es la misma que antes.

#### CP-002 — Las pruebas de integración usan la base de pruebas
- **Verifica:** HU-12 (crit. 3) · RNF-19 · SPEC-004 §4.8 `UsaLaBaseDePruebas`, §8.2
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Sin `SISTEMAVEREDAS_TEST_DB`. User Secrets de Santiago con la cadena de desarrollo (caso real).
- **Pasos:** 1. `factory.CrearDbContext().Database.GetDbConnection().Database`.
- **Datos:** —
- **Resultado esperado:** El nombre de la base es `GestionVeredas_Pruebas` (termina en `_Pruebas`).

#### CP-003 — La base se recrea con las migraciones y no quedan pendientes
- **Verifica:** HU-12 (crit. 3) · RNF-10, RNF-19 · R-05 · SPEC-004 §2, §4.8 `NoQuedanMigracionesPendientes`
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Hay una `GestionVeredas_Pruebas` previa con una vereda de una corrida anterior.
- **Pasos:** 1. Correr la colección de integración. 2. `GetPendingMigrationsAsync()`. 3. Buscar la vereda de la corrida anterior.
- **Datos:** —
- **Resultado esperado:** `GetPendingMigrationsAsync()` devuelve una lista vacía; la vereda de la corrida anterior ya no existe (la base se borró y se creó de nuevo); existe el usuario sembrado `pruebas@sistemaveredas.local`.

#### CP-004 — `dotnet test` deja la salida en `TestResults/`, ignorada por git
- **Verifica:** HU-12 (crit. 1 y 2) · RNF-19 · D-25 · SPEC-004 §4.4, §5
- **Tipo / prioridad:** Manual · Alta
- **Precondiciones:** Repositorio sin cambios pendientes.
- **Pasos:** 1. `dotnet build SistemaVeredas.sln`. 2. `dotnet test SistemaVeredas.Tests --logger "trx;LogFileName=ultima.trx" --logger "console;verbosity=normal" --results-directory TestResults`. 3. `git status --porcelain` y `git check-ignore TestResults/ultima.trx`.
- **Datos:** —
- **Resultado esperado:** Compila con 0 errores; existe `TestResults/ultima.trx` con al menos 1 prueba unitaria y 1 de integración, todas `Passed`; `git status` no lista nada de `TestResults/`; `git check-ignore` imprime `TestResults/ultima.trx`.

#### CP-005 — La carpeta de pruebas no se compila ni se publica con la app
- **Verifica:** HU-12 · SPEC-004 §4.1
- **Tipo / prioridad:** Manual · Media
- **Precondiciones:** Proyecto de pruebas creado.
- **Pasos:** 1. `dotnet build SistemaVeredas.csproj`. 2. `dotnet publish SistemaVeredas.csproj -o <carpeta temporal>`. 3. Buscar en la salida `xunit*`, `SistemaVeredas.Tests*` y `xunit.runner.json`.
- **Datos:** —
- **Resultado esperado:** El build del csproj web da 0 errores (sin errores por `Xunit`). La carpeta publicada no tiene ningún archivo de `SistemaVeredas.Tests` ni `xunit.runner.json`.

#### CP-006 — La app de E2E arranca contra la base de pruebas y lo informa sin la cadena completa
- **Verifica:** HU-12 (crit. 4) · RNF-19 · SPEC-004 §4.5, §6
- **Tipo / prioridad:** Manual · Alta
- **Precondiciones:** Variables de SPEC-004 §6 cargadas en la terminal.
- **Pasos:** 1. `dotnet run --launch-profile https`. 2. Leer la consola. 3. Abrir `https://localhost:7243` e iniciar sesión con `qc@sistemaveredas.local`.
- **Datos:** —
- **Resultado esperado:** La consola muestra `Base de datos: GestionVeredas_Pruebas en DESKTOP-DTLN15N` y no muestra `Trusted_Connection` ni ninguna cadena completa. El login con el usuario de QC entra (el usuario solo existe en la base de pruebas).

#### CP-007 — Sin sesión, toda página redirige al login
- **Verifica:** RF-ACC-03, RNF-04 · RN-01 · SPEC-004 §4.8 `SinSesion_RedirigeAlLogin`
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Cliente sin cookie de sesión (`AllowAutoRedirect = false`).
- **Pasos:** GET a cada ruta de la lista.
- **Datos:** `/`, `/Veredas`, `/Veredas/Create`, `/Veredas/Details/{id existente}`, `/TipoSuelos`, `/Paquetes`, `/Paquetes/Details/{id}`, `/Paquetes/AgregarVeredas/{id}`, `/Usuarios`, `/Home/Veredas`, `/Home/Paquetes`.
- **Resultado esperado:** Cada una responde 302 con `Location` que empieza con `/Access/Login`.

#### CP-008 — Sin sesión, los POST nuevos no cambian nada
- **Verifica:** RF-ACC-03, RNF-04 · SPEC-001 §3.3, SPEC-002 §4.2–4.3
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Un paquete P y una vereda X sin paquete, creados con otro cliente con sesión.
- **Pasos:** Con un cliente sin sesión: POST a `/TipoSuelos/CrearRapido` (`Tipo=QA sin sesión`), `/Paquetes/AgregarVeredas/{P}` (`veredaIds=X`), `/Veredas/AgregarAPaquete` (`paqueteId=P`, `veredaIds=X`), `/Paquetes/QuitarVereda/{P}` (`veredaId=X`).
- **Datos:** los del paso.
- **Resultado esperado:** Cada POST responde 302 a `/Access/Login`. No existe el tipo `QA sin sesión` y X sigue con `PaqueteId` `NULL`.

#### CP-009 — El login y la recuperación se abren sin sesión
- **Verifica:** RF-ACC-03, RNF-04
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Cliente sin sesión.
- **Pasos:** GET `/Access/Login` y GET `/Access/StartRecovery`.
- **Datos:** —
- **Resultado esperado:** Las dos responden 200 con su formulario (incluye `__RequestVerificationToken`).

#### CP-010 — Login correcto entra
- **Verifica:** RF-ACC-01 · SPEC-004 §4.8 `LoginCorrecto_Entra`
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Usuario sembrado.
- **Pasos:** 1. `CrearClienteConSesionAsync`. 2. GET `/Veredas`.
- **Datos:** `EmailPrueba`, `ContrasenaPrueba`.
- **Resultado esperado:** El POST de login responde 302; GET `/Veredas` responde 200.

#### CP-011 — Login con contraseña equivocada
- **Verifica:** RF-ACC-01 · SPEC-004 §4.8 `LoginIncorrecto_MuestraMensaje`
- **Tipo / prioridad:** Integración · Media
- **Precondiciones:** Usuario sembrado.
- **Pasos:** POST `/Access/Login` con token y contraseña `equivocada123`.
- **Datos:** email del usuario sembrado.
- **Resultado esperado:** 200 y el HTML contiene `Email o contraseña incorrectos.`; GET `/Veredas` con ese cliente sigue respondiendo 302 al login.

#### CP-012 — POST de login sin token antifalsificación
- **Verifica:** RNF-07 · SPEC-004 §4.8 `PostSinToken_Rechaza`
- **Tipo / prioridad:** Integración · Media
- **Precondiciones:** —
- **Pasos:** POST `/Access/Login` con email y contraseña correctos y sin `__RequestVerificationToken`.
- **Datos:** —
- **Resultado esperado:** 400.

#### CP-013 — En el navegador, una URL interna sin sesión lleva al login
- **Verifica:** RF-ACC-03, RNF-04
- **Tipo / prioridad:** E2E · Media
- **Precondiciones:** Navegador sin sesión (después de «Cerrar sesión»).
- **Pasos:** 1. Escribir `https://localhost:7243/Veredas/Create` en la barra. 2. Iniciar sesión.
- **Datos:** usuario de QC.
- **Resultado esperado:** Se muestra la página de inicio de sesión; después de iniciar sesión se entra al sistema. Sin sesión no se ve ningún dato de veredas.

### 2.2 SPEC-001 · HU-01 — Migración `RoturasEnVeredas`

#### CP-014 — La migración se aplica sobre una base con datos y conserva las veredas
- **Verifica:** HU-01 (crit. 1) · RNF-10, RNF-18 · D-29 · R-05 · SPEC-001 §2.2, §2.5
- **Tipo / prioridad:** Manual · Alta
- **Precondiciones:** `GestionVeredas_Pruebas` llevada a la migración **anterior** a `RoturasEnVeredas` (`dotnet ef database update <migración anterior>` con la cadena de pruebas). Cargadas: TS-V, TS-C, 3 veredas (una con paquete y 2 fotos) y 2 filas en `Mediciones`. Se guarda el resultado de `SELECT * FROM Veredas` y de `SELECT * FROM TiposSuelo`.
- **Pasos:** 1. Aplicar `RoturasEnVeredas` (arrancar la app o `dotnet ef database update`). 2. Consultar `__EFMigrationsHistory`, las columnas de `Veredas` (`INFORMATION_SCHEMA.COLUMNS`) y los datos.
- **Datos:** los de las precondiciones.
- **Resultado esperado:** Se aplica sin errores y `RoturasEnVeredas` es la última fila de `__EFMigrationsHistory`. `Veredas` tiene `Medicion nvarchar(500) NULL`, `TotalM2 decimal(10,2) NULL`, `TieneCordon bit NOT NULL` con valor por defecto 0, `MedicionCordon nvarchar(500) NULL` y `TotalCordonM3 decimal(10,3) NULL`. Las 3 veredas conservan todas sus columnas anteriores con los mismos valores (incluidos paquete y fotos), con las 5 columnas nuevas en `NULL` / `0`. `TiposSuelo` no cambia.

#### CP-015 — Estructura de la tabla `Roturas`
- **Verifica:** HU-01 (crit. 1 y 3) · RNF-18 · D-29 · SPEC-001 §2.1, §2.4
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Base de pruebas migrada.
- **Pasos:** Consultar `INFORMATION_SCHEMA.COLUMNS`, `sys.indexes` y `sys.foreign_keys` de `dbo.Roturas`.
- **Datos:** —
- **Resultado esperado:** Columnas `Id int` identidad y PK, `VeredaId int NOT NULL`, `Orden int NOT NULL`, `Medidas nvarchar(500) NOT NULL`, `TipoSueloId int NOT NULL`, `SubtotalM2 decimal(10,2) NOT NULL`. Índices `IX_Roturas_VeredaId` e `IX_Roturas_TipoSueloId`, y **ningún** índice único sobre `(VeredaId, Orden)`. FK a `Veredas` con `delete_referential_action_desc = CASCADE`; FK a `TiposSuelo` con `NO_ACTION` (Restrict).

#### CP-016 — `Mediciones` se elimina sin convertir los datos
- **Verifica:** HU-01 (crit. 2) · D-27 · SPEC-001 §2.5 · SPEC-004 §4.8 `NoExisteLaTablaMediciones`
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Base de pruebas migrada (para la parte con datos, la de CP-014).
- **Pasos:** `SELECT OBJECT_ID('dbo.Mediciones')` y `SELECT COUNT(*) FROM Roturas`.
- **Datos:** —
- **Resultado esperado:** `OBJECT_ID` es `NULL`. En la base de CP-014, `Roturas` tiene 0 filas (las 2 mediciones no se convirtieron) y las 3 veredas figuran «Sin medir».

#### CP-017 — La base no admite una rotura sin tipo de suelo o con un tipo inexistente
- **Verifica:** HU-01 (crit. 3) · RF-VER-05 · D-26 · SPEC-001 §2.1
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Una vereda X en la base de pruebas.
- **Pasos:** 1. `INSERT INTO Roturas (VeredaId, Orden, Medidas, TipoSueloId, SubtotalM2) VALUES (X, 1, '2*3', NULL, 6)`. 2. El mismo `INSERT` con `TipoSueloId = 999999`.
- **Datos:** —
- **Resultado esperado:** 1: `SqlException` 515 (no admite `NULL` en `TipoSueloId`). 2: `SqlException` 547 (conflicto con la FK). No queda ninguna fila nueva.

#### CP-018 — El menú ya no tiene Mediciones y Tipos de suelo es solo el catálogo
- **Verifica:** HU-01 (crit. 4) · RN-04 · SPEC-001 §3.1
- **Tipo / prioridad:** E2E · Media
- **Precondiciones:** Sesión iniciada.
- **Pasos:** 1. Recorrer el menú. 2. Ir a `https://localhost:7243/Mediciones`. 3. Abrir Tipos de suelo: listado, alta, edición y detalle.
- **Datos:** —
- **Resultado esperado:** El menú no tiene ninguna opción «Mediciones». `/Mediciones` responde 404. Las pantallas de Tipos de suelo muestran solo tipo, medida, color y disponible; ninguna muestra ni pide mediciones, largo, ancho ni sector.

#### CP-019 — La migración se puede revertir
- **Verifica:** HU-01 · RNF-10 · SPEC-001 §2.5 (`Down`)
- **Tipo / prioridad:** Manual · Baja
- **Precondiciones:** Base de pruebas migrada.
- **Pasos:** `dotnet ef database update <migración anterior>` con la cadena de pruebas.
- **Datos:** —
- **Resultado esperado:** Sin errores. No existe `Roturas`; `Veredas` no tiene las 5 columnas nuevas; existe `Mediciones` vacía con sus índices y FK anteriores.

#### CP-020 — El modelo no tiene cambios sin migración
- **Verifica:** HU-01 · RNF-10 · SPEC-001 §2.5
- **Tipo / prioridad:** Manual · Media
- **Precondiciones:** Código de SPEC-001 completo.
- **Pasos:** `dotnet ef migrations has-pending-model-changes`.
- **Datos:** —
- **Resultado esperado:** Informa que no hay cambios pendientes del modelo (el snapshot coincide con las entidades).

### 2.3 SPEC-001 · HU-02 — Medición con total automático

#### 2.3.1 `MedicionService.Calcular` — cálculos válidos

- **Verifica (todo el grupo):** HU-02 · RF-VER-07 · RN-07, RN-08, RN-15, RN-17 · RNF-16, RNF-17 · SPEC-001 §3.2, §5.1
- **Precondiciones comunes:** la prueba fija `CultureInfo.CurrentCulture = es-AR` (para detectar dependencia de la cultura).
- **Pasos comunes:** llamar `MedicionService.Calcular(entrada, medidas)` y afirmar `Total`, `TextoNormalizado`, `Terminos` (`Orden`, `Medidas`, `Subtotal`) y `Errores`.
- **Resultado común de los válidos:** `Errores` vacío y `EsValido = true`.

| CP | Título | Tipo | Prio | Datos (entrada · medidas) | Resultado esperado |
|---|---|---|---|---|---|
| CP-021 | Ejemplo de varios términos (Docs/01) | Unitaria | Alta | `(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)` · 2 | `Total` 130,35. 5 términos, `Orden` 1 a 5, `Medidas` `2*3`, `5*9`, `4.5*8`, `2,4*4`, `3,75*9`; subtotales 6,00 · 45,00 · 36,00 · 9,60 · 33,75. `TextoNormalizado` = `(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)`. |
| CP-022 | Un solo término | Unitaria | Alta | `3*2` · 2 | `Total` 6,00; 1 término (`Orden` 1, `Medidas` `3*2`, subtotal 6,00); `TextoNormalizado` `(3*2)`. |
| CP-023 | Ejemplo con 0,5 (Docs/01 §5) | Unitaria | Alta | `(2*3)+(5*9)+(0,5*0,5)` · 2 | `Total` 51,25; subtotales 6,00 · 45,00 · 0,25; `Medidas` del término 3 = `0,5*0,5`. |
| CP-024 | Dos términos | Unitaria | Alta | `(2*3)+(5*9)` · 2 | `Total` 51,00; subtotales 6,00 · 45,00; `TextoNormalizado` `(2*3)+(5*9)`. |
| CP-025 | Sin paréntesis | Unitaria | Alta | `2*3+5*9` · 2 | `Total` 51,00; `TextoNormalizado` `(2*3)+(5*9)`. |
| CP-026 | `x` y `X` como multiplicación | Unitaria | Alta | `(2x3)+(5X9)+(4.5x8)+(2,4x4)+(3,75x9)` · 2 | `Total` 130,35; `TextoNormalizado` `(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)`. |
| CP-027 | Espacios permitidos | Unitaria | Alta | a) ` 2 * 3 ` · 2 · b) `2 x 3 + 5 * 9` · 2 · c) `( 2 * 3 )` · 2 · d) `2 x 3 + 5,5X2` · 2 | a) 6,00, `(2*3)` · b) 51,00, `(2*3)+(5*9)` · c) 6,00, `(2*3)` · d) 17,00, `(2*3)+(5,5*2)`. |
| CP-028 | Coma y punto dan lo mismo y se conservan | Unitaria | Alta | a) `2,4x3` · 2 · b) `2.4*3` · 2 | Los dos: `Total` 7,20. `Medidas`: a) `2,4*3`; b) `2.4*3`. |
| CP-029 | El punto no se lee como separador de miles | Unitaria | Alta | `4.5*2` · 2 (con cultura es-AR) | `Total` 9,00 (no 90,00); `TextoNormalizado` `(4.5*2)`. |
| CP-030 | Decimal sin parte entera | Unitaria | Baja | a) `.5*2` · 2 · b) `,5*2` · 2 | Los dos: `Total` 1,00. |
| CP-031 | Muchos términos | Unitaria | Media | `1*1` repetido 100 veces, unido con `+` · 2 | `Total` 100,00; 100 términos con `Orden` 1 a 100, cada uno con subtotal 1,00. |
| CP-032 | Medición vacía = sin medir | Unitaria | Alta | a) `null` · b) `""` · c) `"   "` · 2 | `Total` `null`, `TextoNormalizado` `null`, `Terminos` vacío, `Errores` vacío, `EstaVacia = true`. |
| CP-033 | Redondeo en el punto medio (m²) | Unitaria | Media | `0,005*1` · 2 | Subtotal 0,01; `Total` 0,01 (se aleja de cero). |
| CP-034 | El total es la suma de los subtotales redondeados (criterio Q3) | Unitaria | Alta | a) `0,005*1+0,005*1` · 2 · b) `0,333*1+0,333*1+0,334*1` · 2 | a) subtotales 0,01 · 0,01; `Total` 0,02. b) subtotales 0,33 · 0,33 · 0,33; `Total` **0,99** (no 1,00). |
| CP-035 | Cordón del ejemplo (m³) | Unitaria | Alta | `(5*0,15*0,30)` · 3 | Subtotal 0,225; `Total` **0,225**; `TextoNormalizado` `(5*0,15*0,30)`. |
| CP-036 | m³ con 3 decimales | Unitaria | Media | a) `1*0,5*0,3` · 3 · b) `0,0005*1*1` · 3 | a) `Total` 0,150 · b) `Total` 0,001 (punto medio, se aleja de cero). |
| CP-037 | Medida en el máximo (criterio Q5) | Unitaria | Alta | a) `9999,99*1` · 2 · b) `9999.99*2` · 2 | Válidos: a) 9.999,99 · b) 19.999,98. |
| CP-038 | Medidas muy chicas que redondean a 0 | Unitaria | Baja | `0,001*1` · 2 | Válido (la medida no es 0): subtotal 0,00; `Total` 0,00. Ver O-10. |
| CP-039 | Total en el máximo de la columna | Unitaria | Baja | `9999,99*9999,99` · 2 | Válido: `Total` 99.999.800,00. |

#### 2.3.2 `MedicionService.Calcular` — errores

- **Verifica (todo el grupo):** HU-02 · RN-15, RN-17 · RNF-03 · SPEC-001 §3.2 (algoritmo), §4.1, §5.1
- **Precondiciones y pasos comunes:** los de 2.3.1.
- **Resultado común:** `Total` `null`, `TextoNormalizado` `null`, `Terminos` vacío y `Errores` **exactamente** con los mensajes indicados, en orden de término.

| CP | Título | Tipo | Prio | Datos (entrada · medidas) | Mensajes esperados |
|---|---|---|---|---|---|
| CP-040 | Error de tipeo del ejemplo | Unitaria | Alta | `(2*3)+59` · 2 | `El término 2 («59») tiene 1 medida y lleva 2.` (único error) |
| CP-041 | Un término con 1 medida en m² | Unitaria | Alta | `5` · 2 | `El término 1 («5») tiene 1 medida y lleva 2.` |
| CP-042 | 3 medidas en m² | Unitaria | Alta | `2*3*4` · 2 | `El término 1 («2*3*4») tiene 3 medidas y lleva 2.` |
| CP-043 | 2 medidas en m³ | Unitaria | Alta | `(5*0,15)` · 3 | `El término 1 («(5*0,15)») tiene 2 medidas y lleva 3.` |
| CP-044 | 1 y 4 medidas en m³ | Unitaria | Media | a) `5` · 3 · b) `1*1*1*1` · 3 | a) `El término 1 («5») tiene 1 medida y lleva 3.` · b) `El término 1 («1*1*1*1») tiene 4 medidas y lleva 3.` |
| CP-045 | m³ con un término bien y otro mal | Unitaria | Media | `(2*3)+(4*5*6)` · 3 | `El término 1 («(2*3)») tiene 2 medidas y lleva 3.` (el término 2 no tiene error) |
| CP-046 | Negativos | Unitaria | Alta | a) `-2*3` · 2 · b) `2*3+-4*5` · 2 | a) `El término 1 («-2*3») tiene un carácter que no se permite: «-». Usá solo números, «+», «*» (o «x») y paréntesis.` · b) `El término 2 («-4*5») tiene un carácter que no se permite: «-». Usá solo números, «+», «*» (o «x») y paréntesis.` |
| CP-047 | Resta y división | Unitaria | Alta | a) `2-3` · b) `2/3` · 2 | a) `El término 1 («2-3») tiene un carácter que no se permite: «-». Usá solo números, «+», «*» (o «x») y paréntesis.` · b) igual con `(«2/3»)` y `«/»`. |
| CP-048 | Texto | Unitaria | Alta | a) `abc` · b) `2*a` · c) `dos*tres` · 2 | a) `El término 1 («abc») tiene un carácter que no se permite: «a». Usá solo números, «+», «*» (o «x») y paréntesis.` · b) igual con `(«2*a»)` y `«a»` · c) igual con `(«dos*tres»)` y `«d»`. |
| CP-049 | Término vacío | Unitaria | Alta | a) `2*3++4*5` · b) `+2*3` · c) `(2*3)+` · 2 | a) `El término 2 está vacío: revisá que no haya dos «+» seguidos ni un «+» al principio o al final.` · b) lo mismo con `El término 1` · c) lo mismo con `El término 2`. |
| CP-050 | Paréntesis mal puestos | Unitaria | Alta | a) `(2*3` · b) `2*(3)` · c) `((2*3))` · d) `(2)*(3)` · 2 | `El término 1 («(2*3») tiene los paréntesis mal puestos. Cada término puede ir entre paréntesis, así: (2*3).` y lo mismo con `«2*(3)»`, `«((2*3))»` y `«(2)*(3)»`. |
| CP-051 | Paréntesis que agrupan una suma | Unitaria | Media | `(2+3)*4` · 2 | Dos errores: `El término 1 («(2») tiene los paréntesis mal puestos. Cada término puede ir entre paréntesis, así: (2*3).` y `El término 2 («3)*4») tiene los paréntesis mal puestos. Cada término puede ir entre paréntesis, así: (2*3).` |
| CP-052 | Medida vacía | Unitaria | Media | a) `2**3` · b) `*2*3` · c) `2*3*` · 2 | `El término 1 («2**3») tiene una medida vacía: revisá que no haya dos «*» seguidos ni un «*» al principio o al final.` y lo mismo con `«*2*3»` y `«2*3*»`. |
| CP-053 | Número inválido | Unitaria | Media | a) `1.234,5*2` · b) `2,*3` · c) `2.5.3*1` · 2 | a) `El término 1 («1.234,5*2») tiene una medida que no es un número válido: «1.234,5». Usá la coma o el punto solo para los decimales (2,5 o 2.5).` · b) igual con `(«2,*3»)` y `«2,»` · c) igual con `(«2.5.3*1»)` y `«2.5.3»`. |
| CP-054 | Medida en cero | Unitaria | Alta | a) `0*5` · b) `2*0,0` · 2 | `El término 1 («0*5») tiene una medida en cero. Las medidas tienen que ser mayores a 0.` y lo mismo con `«2*0,0»`. |
| CP-055 | Medida sobre el máximo (criterio Q5) | Unitaria | Alta | a) `10000*1` · b) `9999,991*1` · 2 | `El término 1 («10000*1») tiene una medida mayor a 9.999,99 m.` y lo mismo con `«9999,991*1»`. |
| CP-056 | Espacio dentro de una medida | Unitaria | Alta | a) `4 5*2` · b) `2 ,5*2` · 2 | `El término 1 («4 5*2») tiene un espacio dentro de una medida. Separá las medidas con «*».` y lo mismo con `«2 ,5*2»`. |
| CP-057 | Total demasiado grande | Unitaria | Media | a) `9999,99*9999,99+9999,99*9999,99` · 2 · b) `9999,99*9999,99*9999,99` · 3 | Los dos: `El total es demasiado grande. Revisá la medición.` (sin excepción). |
| CP-058 | Se informan todos los términos con error | Unitaria | Alta | `59+2*3+7` · 2 | Dos errores, en este orden: `El término 1 («59») tiene 1 medida y lleva 2.` y `El término 3 («7») tiene 1 medida y lleva 2.` |
| CP-059 | Solo el primer error de cada término | Unitaria | Media | `(2*a` · 2 | Un solo error: `El término 1 («(2*a») tiene un carácter que no se permite: «a». Usá solo números, «+», «*» (o «x») y paréntesis.` (no el de paréntesis). |
| CP-060 | Cantidad de medidas no soportada | Unitaria | Baja | `2*3` · 4 y `2*3` · 1 | `ArgumentOutOfRangeException` en los dos. |

#### 2.3.3 `MedicionService.FormatearM2` / `FormatearM3`

| CP | Título | Tipo | Prio | Datos | Resultado esperado |
|---|---|---|---|---|---|
| CP-061 | Formato es-AR independiente de la cultura del servidor | Unitaria | Media | Con `CurrentCulture` = `en-US`: `FormatearM2(51.25m)`, `FormatearM2(6m)`, `FormatearM2(130.35m)`, `FormatearM3(0.225m)`, `FormatearM3(0.15m)`, `FormatearM2(null)`, `FormatearM3(null)` | `"51,25 m²"`, `"6,00 m²"`, `"130,35 m²"`, `"0,225 m³"`, `"0,150 m³"`, `null`, `null`. (SPEC-001 §3.2; RNF-01) |
| CP-062 | Separador de miles | Unitaria | Baja | `FormatearM2(2000m)`, `FormatearM2(1082m)` | `"2.000,00 m²"` y `"1.082,00 m²"` (formato es-AR; a confirmar, ver O-11). |

#### 2.3.4 `MedicionService.AplicarAVereda`

- **Verifica (todo el grupo):** HU-02, HU-03, HU-04 · RF-VER-05, RF-VER-08, RF-VER-09, RF-VER-16 · RN-04, RN-05, RN-06 · RNF-07, RNF-18 · D-26 · SPEC-001 §3.2, §4.2, §5.2
- **Precondiciones comunes:** una `Vereda` en memoria; `V`, `C` = ids de TS-V y TS-C.
- **Pasos comunes:** asignar `Medicion`, `TieneCordon`, `MedicionCordon`, llamar `AplicarAVereda(vereda, tipos)` y afirmar la vereda y la lista de `ErrorMedicion`.

| CP | Título | Tipo | Prio | Datos | Resultado esperado |
|---|---|---|---|---|---|
| CP-063 | Una rotura por término, con tipos repetidos | Unitaria | Alta | `(2*3)+(5*9)+(0,5*0,5)`, tipos `[V, C, V]` | Sin errores. 3 roturas con `Id` 0, `Orden` 1–3, `Medidas` `2*3` / `5*9` / `0,5*0,5`, `TipoSueloId` V / C / V, `SubtotalM2` 6,00 / 45,00 / 0,25. `TotalM2` 51,25; `Medicion` `(2*3)+(5*9)+(0,5*0,5)`. |
| CP-064 | Pozo sin tipo | Unitaria | Alta | la misma fórmula, tipos `[V, null, V]` | Un error: campo `tiposRotura[1]`, mensaje `Elegí el tipo de suelo del pozo 2.` |
| CP-065 | Cantidad de tipos distinta de términos | Unitaria | Alta | la misma fórmula con a) `[V, C]`, b) `[V, C, V, C]`; c) `3*2` con `[]` | Un error en cada uno: campo `Medicion`, mensaje `La cantidad de pozos no coincide con los tipos de suelo elegidos. Revisá la medición y volvé a elegir los tipos.` Ningún error de `tiposRotura[i]`. |
| CP-066 | Fórmula vacía con tipos mandados | Unitaria | Alta | `Medicion` `""`, tipos `[V]` | Sin errores. `Medicion` `null`, `TotalM2` `null`, `Roturas` vacía. |
| CP-067 | Fórmula con errores: no se revisan los tipos | Unitaria | Media | `(2*3)+59`, tipos `[null, null]` | Un solo error: campo `Medicion`, `El término 2 («59») tiene 1 medida y lleva 2.` |
| CP-068 | El tipo de suelo no cambia el cálculo | Unitaria | Media | `(2*3)+(5*9)+(0,5*0,5)` con `[V, V, V]` y con `[C, C, C]` | En los dos: subtotales 6,00 / 45,00 / 0,25 y `TotalM2` 51,25. |
| CP-069 | Cordón sin marcar se descarta | Unitaria | Alta | `TieneCordon = false`, `MedicionCordon` a) `(5*0,15*0,30)`, b) `abc` | Sin errores en los dos. `MedicionCordon` `null` y `TotalCordonM3` `null`. |
| CP-070 | Medición y cordón juntos | Unitaria | Alta | `(2*3)+(5*9)` con `[V, C]`; `TieneCordon = true`, `MedicionCordon` `(5*0,15*0,30)` | Sin errores. `TotalM2` 51,00; `TotalCordonM3` 0,225; `MedicionCordon` `(5*0,15*0,30)`; 2 roturas (el cordón no crea roturas); `EstaMedida = true`. |
| CP-071 | Solo cordón | Unitaria | Media | `Medicion` vacía; `TieneCordon = true`, `MedicionCordon` `1*0,5*0,3` | Sin errores. `TotalM2` `null`; `TotalCordonM3` 0,150; `MedicionCordon` `(1*0,5*0,3)`; `EstaMedida = true`. |
| CP-072 | Cordón marcado y vacío | Unitaria | Media | `Medicion` vacía; `TieneCordon = true`, `MedicionCordon` `""` | Sin errores. `TieneCordon` `true`, `TotalCordonM3` `null`, `EstaMedida = false`. |
| CP-073 | Error en el cordón | Unitaria | Alta | `(2*3)` con `[V]`; `TieneCordon = true`, `MedicionCordon` `(5*0,15)` | Un error: campo `MedicionCordon`, `El término 1 («(5*0,15)») tiene 2 medidas y lleva 3.` |
| CP-074 | Reemplaza las roturas anteriores | Unitaria | Media | Vereda con 3 roturas previas (`Id` 1, 2, 3); `(2*3)+(5*9)` con `[V, C]` | `Roturas` tiene exactamente 2 elementos, los dos con `Id` 0 y `Orden` 1 y 2. |

#### 2.3.5 Integración: alta, edición y borrado de veredas con medición

- **Precondiciones comunes:** cliente con sesión (`CrearClienteConSesionAsync`); TS-V, TS-C y TS-G creados; vereda base con calle única. Los POST van con el token del formulario (`/Veredas/Create` o `/Veredas/Edit/{id}`) y los desplegables como `tiposRotura[i]`.

#### CP-075 — Alta guarda la fórmula, una rotura por término y el total
- **Verifica:** HU-02 (crit. 5), HU-03 · RF-VER-07, RF-VER-16 · RNF-18 · SPEC-004 §4.8 `Crear_GuardaUnaRoturaPorTermino`
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST `/Veredas/Create` con la vereda base, `Medicion=(2*3)+(5*9)`, `tiposRotura[0]=V`, `tiposRotura[1]=C`.
- **Datos:** los del paso.
- **Resultado esperado:** 302 a `/Veredas` (Index). En la base: `Veredas.Medicion` = `(2*3)+(5*9)`, `TotalM2` = 51.00; `Roturas` de esa vereda = 2 filas: (`Orden` 1, `Medidas` `2*3`, `TipoSueloId` V, `SubtotalM2` 6.00) y (2, `5*9`, C, 45.00).

#### CP-076 — Alta con el ejemplo de 5 términos escrito con `x`
- **Verifica:** HU-02 (crit. 2) · RN-08, RN-15 · RNF-17
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST Create con `Medicion=(2x3)+(5X9)+(4.5x8)+(2,4x4)+(3,75x9)` y `tiposRotura[0..4]=V`.
- **Datos:** los del paso.
- **Resultado esperado:** 302. `Medicion` = `(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)`, `TotalM2` = 130.35, 5 roturas con subtotales 6.00, 45.00, 36.00, 9.60, 33.75.

#### CP-077 — Mismo tipo en varios pozos
- **Verifica:** HU-03 (crit. 4) · RN-04 · D-26 · SPEC-004 `Crear_MismoTipoEnVariosPozos`
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST Create con `Medicion=(2*3)+(5*9)+(0,5*0,5)` y los tres `tiposRotura` = V.
- **Datos:** —
- **Resultado esperado:** 302. 3 filas en `Roturas`, las tres con `TipoSueloId` V; `TotalM2` 51.25.

#### CP-078 — Pozo sin tipo: no se guarda nada
- **Verifica:** HU-03 (crit. 3) · RF-VER-05 · D-26 · SPEC-004 `Crear_TerminoSinTipoDeSuelo_NoGuarda`
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST Create con `Medicion=(2*3)+(5*9)`, `tiposRotura[0]=V`, `tiposRotura[1]=` (vacío).
- **Datos:** calle única `QA sin tipo <guid>`.
- **Resultado esperado:** 200 y el HTML contiene `Elegí el tipo de suelo del pozo 2.` No existe ninguna vereda con esa calle ni roturas nuevas.

#### CP-079 — Fórmula inválida: se informa, no se guarda y vuelve como se escribió
- **Verifica:** HU-02 (crit. 3) · RN-17 · RNF-03, RNF-07
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST Create con a) `Medicion=(2*3)+59` y dos tipos; b) `Medicion=2-3` y un tipo.
- **Datos:** calle única.
- **Resultado esperado:** a) 200 con `El término 2 («59») tiene 1 medida y lleva 2.` · b) 200 con `El término 1 («2-3») tiene un carácter que no se permite: «-». Usá solo números, «+», «*» (o «x») y paréntesis.` En los dos, el input «Medición» vuelve con el texto tal como se mandó y no se guarda ninguna vereda.

#### CP-080 — Cantidad de tipos distinta de términos (POST manipulado)
- **Verifica:** HU-03 · RNF-07 · SPEC-001 §4.2
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST Create con `Medicion=(2*3)+(5*9)+(0,5*0,5)` y solo `tiposRotura[0]=V`, `tiposRotura[1]=C`.
- **Datos:** calle única.
- **Resultado esperado:** 200 con `La cantidad de pozos no coincide con los tipos de suelo elegidos. Revisá la medición y volvé a elegir los tipos.` No se guarda la vereda.

#### CP-081 — Tipo de suelo inexistente
- **Verifica:** HU-03 · RNF-07 · SPEC-001 §3.3 `ValidarTiposSueloAsync`, §4.2
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST Create con `Medicion=3*2` y `tiposRotura[0]=999999`.
- **Datos:** calle única.
- **Resultado esperado:** 200 con `El tipo de suelo del pozo 1 no existe. Elegí otro de la lista.` No se guarda la vereda (ni hay error 500).

#### CP-082 — Alta sin medición
- **Verifica:** HU-02 (crit. 4) · RF-VER-09 · RN-09 · D-06 · SPEC-004 `Crear_SinMedicion`
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST Create con `Medicion=` (vacío) y `tiposRotura[0]=V`.
- **Datos:** calle única.
- **Resultado esperado:** 302. La vereda existe con `Medicion` `NULL`, `TotalM2` `NULL`, `TieneCordon` 0 y 0 filas en `Roturas`.

#### CP-083 — El total lo calcula el servidor
- **Verifica:** HU-02 (crit. 5) · RNF-07 · SPEC-001 §3.2 paso 5, §3.3 · SPEC-004 `ElTotalLoCalculaElServidor`
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST Create con `Medicion=(2*3)+(5*9)`, tipos V y C, y además `TotalM2=999`, `TotalCordonM3=999`, `Roturas[0].SubtotalM2=999`, `SubtotalM2=999`.
- **Datos:** —
- **Resultado esperado:** 302. `TotalM2` = 51.00, `TotalCordonM3` `NULL`, subtotales 6.00 y 45.00.

#### CP-084 — Editar reemplaza las roturas (de 2 a 1)
- **Verifica:** HU-02 (crit. 6), HU-03 · RF-VER-13 · RNF-18 · SPEC-001 §3.3 · SPEC-004 `Editar_ReemplazaLasRoturas`
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Vereda X creada con `(2*3)+(5*9)` y tipos V, C. Se anota `COUNT(*)` de `Roturas`.
- **Pasos:** POST `/Veredas/Edit/{X}` con `Medicion=4*5` y `tiposRotura[0]=C`.
- **Datos:** —
- **Resultado esperado:** 302. X tiene 1 fila en `Roturas` (`Orden` 1, `Medidas` `4*5`, C, 20.00); el total de filas de `Roturas` bajó en 1 (no quedan huérfanas); `Medicion` `(4*5)`, `TotalM2` 20.00.

#### CP-085 — Editar de 1 a 3 pozos
- **Verifica:** HU-03 · RNF-18 · SPEC-001 §5.2
- **Tipo / prioridad:** Integración · Media
- **Precondiciones:** Vereda X con `3*2` y tipo V.
- **Pasos:** POST Edit con `Medicion=(2*3)+(5*9)+(0,5*0,5)` y tipos C, V, C.
- **Datos:** —
- **Resultado esperado:** 302. X tiene 3 filas, `Orden` 1, 2, 3, tipos C, V, C y subtotales 6.00, 45.00, 0.25; `TotalM2` 51.25.

#### CP-086 — Editar y borrar la medición
- **Verifica:** HU-02 · RF-VER-09 · RN-09
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Vereda X con `(2*3)+(5*9)`.
- **Pasos:** POST Edit con `Medicion=` (vacío) y sin tipos.
- **Datos:** —
- **Resultado esperado:** 302. X con `Medicion` `NULL`, `TotalM2` `NULL` y 0 filas en `Roturas`.

#### CP-087 — Lo guardado se ve igual al editar
- **Verifica:** HU-02 (crit. 6), HU-03 (crit. 6) · RF-VER-13
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Vereda X guardada con `(2*3)+(5*9)+(0,5*0,5)` y tipos V, C, V.
- **Pasos:** GET `/Veredas/Edit/{X}` y analizar el HTML.
- **Datos:** —
- **Resultado esperado:** El input «Medición» tiene `value="(2*3)+(5*9)+(0,5*0,5)"`. Hay 3 renglones «Pozo 1», «Pozo 2», «Pozo 3» con `select name="tiposRotura[0]"`, `[1]` y `[2]` y la opción seleccionada V, C y V. El total dice `51,25 m²`.

#### CP-088 — Si falla otro campo, la medición vuelve con sus tipos
- **Verifica:** HU-02, HU-03 · SPEC-001 §3.3 `CargarListas`, §5.3
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST Create con la calle vacía (u otro campo obligatorio vacío), `Medicion=(2*3)+(5*9)`, tipos C y V, `TieneCordon=true`, `MedicionCordon=(5*0,15*0,30)`.
- **Datos:** —
- **Resultado esperado:** 200 con el error del campo obligatorio. El input «Medición» trae `(2*3)+(5*9)`; `tiposRotura[0]` seleccionado C y `tiposRotura[1]` V; la casilla «Cordón» marcada y `MedicionCordon` con `(5*0,15*0,30)`. No se guarda la vereda.

#### CP-089 — Largo máximo de la medición: 500 sí, 501 no
- **Verifica:** HU-02 · RF-VER-07 · SPEC-001 §2.2, §4.2
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST Create con a) 500 caracteres; b) 501 caracteres, cada uno con 83 tipos V.
- **Datos:** a) `(1*1)+` repetido 82 veces seguido de `(1*1000)` (500 caracteres, 83 términos) · b) `(1*1)+` repetido 82 veces seguido de `(1*999,9)` (501 caracteres).
- **Resultado esperado:** a) 302; `TotalM2` 1082.00; 83 roturas; `Medicion` igual a la entrada. b) 200 con `La medición puede tener hasta 500 caracteres.`; no se guarda.

#### CP-090 — Fórmula de 500 caracteres sin paréntesis (normalizada supera 500)
- **Verifica:** HU-02 · RF-VER-07 (sin límite de pozos) · SPEC-001 §2.2, §3.2 paso 8 · ver O-01
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST Create con la fórmula de los datos y 125 tipos V.
- **Datos:** `1*1` seguido de `+1*1` repetido 123 veces y de `+1*10` (500 caracteres, 125 términos; normalizada con paréntesis mide 750).
- **Resultado esperado:** Nunca 500 ni la fórmula truncada en la base. Lo exacto queda **a definir** (O-01): o se guarda completa (302, `TotalM2` 134.00, 125 roturas) o se rechaza con un mensaje de validación (200, sin guardar).

#### CP-091 — Borrar una vereda borra sus roturas (cascada)
- **Verifica:** HU-01 · RF-VER-14 · SPEC-001 §2.1, §5.3 · SPEC-004 `Eliminar_BorraSusRoturas`
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Vereda X sin paquete con 3 roturas.
- **Pasos:** POST `/Veredas/Delete/{X}` (DeleteConfirmed) con token.
- **Datos:** —
- **Resultado esperado:** 302. No existe X y `SELECT COUNT(*) FROM Roturas WHERE VeredaId = X` = 0.

#### CP-092 — Alta de vereda sin token antifalsificación
- **Verifica:** RNF-07
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST `/Veredas/Create` con sesión y datos válidos, sin `__RequestVerificationToken`.
- **Datos:** calle única.
- **Resultado esperado:** 400; no se guarda la vereda.

#### 2.3.6 E2E: medición en vivo

- **Precondiciones comunes:** app de E2E levantada (CP-006), sesión iniciada, DB-QA cargado, formulario «Agregar vereda» abierto.

#### CP-093 — Total al instante
- **Verifica:** HU-02 (crit. 1) · RF-VER-07 · RNF-03
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Escribir `(2*3)+(5*9)` en «Medición», sin guardar.
- **Datos:** —
- **Resultado esperado:** «Total» muestra `51,00 m²` mientras se escribe, sin recargar la página.

#### CP-094 — Ejemplos de Docs/01 en vivo
- **Verifica:** HU-02 (crit. 2) · RN-08, RN-15
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Escribir cada fórmula, borrando la anterior.
- **Datos:** a) `(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)` · b) `(2x3)+(5X9)+(4.5x8)+(2,4x4)+(3,75x9)` · c) `3*2` · d) `(2*3)+(5*9)+(0,5*0,5)`
- **Resultado esperado:** a) `130,35 m²` · b) `130,35 m²` · c) `6,00 m²` · d) `51,25 m²`.

#### CP-095 — Error en vivo y bloqueo del envío
- **Verifica:** HU-02 (crit. 3) · RN-17 · RNF-03 · SPEC-001 §3.6.1, §3.6.4
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. Completar los demás campos. 2. Escribir `(2*3)+59`. 3. Elegir tipo en los dos renglones. 4. «Guardar».
- **Datos:** —
- **Resultado esperado:** El renglón «Pozo 2» muestra en rojo `El término 2 («59») tiene 1 medida y lleva 2.` y conserva su desplegable. «Total» muestra `—`. Al guardar no se envía el formulario (sigue en la misma URL, con las fotos elegidas) y aparece `Revisá la medición y los tipos de suelo marcados en rojo antes de guardar.`

#### CP-096 — El JS muestra los mismos mensajes que el servidor
- **Verifica:** HU-02 · RNF-03 · SPEC-001 §3.6.1, §4.1
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Escribir cada fórmula en «Medición» y leer el mensaje del renglón con error.
- **Datos:** `-2*3`, `abc`, `2*3++4*5`, `(2*3`, `2**3`, `1.234,5*2`, `0*5`, `10000*1`, `4 5*2`, `2*3*4`.
- **Resultado esperado:** Cada mensaje es idéntico al esperado en CP-046 a, CP-048 a, CP-049 a, CP-050 a, CP-052 a, CP-053 a, CP-054 a, CP-055 a, CP-056 a y CP-042. «Total» muestra `—` en todos.

#### CP-097 — Medición vacía: sin renglones y se guarda sin medir
- **Verifica:** HU-02 (crit. 4) · RF-VER-09 · RN-09
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. Escribir `(2*3)` y borrarlo. 2. Guardar con los demás campos completos. 3. Ver el listado.
- **Datos:** calle `QA Sin medir`.
- **Resultado esperado:** Con el campo vacío no hay ningún renglón «Pozo» y «Total» muestra `—`. Se guarda y en el listado la vereda tiene la insignia «Sin medir».

#### CP-098 — La vista previa redondea igual que el servidor
- **Verifica:** HU-02 · RNF-17 · SPEC-001 §3.6.2 · criterio Q3
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Escribir cada fórmula, elegir tipos, guardar y abrir el detalle.
- **Datos:** a) `0,005*1+0,005*1` · b) `0,333*1+0,333*1+0,334*1`
- **Resultado esperado:** En vivo: a) `0,02 m²`, b) `0,99 m²`. En el detalle, «Total m²»: a) `0,02 m²`, b) `0,99 m²`.

#### CP-099 — El servidor valida aunque se saltee el JS
- **Verifica:** HU-02 (crit. 5) · RNF-07
- **Tipo / prioridad:** E2E · Media
- **Pasos:** 1. Desactivar JavaScript en el navegador (o quitar el `submit` con las herramientas de desarrollo). 2. Escribir `(2*3)+59` y guardar.
- **Datos:** —
- **Resultado esperado:** El servidor devuelve el formulario con `El término 2 («59») tiene 1 medida y lleva 2.` y la vereda no aparece en el listado.

#### CP-100 — Textos del campo Medición
- **Verifica:** HU-02 · SPEC-001 §3.5
- **Tipo / prioridad:** E2E · Baja
- **Pasos:** Mirar el campo vacío.
- **Datos:** —
- **Resultado esperado:** Placeholder `(2*3)+(5*9)`, `maxlength` 500 y, debajo, la ayuda: `Escribí cada pozo como largo × ancho en metros y sumalos: (2*3)+(5*9). Podés usar coma o punto decimal y «x» en lugar de «*». Después elegí el tipo de suelo de cada pozo.`

### 2.4 SPEC-001 · HU-03 — Tipo de suelo de cada rotura

- **Precondiciones comunes:** las de 2.3.6.

#### CP-101 — Un renglón por pozo con medidas, subtotal y desplegable
- **Verifica:** HU-03 (crit. 1) · RF-VER-05, RF-VER-16 · SPEC-001 §3.5, §3.6.1
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Escribir `(2*3)+(5*9)+(0,5*0,5)` y abrir cada desplegable.
- **Datos:** DB-QA.
- **Resultado esperado:** Tres renglones: «Pozo 1» `2 × 3` `= 6,00 m²`; «Pozo 2» `5 × 9` `= 45,00 m²`; «Pozo 3» `0,5 × 0,5` `= 0,25 m²`. Cada uno con un desplegable que empieza en `-- Elegí el tipo de suelo --` y lista, ordenados por tipo y medida, `Cemento alisado`, `Granítico (40x40)` y `Vainilla (0,20x0,20)`, y un botón «+ Nuevo». «Total» `51,25 m²`.

#### CP-102 — Agregar un término al final conserva los tipos (criterio Q4)
- **Verifica:** HU-03 (crit. 2) · RF-VER-16 · SPEC-001 §3.6.1
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. `(2*3)+(5*9)`, elegir TS-V y TS-C. 2. Agregar `+(1*1)` al final.
- **Datos:** —
- **Resultado esperado:** Pozo 1 sigue con `Vainilla (0,20x0,20)`, Pozo 2 con `Cemento alisado` y aparece Pozo 3 en `-- Elegí el tipo de suelo --`. «Total» `52,00 m²`.

#### CP-103 — Borrar el último término conserva los demás
- **Verifica:** HU-03 (crit. 2) · RF-VER-16
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. `(2*3)+(5*9)+(0,5*0,5)` con TS-V, TS-C, TS-G. 2. Borrar `+(0,5*0,5)`.
- **Datos:** —
- **Resultado esperado:** Quedan 2 renglones: Pozo 1 `Vainilla (0,20x0,20)` y Pozo 2 `Cemento alisado`. «Total» `51,00 m²`.

#### CP-104 — Insertar o borrar en el medio: los tipos se conservan por posición
- **Verifica:** HU-03 (crit. 2) · SPEC-001 §5.3 · criterio Q4 · ver O-04
- **Tipo / prioridad:** E2E · Media
- **Pasos:** a) `(2*3)+(5*9)` con TS-V y TS-C → cambiar a `(2*3)+(1*1)+(5*9)`. b) `(2*3)+(5*9)+(0,5*0,5)` con TS-V, TS-C, TS-G → borrar `+(5*9)`.
- **Datos:** —
- **Resultado esperado:** a) Pozo 1 `Vainilla (0,20x0,20)`, Pozo 2 (`1 × 1`) `Cemento alisado`, Pozo 3 (`5 × 9`) sin elegir; «Guardar» se bloquea hasta elegirlo. b) Pozo 1 `Vainilla (0,20x0,20)`, Pozo 2 (`0,5 × 0,5`) `Cemento alisado`.

#### CP-105 — Los desplegables se renumeran en orden
- **Verifica:** HU-03 · SPEC-001 §3.3, §3.6.1
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Después de CP-104 b, inspeccionar el DOM.
- **Datos:** —
- **Resultado esperado:** Los `select` se llaman `tiposRotura[0]` y `tiposRotura[1]`, sin huecos; cada botón «+ Nuevo» tiene `data-destino` 0 y 1.

#### CP-106 — Un término con error mantiene su desplegable
- **Verifica:** HU-03 · RF-VER-16 · SPEC-001 §3.6.1
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Escribir `(2*3)+59+(1*1)`.
- **Datos:** —
- **Resultado esperado:** 3 renglones y 3 desplegables. Pozo 2 muestra el error en rojo `El término 2 («59») tiene 1 medida y lleva 2.`; «Total» `—`.

#### CP-107 — Pozo sin tipo: se bloquea el envío y se marca el pozo
- **Verifica:** HU-03 (crit. 3) · RF-VER-05 · D-26 · SPEC-001 §3.6.4
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** `(2*3)+(5*9)`, elegir solo el Pozo 1 y «Guardar».
- **Datos:** demás campos completos.
- **Resultado esperado:** No se envía. Aparece `Revisá la medición y los tipos de suelo marcados en rojo antes de guardar.`; el Pozo 2 muestra en rojo `Elegí el tipo de suelo del pozo 2.` y el foco queda en su desplegable.

#### CP-108 — Tipos repetidos se guardan
- **Verifica:** HU-03 (crit. 4) · RN-04 · D-26
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** `(2*3)+(5*9)+(0,5*0,5)` con TS-V en los tres; guardar; abrir el detalle.
- **Datos:** calle `QA Repetidos`.
- **Resultado esperado:** Se guarda sin error. El detalle muestra los 3 pozos, cada uno con `Vainilla (0,20x0,20)`, y «Total m²» `51,25 m²`.

#### CP-109 — Al editar, cada renglón muestra su tipo
- **Verifica:** HU-03 (crit. 6) · RF-VER-13 · SPEC-001 §3.5
- **Tipo / prioridad:** E2E · Alta
- **Precondiciones:** Vereda guardada con `(2*3)+(5*9)+(0,5*0,5)` y TS-V, TS-C, TS-V.
- **Pasos:** Abrir «Editar» y esperar a que cargue el JS.
- **Datos:** —
- **Resultado esperado:** «Medición» `(2*3)+(5*9)+(0,5*0,5)`; Pozo 1 `Vainilla (0,20x0,20)`, Pozo 2 `Cemento alisado`, Pozo 3 `Vainilla (0,20x0,20)`; «Total» `51,25 m²`. Guardar sin cambios deja las mismas 3 roturas.

#### CP-110 — Tipo no disponible: se ve en los pozos que lo usan y no se ofrece para nuevos
- **Verifica:** HU-03 · RN-13 · SPEC-001 §3.3 `CargarListas`, §5.3
- **Tipo / prioridad:** E2E · Media
- **Precondiciones:** Vereda X con `2*3` y TS-G. Después, TS-G marcado como no disponible.
- **Pasos:** 1. Editar X. 2. Agregar `+(1*1)` y abrir el desplegable del Pozo 2. 3. Abrir «Agregar vereda» y un desplegable.
- **Datos:** —
- **Resultado esperado:** 1. Pozo 1 muestra seleccionado `Granítico (40x40) (no disponible)`. 2. y 3. Los desplegables no incluyen Granítico.

### 2.5 SPEC-001 · HU-04 — Cordón con casilla

#### CP-111 — La casilla muestra y oculta los campos del cordón
- **Verifica:** HU-04 (crit. 1) · RF-VER-08 · D-15 · SPEC-001 §3.5, §3.6.3
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. Abrir «Agregar vereda». 2. Marcar «Cordón». 3. Desmarcarla.
- **Datos:** —
- **Resultado esperado:** 1. «Cordón» sin marcar; no se ven «Medición cordón» ni «Total cordón». 2. Aparecen «Medición cordón» (placeholder `(5*0,15*0,30)`), la ayuda `Cada rotura lleva largo × ancho × alto, en metros.` y «Total cordón». 3. Se ocultan de nuevo.

#### CP-112 — Total del cordón al instante
- **Verifica:** HU-04 (crit. 2) · RN-06 · RNF-03
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Marcar «Cordón» y escribir `(5*0,15*0,30)`.
- **Datos:** —
- **Resultado esperado:** «Total cordón» `0,225 m³`. No aparecen renglones de pozos para el cordón.

#### CP-113 — Cordón con 2 medidas: aviso y bloqueo
- **Verifica:** HU-04 (crit. 3) · RN-17 · RNF-03
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Marcar «Cordón», escribir `(5*0,15)` y «Guardar».
- **Datos:** demás campos completos, sin medición de vereda.
- **Resultado esperado:** Se ve `El término 1 («(5*0,15)») tiene 2 medidas y lleva 3.`; «Total cordón» `—`; no se envía y aparece `Revisá la medición y los tipos de suelo marcados en rojo antes de guardar.`

#### CP-114 — El servidor rechaza el cordón mal escrito
- **Verifica:** HU-04 (crit. 3) · RNF-07
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST Create con `TieneCordon=true` y `MedicionCordon=(5*0,15)`.
- **Datos:** calle única.
- **Resultado esperado:** 200 con `El término 1 («(5*0,15)») tiene 2 medidas y lleva 3.`; no se guarda.

#### CP-115 — Cordón en la base; sin casilla, se descarta
- **Verifica:** HU-04 · RF-VER-08 · RNF-18 · SPEC-004 `Cordon_SeGuardaEnVeredas`
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** a) POST Create con `TieneCordon=true`, `MedicionCordon=1*0,5*0,3`, sin medición. b) POST Create con `TieneCordon=false` y `MedicionCordon=1*0,5*0,3`.
- **Datos:** calles únicas.
- **Resultado esperado:** a) 302; `TieneCordon` 1, `MedicionCordon` `(1*0,5*0,3)`, `TotalCordonM3` 0.150, 0 filas en `Roturas`. b) 302; `TieneCordon` 0, `MedicionCordon` `NULL`, `TotalCordonM3` `NULL`.

#### CP-116 — Destildar el cordón limpia lo guardado
- **Verifica:** HU-04 · RF-VER-08 · SPEC-001 §3.2 (`AplicarAVereda` paso 1), §3.6.3–3.6.4
- **Tipo / prioridad:** E2E · Alta
- **Precondiciones:** Vereda X guardada con cordón `(5*0,15*0,30)`.
- **Pasos:** 1. Editar X, desmarcar «Cordón» y guardar. 2. Abrir el detalle. 3. Editar X otra vez y marcar «Cordón». 4. Aparte: en una vereda nueva, marcar «Cordón», escribir `(5*0,15)`, desmarcar y guardar.
- **Datos:** —
- **Resultado esperado:** 1. Se guarda (en la base `TieneCordon` 0, `MedicionCordon` y `TotalCordonM3` `NULL`). 2. El detalle no tiene la fila «Cordón». 3. La casilla viene desmarcada y, al marcarla, «Medición cordón» está vacía y «Total cordón» `—`. 4. El cordón oculto con error no bloquea el envío: se guarda sin cordón.

#### CP-117 — Ocultar el cordón no borra lo escrito en pantalla
- **Verifica:** HU-04 · SPEC-001 §3.6.3
- **Tipo / prioridad:** E2E · Baja
- **Pasos:** Marcar «Cordón», escribir `(5*0,15*0,30)`, desmarcar y volver a marcar.
- **Datos:** —
- **Resultado esperado:** «Medición cordón» sigue con `(5*0,15*0,30)` y «Total cordón» `0,225 m³`.

#### CP-118 — Vereda con medición y cordón: se conservan al editar
- **Verifica:** HU-04 (crit. 4) · RF-VER-08 · D-15
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. Guardar `(2*3)+(5*9)` con TS-V y TS-C y cordón `(5*0,15*0,30)`. 2. Abrir «Editar».
- **Datos:** calle `QA Cordón`.
- **Resultado esperado:** «Cordón» marcada con su bloque visible; «Medición» `(2*3)+(5*9)` con sus tipos y `51,00 m²`; «Medición cordón» `(5*0,15*0,30)` y `0,225 m³`.

#### CP-119 — Cordón marcado sin medición
- **Verifica:** HU-04 · SPEC-001 §4.2
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST Create con `TieneCordon=true`, `MedicionCordon=` vacío y sin medición.
- **Datos:** calle única.
- **Resultado esperado:** 302; `TieneCordon` 1, `TotalCordonM3` `NULL`, `TotalM2` `NULL`. En el listado figura «Sin medir».

### 2.6 SPEC-001 · HU-05 — Alta rápida de tipo de suelo y catálogo

#### CP-120 — Alta rápida: queda elegido en el pozo que lo pidió
- **Verifica:** HU-05 (crit. 1) · RF-VER-06 · D-04 · SPEC-001 §3.5, §3.6.5
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. `(2*3)+(5*9)` con TS-V en el Pozo 1. 2. «+ Nuevo» del Pozo 2. 3. Completar y «Agregar».
- **Datos:** Nombre `Mosaico calcáreo`, Medida de la baldosa `20x20`.
- **Resultado esperado:** 2. Se abre el modal «Nuevo tipo de suelo» con «Nombre», «Medida de la baldosa» (placeholder `40x40`), «Cancelar» y «Agregar». 3. El modal se cierra y sus campos quedan vacíos; Pozo 2 tiene seleccionado `Mosaico calcáreo (20x20)`; Pozo 1 sigue con `Vainilla (0,20x0,20)`.

#### CP-121 — Lo cargado no se pierde
- **Verifica:** HU-05 (crit. 2) · RF-VER-06
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. Completar calle, altura, entre calles, pin en el mapa, fechas, 2 fotos, estado, prioridad, observación, `(2*3)+(5*9)` con TS-V en el Pozo 1 y cordón `(5*0,15*0,30)`. 2. Alta rápida desde el Pozo 2 (`Baldosa QA` / `30x30`). 3. Guardar.
- **Datos:** los del paso.
- **Resultado esperado:** 2. La página no se recarga; todos los campos siguen iguales, el selector de fotos sigue con 2 archivos, Pozo 1 sigue con TS-V. 3. La vereda se guarda con las 2 fotos, los datos, 2 pozos (TS-V y `Baldosa QA (30x30)`) y el cordón.

#### CP-122 — El tipo nuevo aparece en todos los desplegables, ordenado y sin duplicar
- **Verifica:** HU-05 (crit. 3) · SPEC-001 §3.6.5, §5.3
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Después de CP-120: 1. Abrir el desplegable del Pozo 1. 2. Agregar `+(1*1)` y abrir el del Pozo 3.
- **Datos:** —
- **Resultado esperado:** En los dos aparece una sola vez `Mosaico calcáreo (20x20)`, en orden alfabético (después de `Granítico (40x40)` y antes de `Vainilla (0,20x0,20)`), sin seleccionar. Pozo 1 sigue con TS-V.

#### CP-123 — El tipo agregado figura en el catálogo
- **Verifica:** HU-05 (crit. 4) · RF-TSU-01
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Después de CP-120, abrir Tipos de suelo.
- **Datos:** —
- **Resultado esperado:** Figura `Mosaico calcáreo` con medida `20x20`, disponible y sin color.

#### CP-124 — Tipo repetido y disponible: se elige el existente
- **Verifica:** HU-05 · SPEC-001 §3.3 `CrearRapido`, §4.3
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** «+ Nuevo» de un pozo y agregar un tipo igual a TS-V con otras mayúsculas.
- **Datos:** Nombre `vainilla`, Medida `0,20X0,20`.
- **Resultado esperado:** El modal se cierra; el pozo queda con `Vainilla (0,20x0,20)` y muestra `Ese tipo de suelo ya estaba en la lista: quedó elegido.` La opción no se duplica y el catálogo no tiene un tipo nuevo.

#### CP-125 — Tipo repetido y no disponible: error en el modal
- **Verifica:** HU-05 · RN-13 · SPEC-001 §4.3
- **Tipo / prioridad:** E2E · Media
- **Precondiciones:** TS-G marcado como no disponible.
- **Pasos:** Alta rápida con `Granítico` / `40x40`.
- **Datos:** —
- **Resultado esperado:** El modal sigue abierto con `«Granítico (40x40)» ya existe pero está marcado como no disponible. Activalo desde Tipos de suelo.`; no se crea ningún tipo y el pozo no cambia.

#### CP-126 — Validaciones del alta rápida (servidor)
- **Verifica:** HU-05 · RF-TSU-01 · RNF-07 · SPEC-001 §2.3, §3.3, §4.3
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST `/TipoSuelos/CrearRapido` con token y cada dato.
- **Datos:** a) `Tipo=` vacío · b) `Tipo` de 101 caracteres · c) `Tipo=Laja`, `Medida` de 21 caracteres · d) `Tipo` de 100 caracteres y `Medida` de 20.
- **Resultado esperado:** a) 400 `{ errores: ["El tipo es obligatorio."] }` · b) 400 con `El tipo puede tener hasta 100 caracteres.` · c) 400 con `La medida puede tener hasta 20 caracteres.` · d) 200 `{ id, texto }`. En a–c no se crea ningún tipo.

#### CP-127 — Error de validación visible en el modal
- **Verifica:** HU-05 · SPEC-001 §3.6.5
- **Tipo / prioridad:** E2E · Media
- **Pasos:** «+ Nuevo» y «Agregar» con el nombre vacío.
- **Datos:** —
- **Resultado esperado:** El modal sigue abierto y muestra `El tipo es obligatorio.`; el formulario de la vereda no cambia.

#### CP-128 — Respuestas del alta rápida y recorte de espacios
- **Verifica:** HU-05 · SPEC-001 §3.3 `CrearRapido`
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST `CrearRapido` con cada dato.
- **Datos:** a) `Tipo=Mosaico QA`, `Medida=20x20` · b) de nuevo `Tipo=mosaico qa`, `Medida=20X20` · c) `Tipo=  Laja  `, `Medida=   `
- **Resultado esperado:** a) 200 `{ id, texto: "Mosaico QA (20x20)" }`; en la base `Disponible` 1 y `Color` `NULL`. b) 200 `{ id: <el de a>, texto: "Mosaico QA (20x20)", existente: true }` y no hay fila nueva. c) 200 con `texto: "Laja"`; en la base `Tipo` `Laja` y `Medida` `NULL`.

#### CP-129 — Alta rápida sin token
- **Verifica:** HU-05 · RNF-07
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST `CrearRapido` con sesión y sin token.
- **Datos:** `Tipo=QA sin token`.
- **Resultado esperado:** 400; no se crea el tipo.

#### CP-130 — Sesión vencida durante el alta rápida
- **Verifica:** HU-05 · RF-ACC-03 · SPEC-001 §3.6.5, §4.3
- **Tipo / prioridad:** E2E · Media
- **Pasos:** 1. Cargar datos y medición en «Agregar vereda». 2. En otra pestaña, «Cerrar sesión». 3. Volver y hacer un alta rápida.
- **Datos:** `Tipo QA vencida`.
- **Resultado esperado:** El modal muestra `No se pudo agregar el tipo de suelo porque se venció la sesión. Iniciá sesión en otra pestaña y volvé a probar; lo que cargaste acá no se pierde.`; la página no se recarga y los datos siguen. No se crea el tipo.

#### CP-131 — Otro error de red en el alta rápida
- **Verifica:** HU-05 · SPEC-001 §4.3
- **Tipo / prioridad:** E2E · Baja
- **Pasos:** Con las herramientas de desarrollo en «Offline», hacer un alta rápida.
- **Datos:** —
- **Resultado esperado:** El modal muestra `No se pudo agregar el tipo de suelo. Probá de nuevo.`; el formulario no cambia.

#### CP-132 — «Agregar» se deshabilita mientras espera
- **Verifica:** HU-05 · SPEC-001 §3.6.5
- **Tipo / prioridad:** E2E · Baja
- **Pasos:** Con la red limitada («Slow 3G»), hacer clic en «Agregar».
- **Datos:** —
- **Resultado esperado:** «Agregar» queda deshabilitado hasta la respuesta; un doble clic no crea dos tipos.

#### CP-133 — Cancelar el modal
- **Verifica:** HU-05
- **Tipo / prioridad:** E2E · Baja
- **Pasos:** Abrir el modal, escribir `QA cancelado` y «Cancelar».
- **Datos:** —
- **Resultado esperado:** El modal se cierra, el pozo no cambia y no existe el tipo `QA cancelado`.

#### CP-134 — Eliminar un tipo en uso: aviso (criterio RN-13)
- **Verifica:** RN-13 · RF-TSU-01 · SPEC-001 §3.3, §3.5, §4.3 · Q1
- **Tipo / prioridad:** E2E · Alta
- **Precondiciones:** TS-G usado por 2 veredas: una con 2 pozos de TS-G y otra con 1.
- **Pasos:** Tipos de suelo → «Eliminar» en Granítico.
- **Datos:** —
- **Resultado esperado:** Alerta `Este tipo de suelo lo usan 2 vereda(s), así que no se puede eliminar. Si ya no se consigue, marcalo como no disponible.` (cuenta veredas, no pozos). El botón dice «Marcar como no disponible». Ver O-12.

#### CP-135 — Confirmar sobre un tipo en uso: se marca no disponible
- **Verifica:** RN-13 · SPEC-001 §3.3 `DeleteConfirmed`, §4.3 · SPEC-004 `TipoSueloEnUso_NoSeBorra`
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** las de CP-134.
- **Pasos:** POST `/TipoSuelos/Delete/{G}` con token; seguir la redirección.
- **Datos:** —
- **Resultado esperado:** 302 a `/TipoSuelos`. TS-G existe con `Disponible` 0 y las 3 roturas siguen con `TipoSueloId` G. El Index muestra `«Granítico (40x40)» lo usan 2 vereda(s): no se eliminó y quedó como no disponible.`

#### CP-136 — Eliminar un tipo sin uso
- **Verifica:** RF-TSU-01 · RN-13
- **Tipo / prioridad:** Integración · Media
- **Pasos:** Crear `QA sin uso` y hacer POST Delete.
- **Datos:** —
- **Resultado esperado:** 302 a `/TipoSuelos`; el tipo ya no existe.

#### CP-137 — Se vuelve a contar al confirmar
- **Verifica:** RN-13 · SPEC-001 §3.3
- **Tipo / prioridad:** E2E · Media
- **Pasos:** 1. Abrir «Eliminar» de un tipo sin uso (`QA recuento`). 2. En otra pestaña, guardar una vereda con un pozo de ese tipo. 3. Confirmar en la primera.
- **Datos:** —
- **Resultado esperado:** No se borra; queda no disponible y se ve `«QA recuento» lo usan 1 vereda(s): no se eliminó y quedó como no disponible.`

#### CP-138 — La FK impide borrar un tipo en uso directo en la base
- **Verifica:** RN-13 · SPEC-001 §2.4, §5.3
- **Tipo / prioridad:** Integración · Media
- **Pasos:** `DELETE FROM TiposSuelo WHERE Id = G` con TS-G en uso.
- **Datos:** —
- **Resultado esperado:** `SqlException` 547; TS-G sigue existiendo.

### 2.7 SPEC-001 · HU-06 — Filtro y totales con la medición nueva

- **Precondiciones comunes (datos HU-06):** base de pruebas vacía de veredas, más:
  - F1 `(2*3)+(5*9)+(0,5*0,5)` con TS-V, TS-C, TS-V y cordón `(5*0,15*0,30)`.
  - F2 `3*2` con TS-C, sin cordón.
  - F3 solo cordón `1*0,5*0,3`.
  - F4 sin medición.
  - F5 «Cordón» marcado y vacío, sin medición.

#### CP-139 — Filtro medidas / sin medir
- **Verifica:** HU-06 (crit. 1) · RF-VER-10 · RN-09 · D-06 · SPEC-001 §3.5 (Index)
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** En Veredas, elegir «Medidas» y después «Sin medir».
- **Datos:** F1–F5.
- **Resultado esperado:** «Medidas»: F1, F2 y F3 (el contador de medidas dice 3). «Sin medir»: F4 y F5.

#### CP-140 — Columna Medición del listado
- **Verifica:** HU-06 · RF-VER-10 · SPEC-001 §3.5 (Index)
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Mirar la columna «Medición».
- **Datos:** F1–F5.
- **Resultado esperado:** F1: insignia «Medida» y `51,25 m² · 0,225 m³`. F2: «Medida» y `6,00 m²`. F3: «Medida» y `0,150 m³`. F4 y F5: «Sin medir». Ninguna muestra metros lineales.

#### CP-141 — Buscador y filtro por estado siguen funcionando
- **Verifica:** HU-06 (crit. 2) · RF-VER-10
- **Tipo / prioridad:** E2E · Media
- **Precondiciones:** F1 en estado «En proceso», los demás «Sin definir».
- **Pasos:** 1. Buscar `QA F1`. 2. Filtrar estado «En proceso». 3. Combinar «En proceso» con «Sin medir».
- **Datos:** —
- **Resultado esperado:** 1. Solo F1. 2. Solo F1. 3. Ninguna, con el estado vacío del listado.

#### CP-142 — Detalle con pozos, total y cordón
- **Verifica:** HU-06 (crit. 3) · RF-VER-12, RF-VER-17 · D-11 · SPEC-001 §3.5 (Details)
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Abrir el detalle de F1.
- **Datos:** F1.
- **Resultado esperado:** La foto sigue como protagonista. En el panel, la sección «Medición» está después de «Reparación» y no hay fila «Superficie medida». Filas: «Pozo 1» `6,00 m²` con `2 × 3 · Vainilla (0,20x0,20)`; «Pozo 2» `45,00 m²` con `5 × 9 · Cemento alisado`; «Pozo 3» `0,25 m²` con `0,5 × 0,5 · Vainilla (0,20x0,20)`; «Total m²» `51,25 m²` en negrita; «Cordón» `0,225 m³` con `(5*0,15*0,30)` debajo.

#### CP-143 — Detalle sin medir y solo cordón
- **Verifica:** HU-06 (crit. 4) · RF-VER-17 · RN-09
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Abrir el detalle de F4 y de F3.
- **Datos:** F3, F4.
- **Resultado esperado:** F4: fila «Estado de medición» → «Sin medir», sin filas de pozos. F3: sin filas de pozos; «Total m²» → «Sin medir»; «Cordón» → `0,150 m³` con `(1*0,5*0,3)`.

#### CP-144 — Home: resumen con la medición nueva
- **Verifica:** HU-06 (crit. 5) · RF-HOM-01 · SPEC-001 §3.3, §3.5 (Home) · ver O-05
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. Abrir el Home. 2. Quitar el cordón de F1 y de F3 (destildar) y volver al Home.
- **Datos:** F1–F5 (m² = 51,25 + 6,00 = 57,25; m³ = 0,225 + 0,150 = 0,375).
- **Resultado esperado:** 1. El subtítulo dice `veredas cargadas · 57,25 m² medidos · 0,375 m³ de cordón` (precedido por la cantidad de veredas, 5). Siguen los totales por estado y con/sin paquete como antes. 2. Dice `veredas cargadas · 57,25 m² medidos`, sin la parte de cordón.

#### CP-145 — Home / Veredas: superficie, pozos e insignias
- **Verifica:** HU-06 · RF-HOM-01 · SPEC-001 §3.5 (Home/Veredas)
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Abrir la vista de veredas del Home.
- **Datos:** F1, F2, F4.
- **Resultado esperado:** F1: `51,25 m²` (y los m³), `(3 pozos)` e insignias `Vainilla` y `Cemento alisado` (2, no 3). F2: `6,00 m²`, `(1 pozo)`, insignia `Cemento alisado`. F4: «Sin medir».

#### CP-146 — Home / Paquetes: superficie del paquete
- **Verifica:** HU-06 · RF-HOM-01 · SPEC-001 §3.5 (Home/Paquetes)
- **Tipo / prioridad:** E2E · Media
- **Precondiciones:** Paquete `QA Home` con F1, F2 y F4.
- **Pasos:** Abrir la vista de paquetes del Home.
- **Datos:** —
- **Resultado esperado:** El mini-stat de superficie de `QA Home` dice `57,25` m².

#### CP-147 — Los totales se actualizan al editar la medición
- **Verifica:** HU-06 · RF-VER-13, RF-VER-17
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Editar F2 a `4*5` y ver listado, detalle y Home.
- **Datos:** —
- **Resultado esperado:** Listado y detalle de F2: `20,00 m²`; Home: `71,25 m² medidos` (51,25 + 20,00).

### 2.8 SPEC-002 · HU-07 — Asignar varias veredas desde el paquete

- **Precondiciones comunes (datos PAQ):** PR creado. Veredas sin paquete: A (`51,25 m²`, Finalizado), B (sin medir, Falta medir), C (`6,00 m²` y cordón `0,225 m³`, En proceso), E (`20,00 m²` y cordón `0,225 m³`, Sin definir). D en el paquete `QA Abril` (fecha 01/04/2026).

#### CP-148 — Crear un paquete lleva a agregar veredas
- **Verifica:** HU-07 · RF-PAQ-01, RF-PAQ-02 · D-20 · SPEC-002 §4.2, §5
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Paquetes → nuevo: `QA Mayo`, fecha 01/05/2026, proveedor PR → guardar.
- **Datos:** —
- **Resultado esperado:** Se abre «Agregar veredas a «QA Mayo»» (`/Paquetes/AgregarVeredas/{id}`) con el mensaje `Se creó el paquete «QA Mayo». Ahora elegí las veredas que van en él.`

#### CP-149 — Se ofrecen solo las veredas sin paquete, con estado y medición
- **Verifica:** HU-07 (crit. 1 y 3) · RF-PAQ-02 · RN-10 · P-13 (propuesta) · SPEC-002 §4.4
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Mirar la tabla de «Agregar veredas» de `QA Mayo`.
- **Datos:** datos PAQ.
- **Resultado esperado:** Filas A, B, C y E (incluida A, que está Finalizado); D no aparece. Columnas: casilla, dirección y entre calles, estado, prioridad, medición (A «Medida» `51,25 m²`; B «Sin medir»; C «Medida» `6,00 m²` y `0,225 m³`) y fecha de reclamo. No hay fotos.

#### CP-150 — Asignar varias de una vez
- **Verifica:** HU-07 (crit. 2) · RF-PAQ-02 · D-20 · SPEC-002 §5
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Marcar A, B y C y «Agregar al paquete».
- **Datos:** —
- **Resultado esperado:** Antes de enviar, el pie dice `3 seleccionadas`. Se abre el detalle de `QA Mayo` con `Se agregaron 3 veredas al paquete «QA Mayo».` y las 3 veredas en la tabla. En la base, las 3 tienen `PaqueteId` de `QA Mayo`.

#### CP-151 — Asignar una sola
- **Verifica:** HU-07 · SPEC-002 §5
- **Tipo / prioridad:** E2E · Media
- **Pasos:** En «Agregar veredas» de `QA Mayo`, marcar E y confirmar.
- **Datos:** —
- **Resultado esperado:** `Se agregó 1 vereda al paquete «QA Mayo».`

#### CP-152 — Confirmar sin veredas marcadas
- **Verifica:** HU-07 · RNF-07 · SPEC-002 §4.2, §5
- **Tipo / prioridad:** Integración · Media
- **Pasos:** 1. GET `AgregarVeredas/{id}`: verificar el botón. 2. POST `AgregarVeredas/{id}` con token y sin `veredaIds`.
- **Datos:** —
- **Resultado esperado:** 1. El botón «Agregar al paquete» está `disabled`. 2. 200 con `Elegí al menos una vereda.` y la lista de veredas recargada; ningún `PaqueteId` cambia.

#### CP-153 — Filtros, «Seleccionar todas» y contador
- **Verifica:** HU-07 · SPEC-002 §4.4, §4.5, §6.7
- **Tipo / prioridad:** E2E · Media
- **Pasos:** 1. Marcar B. 2. Filtrar «Medidas» y marcar «Seleccionar todas». 3. Quitar el filtro. 4. Buscar `zzz`.
- **Datos:** datos PAQ (A, B, C, E libres).
- **Resultado esperado:** 2. Se marcan A, C y E (visibles); el contador dice 4 (B sigue marcada aunque oculta). El desplegable de medición dice «Medidas (3)» y «Sin medir (1)». 3. B, A, C y E marcadas; «Seleccionar todas» marcada. Con algunas desmarcadas queda en estado indeterminado. 4. `No hay veredas que coincidan con la búsqueda.`

#### CP-154 — La fila entera marca la casilla
- **Verifica:** HU-07 · SPEC-002 §4.4
- **Tipo / prioridad:** E2E · Baja
- **Pasos:** Hacer clic en la dirección de una fila (fuera de la casilla).
- **Datos:** —
- **Resultado esperado:** La casilla se marca y el contador sube en 1.

#### CP-155 — No hay veredas libres
- **Verifica:** HU-07 · SPEC-002 §5
- **Tipo / prioridad:** E2E · Media
- **Precondiciones:** Todas las veredas tienen paquete.
- **Pasos:** Abrir «Agregar veredas» de un paquete.
- **Datos:** —
- **Resultado esperado:** `No hay veredas sin paquete. Podés cargar una nueva desde Veredas.` con los enlaces «+ Nueva vereda» y «Volver al paquete».

#### CP-156 — Una vereda recién guardada sin paquete queda disponible
- **Verifica:** HU-07 (crit. 4) · RN-18 · D-20
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. Guardar una vereda nueva `QA Nueva` sin elegir paquete. 2. Abrir «Agregar veredas» de `QA Mayo`.
- **Datos:** —
- **Resultado esperado:** 1. Se guarda sin error (`PaqueteId` `NULL`). 2. `QA Nueva` figura en la lista.

#### CP-157 — Concurrencia: la misma vereda a dos paquetes a la vez (servicio)
- **Verifica:** HU-07 · RN-10 · SPEC-002 §3.2, §6.1, §7
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Vereda X libre; paquetes P1 y P2.
- **Pasos:** Con dos `DbContext` distintos, llamar a la vez `AgregarVeredasAsync(P1, [X])` y `AgregarVeredasAsync(P2, [X])`.
- **Datos:** —
- **Resultado esperado:** X queda con `PaqueteId` P1 o P2, uno solo. Un resultado tiene `Agregadas` 1; el otro `Agregadas` 0 y `Salteadas` con la dirección de X y el nombre del paquete que ganó.

#### CP-158 — Concurrencia en el navegador (dos pestañas)
- **Verifica:** HU-07 · RN-10 · SPEC-002 §5, §6.1
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. Pestaña 1: «Agregar veredas» de `QA Mayo`; pestaña 2: de `QA Junio`. 2. Marcar X en las dos. 3. Confirmar en la 1 y después en la 2.
- **Datos:** X libre.
- **Resultado esperado:** Pestaña 2: detalle de `QA Junio` con el aviso `No se agregó {Dirección de X} porque ya está en el paquete «QA Mayo».` y sin mensaje de éxito. X solo está en `QA Mayo`.

#### CP-159 — Ya estaba en este paquete
- **Verifica:** HU-07 · SPEC-002 §5, §6.2
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Dos pestañas en «Agregar veredas» de `QA Mayo`; marcar X y confirmar en las dos.
- **Datos:** —
- **Resultado esperado:** La segunda muestra el aviso `{Dirección de X} ya estaba en este paquete.` sin error.

#### CP-160 — Veredas que se borraron mientras la lista estaba abierta
- **Verifica:** HU-07 · SPEC-002 §5, §6.3
- **Tipo / prioridad:** E2E · Media
- **Pasos:** 1. Abrir «Agregar veredas». 2. En otra pestaña, eliminar la vereda Y (y en otra corrida, Y y Z). 3. Marcar Y (o Y y Z) y otra vereda libre, y confirmar.
- **Datos:** —
- **Resultado esperado:** `Se agregó 1 vereda al paquete «…».` y el aviso `Una de las veredas que elegiste ya no existe.` (con dos: `2 de las veredas que elegiste ya no existen.`).

#### CP-161 — IDs manipulados en el POST
- **Verifica:** HU-07 · RNF-07 · SPEC-002 §3.2, §6.6
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST `AgregarVeredas/{P}` con token y `veredaIds` = A, A, -1, 0, 999999, D.
- **Datos:** A libre; D en `QA Abril`.
- **Resultado esperado:** 302 al detalle, nunca 500. A queda en P (una vez); D sigue en `QA Abril`. Mensajes: `Se agregó 1 vereda al paquete «…».` y un aviso con `No se agregó {Dirección de D} porque ya está en el paquete «QA Abril».` y `3 de las veredas que elegiste ya no existen.`

#### CP-162 — Salteadas: se listan hasta 10
- **Verifica:** HU-07 · SPEC-002 §5 · ver O-08
- **Tipo / prioridad:** Integración · Baja
- **Pasos:** POST `AgregarVeredas/{P}` con 12 veredas que están en `QA Abril`.
- **Datos:** —
- **Resultado esperado:** Sin mensaje de éxito. El aviso empieza con `No se agregaron 12 veredas porque ya están en otro paquete:`, lista 10 direcciones con `(«QA Abril»)` y termina con `y 2 más`.

#### CP-163 — Más de 1.000 ids
- **Verifica:** HU-07 · SPEC-002 §3.2
- **Tipo / prioridad:** Integración · Baja
- **Pasos:** POST `AgregarVeredas/{P}` con 1.001 ids.
- **Datos:** —
- **Resultado esperado:** 400; ningún `PaqueteId` cambia.

#### CP-164 — Paquete inexistente
- **Verifica:** HU-07 · SPEC-002 §4.2, §5, §6.4
- **Tipo / prioridad:** Integración · Media
- **Pasos:** GET y POST (con token) `/Paquetes/AgregarVeredas/999999`.
- **Datos:** —
- **Resultado esperado:** 404 en los dos; ninguna vereda cambia.

#### CP-165 — Paquete eliminado mientras se asigna
- **Verifica:** HU-07 · SPEC-002 §6.4
- **Tipo / prioridad:** E2E · Media
- **Pasos:** 1. Abrir «Agregar veredas» de `QA Borrar`. 2. En otra pestaña, eliminar `QA Borrar`. 3. Marcar A y confirmar.
- **Datos:** —
- **Resultado esperado:** Página 404; A sigue sin paquete.

#### CP-166 — POST de asignación sin token
- **Verifica:** HU-07 · RNF-07 · SPEC-002 §7
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST `AgregarVeredas/{P}` con sesión y sin token.
- **Datos:** `veredaIds` = A.
- **Resultado esperado:** 400; A sigue sin paquete.

### 2.9 SPEC-002 · HU-08 — Quitar, totales, editar y eliminar paquete

#### CP-167 — Detalle: cantidad, m², m³, avance y sin medir
- **Verifica:** HU-08 (crit. 2 y 3) · RF-PAQ-04 · P-14 · SPEC-002 §3.3, §4.4
- **Tipo / prioridad:** E2E · Alta
- **Precondiciones:** `QA Mayo` con A, B, C y E.
- **Pasos:** Abrir el detalle.
- **Datos:** datos PAQ.
- **Resultado esperado:** Tarjetas: «Veredas» `4`; «m²» `77,25 m²`; «m³» `0,450 m³`; «Avance» `25 %` con barra y `1 de 4 finalizadas`. Texto `1 sin medir: no suman a los totales`. Encabezado con nombre, fecha `01/05/2026`, proveedor y botones «Agregar veredas», «Editar», «Eliminar» y «← Paquetes».

#### CP-168 — `CalcularTotales`
- **Verifica:** HU-08 · RF-PAQ-04 · SPEC-002 §3.3, §7
- **Tipo / prioridad:** Unitaria · Alta
- **Pasos:** Llamar `PaqueteService.CalcularTotales` con cada lista.
- **Datos:** a) 4 veredas, 1 Finalizado · b) 0 veredas · c) 3 veredas, 1 Finalizado · d) 3 veredas, 2 Finalizado · e) A, B, C, E de los datos PAQ (m² 51,25 / null / 6,00 / 20,00; m³ null / null / 0,225 / 0,225).
- **Resultado esperado:** `PorcentajeAvance`: a) 25 · b) 0 (sin excepción) · c) 33 · d) 67. e) `Cantidad` 4, `TotalM2` 77,25, `TotalM3` 0,450, `SinMedir` 1, `Finalizadas` 1, `PorcentajeAvance` 25.

#### CP-169 — Paquete sin veredas
- **Verifica:** HU-08 · SPEC-002 §5, §6.8
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Abrir el detalle de un paquete sin veredas.
- **Datos:** —
- **Resultado esperado:** Estado vacío `Este paquete todavía no tiene veredas. Agregá las que van en él.` con el botón «Agregar veredas». Tarjetas: `0`, `0,00 m²`, `0,000 m³`, `0 %` con la barra vacía.

#### CP-170 — Tabla de veredas del paquete
- **Verifica:** HU-08 · RF-PAQ-04 · SPEC-002 §4.4
- **Tipo / prioridad:** E2E · Baja
- **Pasos:** Mirar la tabla del detalle de `QA Mayo`.
- **Datos:** —
- **Resultado esperado:** Ordenada por calle y altura; cada fila con miniatura, dirección como enlace a su detalle, estado, prioridad, medición y botón «Quitar».

#### CP-171 — Quitar una vereda
- **Verifica:** HU-08 (crit. 1 y 4) · RF-PAQ-03 · RN-18 · SPEC-002 §4.4, §5
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** 1. «Quitar» en C. 2. Aceptar la confirmación. 3. Abrir el detalle de C y «Agregar veredas» del paquete.
- **Datos:** —
- **Resultado esperado:** 1. Confirmación `¿Quitar {Dirección de C} de este paquete? La vereda no se borra: queda sin paquete.` 2. `Se quitó {Dirección de C} del paquete. Ya podés asignarla a otro.`; tarjetas `3`, `71,25 m²`, `0,225 m³`, `33 %`. 3. C conserva medición, pozos, cordón y fotos, sin paquete, y figura en «Agregar veredas».

#### CP-172 — Cancelar la confirmación de quitar
- **Verifica:** HU-08 · RF-PAQ-03
- **Tipo / prioridad:** E2E · Baja
- **Pasos:** «Quitar» en A y «Cancelar».
- **Datos:** —
- **Resultado esperado:** A sigue en el paquete; nada cambia.

#### CP-173 — Quitar dos veces y quitar sin token
- **Verifica:** HU-08 · RNF-07 · SPEC-002 §3.2, §5, §6.5, §7
- **Tipo / prioridad:** Integración · Media
- **Pasos:** 1. POST `QuitarVereda/{P}` con `veredaId` = A dos veces. 2. POST `QuitarVereda/{P}` con `veredaId` = B sin token.
- **Datos:** A y B en P.
- **Resultado esperado:** 1. Primero: `PaqueteId` de A `NULL`; segundo: 302 con el aviso `Esa vereda ya no estaba en este paquete.` 2. 400 y B sigue en P.

#### CP-174 — El avance cambia con el estado de una vereda
- **Verifica:** HU-08 (crit. 4) · RF-PAQ-04 · SPEC-002 §6.10
- **Tipo / prioridad:** E2E · Media
- **Precondiciones:** `QA Mayo` con A, B, C, E (25 %).
- **Pasos:** Editar B a «Finalizado» y abrir el detalle del paquete.
- **Datos:** —
- **Resultado esperado:** `50 %` y `2 de 4 finalizadas`.

#### CP-175 — Editar el paquete conserva sus veredas
- **Verifica:** HU-08 (crit. 5) · RF-PAQ-06
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Editar `QA Mayo` → nombre `QA Mayo bis` → guardar.
- **Datos:** —
- **Resultado esperado:** El detalle de `QA Mayo bis` sigue con sus 4 veredas y los mismos totales.

#### CP-176 — Confirmación de eliminar paquete
- **Verifica:** HU-08 (crit. 5) · RF-PAQ-06 · RN-11 · SPEC-002 §5
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** «Eliminar» en `QA Mayo` (4 veredas) y en un paquete sin veredas `QA Vacío`.
- **Datos:** —
- **Resultado esperado:** `QA Mayo`: `Este paquete tiene 4 veredas. No se borran: quedan sin paquete.` y `¿Seguro que querés eliminar el paquete «QA Mayo»?` `QA Vacío`: `Este paquete no tiene veredas.` y la pregunta con «QA Vacío».

#### CP-177 — Eliminar el paquete deja las veredas sin paquete
- **Verifica:** HU-08 (crit. 5) · RF-PAQ-06 · RN-11 · SPEC-002 §5, §6.13
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Confirmar la eliminación de `QA Mayo`; abrir «Agregar veredas» de otro paquete.
- **Datos:** —
- **Resultado esperado:** Listado de paquetes con `Se eliminó el paquete «QA Mayo». Sus veredas quedaron sin paquete.` A, B, C y E siguen existiendo con todos sus datos y aparecen en «Agregar veredas».

#### CP-178 — Eliminar el paquete en la base
- **Verifica:** RF-PAQ-06 · RN-11 · SPEC-002 §2, §7
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** POST `/Paquetes/Delete/{P}` con token, con P con 3 veredas.
- **Datos:** —
- **Resultado esperado:** 302 a `/Paquetes`. P no existe; las 3 veredas existen con `PaqueteId` `NULL` y sus `Roturas` intactas.

#### CP-179 — Listado de paquetes
- **Verifica:** RF-PAQ-05 · SPEC-002 §4.4
- **Tipo / prioridad:** E2E · Baja
- **Pasos:** Abrir Paquetes.
- **Datos:** `QA Abril` con 1 vereda.
- **Resultado esperado:** Columna «Veredas» con `1` en `QA Abril` y botón «Agregar veredas» por fila. Textos en español: no aparecen «Details», «Edit», «Delete» ni «Back to List» en las vistas de Paquetes.

#### CP-180 — Cambiar el paquete desde la edición de la vereda
- **Verifica:** RN-18 · SPEC-002 §4.3, §6.11 · Q-05
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Editar D (en `QA Abril`), elegir `QA Junio` en «Paquete» y guardar.
- **Datos:** —
- **Resultado esperado:** Se guarda; D figura en el detalle de `QA Junio` y ya no en el de `QA Abril`.

### 2.10 SPEC-002 · HU-09 — Agregar al paquete desde el listado (parte B)

#### CP-181 — Barra de acción y desplegable de paquetes
- **Verifica:** HU-09 · RF-PAQ-08 · SPEC-002 §4.3, §4.4
- **Tipo / prioridad:** E2E · Media
- **Pasos:** En Veredas, marcar 2 veredas libres.
- **Datos:** paquetes `QA Junio` (01/06/2026) y `QA Abril` (01/04/2026).
- **Resultado esperado:** Aparece la barra con `2 seleccionadas`, el desplegable con `Elegí un paquete…` primero y después `QA Junio (01/06/2026)` y `QA Abril (01/04/2026)` (fecha descendente), y el botón «Agregar al paquete…». Sin ninguna marcada, la barra no se ve.

#### CP-182 — Agregar desde el listado
- **Verifica:** HU-09 (crit. 1) · RF-PAQ-08 · SPEC-002 §5
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Marcar A y B, elegir `QA Junio`, «Agregar al paquete…».
- **Datos:** —
- **Resultado esperado:** Se vuelve a Veredas con `Se agregaron 2 veredas al paquete «QA Junio».` y el enlace «Ver paquete» al detalle de `QA Junio`. A y B tienen su casilla deshabilitada.

#### CP-183 — Veredas con paquete: casilla deshabilitada
- **Verifica:** HU-09 (crit. 2) · RN-10 · SPEC-002 §4.4
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Pasar el mouse sobre la casilla de D; marcar «Seleccionar todas».
- **Datos:** D en `QA Abril`.
- **Resultado esperado:** La casilla de D está deshabilitada con el título `Ya está en «QA Abril». Quitala de ese paquete para moverla.`; «Seleccionar todas» no la marca.

#### CP-184 — Vereda con paquete forzada en el HTML
- **Verifica:** HU-09 (crit. 2) · RN-10 · SPEC-002 §6.12
- **Tipo / prioridad:** E2E · Alta
- **Pasos:** Con las herramientas de desarrollo, habilitar y marcar la casilla de D; elegir `QA Junio` y enviar.
- **Datos:** —
- **Resultado esperado:** Aviso `No se agregó {Dirección de D} porque ya está en el paquete «QA Abril».`, sin mensaje de éxito. D sigue en `QA Abril`.

#### CP-185 — Sin paquete elegido
- **Verifica:** HU-09 · RNF-07 · SPEC-002 §4.5, §5
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST `/Veredas/AgregarAPaquete` con token, `veredaIds` = A y sin `paqueteId`.
- **Datos:** —
- **Resultado esperado:** 302 a `/Veredas` con `Elegí el paquete al que querés agregar las veredas.`; A sigue sin paquete. (En el navegador, el JS no deja enviar sin paquete.)

#### CP-186 — Paquete que ya no existe
- **Verifica:** HU-09 · SPEC-002 §5, §6.4
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST `/Veredas/AgregarAPaquete` con `paqueteId=999999` y `veredaIds` = A.
- **Datos:** —
- **Resultado esperado:** 302 a `/Veredas` con `El paquete que elegiste ya no existe. Elegí otro.`; A sigue sin paquete.

#### CP-187 — Todavía no hay paquetes
- **Verifica:** HU-09 · SPEC-002 §4.4
- **Tipo / prioridad:** E2E · Baja
- **Precondiciones:** No hay paquetes.
- **Pasos:** Marcar una vereda en el listado.
- **Datos:** —
- **Resultado esperado:** En lugar del desplegable: `Todavía no hay paquetes. Creá uno`, con «Creá uno» como enlace a `/Paquetes/Create`.

### 2.11 SPEC-003 · HU-10 — Contraseña de 8 a 50 caracteres

- **Datos de contraseña:** P7 = `Colon07` (7) · P8 = `Colon008` (8) · P50 = `Veredas-Colon-2026-prueba-de-contrasena-larga-ok50` (50) · P51 = P50 + `X` (51).

#### 2.11.1 Validación de los ViewModels (SPEC-003 §3.1, §7)

- **Precondiciones y pasos comunes:** armar el ViewModel y validar con `Validator.TryValidateObject(..., validateAllProperties: true)`.

| CP | Título | Tipo | Prio | Datos | Resultado esperado |
|---|---|---|---|---|---|
| CP-188 | Alta: límites de largo | Unitaria | Alta | `UsuarioCreateViewModel` con contraseña y confirmación iguales: P7, P8, P50, P51 | P8 y P50: sin errores de contraseña. P7 y P51: `La contraseña debe tener entre 8 y 50 caracteres.` |
| CP-189 | Alta: vacías y distintas | Unitaria | Alta | a) contraseña vacía · b) confirmación vacía · c) P8 y `Colon009` | a) `Escribí la contraseña.` · b) `Repetí la contraseña.` · c) `Las contraseñas no coinciden.` |
| CP-190 | Edición: límites y vacía | Unitaria | Alta | `UsuarioEditViewModel.NuevaContrasena`: vacía, P7, P8, P50, P51; y P8 con confirmación `Colon009` | Vacía, P8, P50: sin errores. P7, P51: `La contraseña debe tener entre 8 y 50 caracteres.` Distintas: `Las contraseñas no coinciden.` |
| CP-191 | Recuperación: límites, vacías y distintas | Unitaria | Alta | `RecoveryPasswordViewModel`: P7, P8, P50, P51; contraseña vacía; confirmación vacía; P8 y `Colon009` | P8, P50: sin errores. P7, P51: `La contraseña debe tener entre 8 y 50 caracteres.` Vacía: `Escribí la nueva contraseña.` Confirmación vacía: `Repetí la nueva contraseña.` Distintas: `Las contraseñas no coinciden.` |
| CP-192 | El login no aplica la regla de largo | Unitaria | Media | `LoginViewModel` con email válido y P7 | Sin errores de largo. |
| CP-193 | Los espacios cuentan | Unitaria | Media | `UsuarioCreateViewModel` con a) ` Colon6 ` (8, con espacios) · b) `Colon6 ` (7) | a) sin errores · b) `La contraseña debe tener entre 8 y 50 caracteres.` |

#### CP-194 — Alta de usuario: 51 no, 50 sí y entra
- **Verifica:** HU-10 (crit. 1, 2 y 3) · RN-16 · RF-ACC-01, RF-ACC-04 · SPEC-003 §5.1–5.2, §7
- **Tipo / prioridad:** Integración · Alta
- **Pasos:** 1. POST `/Usuarios/Create` con email `qa51@sistemaveredas.local` y P51. 2. POST con `qa50@sistemaveredas.local` y P50. 3. Con un cliente nuevo, login con `qa50@…` y P50; GET `/Veredas`.
- **Datos:** los del paso.
- **Resultado esperado:** 1. 200 con `La contraseña debe tener entre 8 y 50 caracteres.`; no existe `qa51@…`. 2. 302; existe `qa50@…` con `UsContrasena` de 44 caracteres. 3. Login 302 y `/Veredas` 200.

#### CP-195 — Edición de usuario: vacía mantiene, 7 no, 8 sí
- **Verifica:** HU-10 (crit. 1 y 2) · RN-16 · RF-ACC-04, RF-ACC-05 · SPEC-003 §5.5
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** Usuario `qa50@…` de CP-194.
- **Pasos:** 1. POST Edit con `NuevaContrasena` vacía; login con P50. 2. POST Edit con P7. 3. POST Edit con P8 y confirmación P8; login con P8 y con P50.
- **Datos:** —
- **Resultado esperado:** 1. 302 y el login con P50 entra. 2. 200 con `La contraseña debe tener entre 8 y 50 caracteres.`; el hash no cambia. 3. 302; el login con P8 entra y con P50 no (`Email o contraseña incorrectos.`).

#### CP-196 — Ayudas y validación en el navegador
- **Verifica:** HU-10 · SPEC-003 §3.1 (Vistas)
- **Tipo / prioridad:** E2E · Media
- **Pasos:** 1. Usuarios → nuevo: mirar la ayuda; pegar P51 en la contraseña y salir del campo. 2. Editar un usuario: mirar la ayuda.
- **Datos:** —
- **Resultado esperado:** 1. Ayuda `Entre 8 y 50 caracteres.`; el campo conserva los 51 caracteres (sin `maxlength`) y, antes de enviar, se ve `La contraseña debe tener entre 8 y 50 caracteres.` 2. Ayuda `Dejala vacía para no cambiarla. Entre 8 y 50 caracteres.`

#### CP-197 — Recuperación: misma regla y cambio correcto
- **Verifica:** HU-10 (crit. 4) · RN-16 · RF-ACC-06 · SPEC-003 §3.1, §4
- **Tipo / prioridad:** E2E · Media
- **Precondiciones:** En la base de pruebas, `UPDATE Usuarios SET token_recovery = 'tokenqa' WHERE UsEmail = 'qa50@sistemaveredas.local'`.
- **Pasos:** 1. Abrir `/Access/Recovery?token=tokenqa`. 2. Escribir P7 en las dos casillas. 3. Escribir P8 en las dos y enviar. 4. Iniciar sesión con P8. 5. Volver a abrir el enlace y enviar P8.
- **Datos:** —
- **Resultado esperado:** 1. El recuadro de requisitos tiene un solo ítem: `Entre 8 y 50 caracteres.` 2. Antes de enviar: `La contraseña debe tener entre 8 y 50 caracteres.` 3. Login con `Listo, cambiaste la contraseña. Ya podés iniciar sesión.` 4. Entra. 5. `El enlace no es válido. Pedí uno nuevo.` (el enlace sirve una sola vez).

#### CP-198 — Un usuario con contraseña vieja de menos de 8 sigue entrando
- **Verifica:** RN-16 · SPEC-003 §3.1, §5.6
- **Tipo / prioridad:** Integración · Baja
- **Precondiciones:** Fila de usuario insertada con `UsContrasena` = hash SHA-256 en Base64 de `abc123`.
- **Pasos:** Login con `abc123`.
- **Datos:** —
- **Resultado esperado:** 302 y GET `/Veredas` 200.

### 2.12 SPEC-003 · HU-11 — Credenciales SMTP fuera del código y textos

#### CP-199 — No hay credenciales en los archivos versionados
- **Verifica:** HU-11 (crit. 1) · RNF-06 · D-22 · R-01 · SPEC-003 §3.2, §6
- **Tipo / prioridad:** Manual · Alta
- **Pasos:** En la raíz: `git grep -n -i "casisantiagopablo"`, `git grep -n "NetworkCredential(\""`, `git grep -n "localhost:7054"`, `git grep -n "Sendemail"`.
- **Datos:** —
- **Resultado esperado:** Ninguno de los cuatro comandos devuelve resultados.

#### CP-200 — Configuración: solo lo no secreto se versiona
- **Verifica:** HU-11 (crit. 3) · RNF-06 · SPEC-003 §3.2
- **Tipo / prioridad:** Manual · Alta
- **Pasos:** 1. Abrir `appsettings.json`. 2. `git check-ignore appsettings.Production.json` y `git ls-files | findstr Production`. 3. Abrir `appsettings.Production.json` en la PC (sin copiar valores).
- **Datos:** —
- **Resultado esperado:** 1. La sección `Smtp` tiene solo `"Host": "smtp.gmail.com"` y `"Puerto": 587`. 2. `appsettings.Production.json` está ignorado y no figura en `git ls-files`. 3. Tiene la sección `Smtp` con las claves `Usuario`, `Contrasena` y `Remitente`.

#### CP-201 — SMTP sin configurar: mensaje y token bloqueado
- **Verifica:** HU-11 · RNF-06 · RF-ACC-06 · SPEC-003 §3.2, §4, §5.7, §7
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** `AppFactory` con `Smtp:Usuario` y `Smtp:Contrasena` vacíos (la app arranca igual).
- **Pasos:** POST `/Access/StartRecovery` con token y el email del usuario sembrado.
- **Datos:** —
- **Resultado esperado:** 200 (sin página de error) con `La recuperación por mail no está disponible en este momento. Pedile a otro usuario que te asigne una contraseña nueva desde Usuarios.` En la base, `token_recovery` = `tokenbloqueado`.

#### CP-202 — Mail enviado (servicio falso)
- **Verifica:** HU-11 · RF-ACC-06 · SPEC-003 §3.2, §5.9, §7
- **Tipo / prioridad:** Integración · Alta
- **Precondiciones:** `IEmailService` reemplazado por uno falso que devuelve `Enviado` y guarda `destino` y `enlace`.
- **Pasos:** POST `StartRecovery` con el email del usuario sembrado.
- **Datos:** —
- **Resultado esperado:** 302 a `/Access/Login`, que muestra `Te enviamos un mail con el enlace para elegir una contraseña nueva.` El falso recibió el email del usuario y un enlace `https://localhost/Access/Recovery?token=<t>` (dominio de la solicitud); en la base `token_recovery` = `<t>` (no `tokenbloqueado`).

#### CP-203 — Falla el envío: mensaje, token bloqueado y log sin credenciales
- **Verifica:** HU-11 · RNF-06 · SPEC-003 §3.2, §4, §5.8
- **Tipo / prioridad:** E2E · Alta
- **Precondiciones:** App de E2E con `Smtp__Host=localhost` y `Smtp__Puerto=2525` (SPEC-004 §6) y User Secrets con usuario y contraseña.
- **Pasos:** 1. «¿Olvidaste tu contraseña?» con `qc@sistemaveredas.local`. 2. Leer la consola.
- **Datos:** —
- **Resultado esperado:** 1. SweetAlert con el texto completo `No pudimos enviar el mail. Probá de nuevo más tarde o pedile a otro usuario que te asigne una contraseña nueva desde Usuarios.` (tildes bien, sin error de JS en la consola del navegador). `token_recovery` = `tokenbloqueado`. 2. El log tiene `No se pudo enviar el mail de recuperación a qc@sistemaveredas.local.` y no contiene el usuario ni la contraseña de SMTP.

#### CP-204 — Recuperación sin token antifalsificación
- **Verifica:** HU-11 · RNF-07 · SPEC-003 §3.2 (StartRecovery 1)
- **Tipo / prioridad:** Integración · Media
- **Pasos:** POST `/Access/StartRecovery` sin `__RequestVerificationToken`.
- **Datos:** —
- **Resultado esperado:** 400; `token_recovery` no cambia.

#### CP-205 — El mail real sale con las credenciales de User Secrets y dice «SistemaVeredas»
- **Verifica:** HU-11 (crit. 2 y 5) · RNF-06 · P-19 · SPEC-003 §3.3
- **Tipo / prioridad:** Manual · Alta
- **Precondiciones:** Santiago cargó `Smtp:Usuario`, `Smtp:Contrasena` y `Smtp:Remitente` en User Secrets. App contra la base de pruebas **sin** `Smtp__Host` / `Smtp__Puerto`. Usuario en la base de pruebas con un email real al que Santiago tiene acceso. Depende de R-01 (impedimento 3).
- **Pasos:** 1. Pedir la recuperación. 2. Abrir el mail. 3. Usar el botón.
- **Datos:** —
- **Resultado esperado:** 1. Login con `Te enviamos un mail con el enlace para elegir una contraseña nueva.` 2. Asunto `Restablecé tu contraseña de SistemaVeredas`. El cuerpo tiene «Restablecer la contraseña», «Hola:», «Recibimos un pedido para restablecer la contraseña de tu usuario en SistemaVeredas.», el botón «Elegir contraseña nueva», «El enlace sirve una sola vez.», «Si no lo pediste, ignorá este mail: tu contraseña no cambia.» y «— SistemaVeredas». No dice «WebTech», «instituto», «aprendizaje» ni «30 minutos». 3. Abre `https://localhost:7243/Access/Recovery?token=…`.

#### CP-206 — Títulos de las páginas de acceso
- **Verifica:** HU-11 (crit. 4) · P-19 · SPEC-003 §3.3
- **Tipo / prioridad:** E2E · Media
- **Pasos:** Abrir Login, StartRecovery y Recovery (`?token=tokenqa`), ver el título de la pestaña, el código fuente y la pestaña de red.
- **Datos:** —
- **Resultado esperado:** Los tres títulos terminan en `- SistemaVeredas`. El código fuente de las tres no contiene `ISFDyT` ni `WebTech` (tampoco en comentarios). Recovery no pide `css/image/logo.png` (sin 404 en red).

---

## 3. Matriz de trazabilidad

Requisitos del alcance del sprint 1 (backlog §1 y specs). «Debe» según `Docs/03`.

| Requisito | Prio. 03 | Casos |
|---|---|---|
| RF-ACC-01 Iniciar sesión | Debe | CP-006, CP-010, CP-011, CP-194, CP-198 |
| RF-ACC-03 Sesión en todo el sistema | Debe | CP-007, CP-008, CP-009, CP-013, CP-130 |
| RF-ACC-04 ABM de usuarios (contraseña) | Debe | CP-194, CP-195 |
| RF-ACC-05 Contraseña nueva desde la edición | Debería | CP-195 |
| RF-ACC-06 Recuperación por mail | Podría | CP-197, CP-201, CP-202, CP-203, CP-205 |
| RF-VER-01 Formulario (medición, tipos y cordón) | Debe | CP-075, CP-118, CP-121 |
| RF-VER-05 Tipo por pozo, obligatorio | Debe | CP-017, CP-063, CP-064, CP-078, CP-101, CP-107 |
| RF-VER-06 Alta rápida de tipo | Debe | CP-120 a CP-133 |
| RF-VER-07 Términos y total automático | Debe | CP-021 a CP-060, CP-075, CP-076, CP-089, CP-090, CP-093, CP-094 |
| RF-VER-08 Casilla Cordón | Debe | CP-069, CP-070, CP-111 a CP-119 |
| RF-VER-09 Guardar sin medición | Debe | CP-032, CP-066, CP-082, CP-086, CP-097 |
| RF-VER-10 Listado, buscador y filtros (medidas / sin medir) | Debe | CP-139, CP-140, CP-141 |
| RF-VER-12 Detalle con foto protagonista | Debe | CP-142 |
| RF-VER-13 Editar vereda | Debe | CP-084 a CP-087, CP-109, CP-147 |
| RF-VER-14 Eliminar vereda (solo roturas en cascada) | Debe | CP-091 |
| RF-VER-16 Renglón por pozo | Debe | CP-063, CP-075, CP-101 a CP-106 |
| RF-VER-17 Detalle con pozos y totales | Debe | CP-142, CP-143, CP-147 |
| RF-TSU-01 Catálogo de tipos de suelo | Debe | CP-018, CP-123, CP-126, CP-128, CP-134 a CP-138 |
| RF-HOM-01 Home | Debe | CP-144, CP-145, CP-146, CP-147 |
| RF-PAQ-01 Crear paquete | Debe | CP-148 |
| RF-PAQ-02 Asignar varias desde el paquete | Debe | CP-148 a CP-166 |
| RF-PAQ-03 Quitar vereda | Debe | CP-171, CP-172, CP-173 |
| RF-PAQ-04 Detalle del paquete con totales y avance | Debe | CP-167 a CP-171, CP-174 |
| RF-PAQ-05 Listar paquetes | Debe | CP-179 |
| RF-PAQ-06 Editar y eliminar paquete | Debe | CP-175 a CP-178 |
| RF-PAQ-08 Agregar desde el listado | Debería | CP-181 a CP-187 |
| RN-01 Un solo rol | — | CP-007 (indirecto) |
| RN-04 Tipo de suelo solo catálogo | — | CP-018, CP-068, CP-077, CP-108 |
| RN-05 La medición no usa la baldosa | — | CP-068 |
| RN-06 m² y m³ | — | CP-035, CP-036, CP-070, CP-112 |
| RN-07 Suma de productos | — | CP-021 a CP-031 |
| RN-08 Coma o punto | — | CP-021, CP-026, CP-028, CP-029, CP-030, CP-076 |
| RN-09 Sin medición = sin medir | — | CP-082, CP-097, CP-139, CP-143 |
| RN-10 Un paquete como máximo | — | CP-149, CP-157, CP-158, CP-161, CP-183, CP-184 |
| RN-11 Eliminar paquete no borra veredas | — | CP-176, CP-177, CP-178 |
| RN-13 Tipo en uso no se borra | — | CP-110, CP-125, CP-134 a CP-138 |
| RN-15 Solo `+`, `*` y `x`; paréntesis opcionales | — | CP-025, CP-026, CP-046 a CP-051 |
| RN-16 Contraseña de 8 a 50 | — | CP-188 a CP-198 |
| RN-17 2 medidas en m², 3 en m³ | — | CP-040 a CP-045, CP-095, CP-113, CP-114 |
| RN-18 Guardar sin paquete y asignar después | — | CP-156, CP-171, CP-180 |
| RNF-03 Total al instante y término con error | — | CP-058, CP-093 a CP-096, CP-106, CP-112, CP-113 |
| RNF-04 Páginas con sesión | — | CP-007, CP-008, CP-009, CP-013 |
| RNF-06 Secretos fuera del código | — | CP-199 a CP-205 |
| RNF-07 Validación en servidor y token | — | CP-012, CP-079, CP-080, CP-083, CP-092, CP-099, CP-114, CP-126, CP-129, CP-152, CP-161, CP-166, CP-173, CP-185, CP-204 |
| RNF-10 Solo migraciones | — | CP-003, CP-014, CP-019, CP-020 |
| RNF-16 Cálculo en servicio con pruebas | — | CP-021 a CP-074 |
| RNF-17 2 decimales m² y 3 m³ | — | CP-033 a CP-036, CP-061, CP-098 |
| RNF-18 Veredas / Roturas / TiposSuelo | — | CP-014, CP-015, CP-075, CP-084, CP-115 |
| RNF-19 Pruebas contra base de pruebas | — | CP-001 a CP-006 |
| D-27 Mediciones se borra | — | CP-016 |
| P-19 Textos «SistemaVeredas» | — | CP-205, CP-206 |
| R-05 Migración probada antes | — | CP-003, CP-014 |

### 3.1 Por historia

| Historia | Casos |
|---|---|
| HU-01 | CP-014 a CP-020, CP-091, CP-138 |
| HU-02 | CP-021 a CP-062, CP-066, CP-067, CP-075, CP-076, CP-079, CP-082 a CP-084, CP-086 a CP-100 |
| HU-03 | CP-063 a CP-065, CP-068, CP-074, CP-077, CP-078, CP-080, CP-081, CP-085, CP-087, CP-101 a CP-110 |
| HU-04 | CP-069 a CP-073, CP-111 a CP-119 |
| HU-05 | CP-120 a CP-137 |
| HU-06 | CP-139 a CP-147 |
| HU-07 | CP-148 a CP-166 |
| HU-08 | CP-167 a CP-180 |
| HU-09 | CP-181 a CP-187 |
| HU-10 | CP-188 a CP-198 |
| HU-11 | CP-199 a CP-206 |
| HU-12 | CP-001 a CP-013 |

## 4. Requisitos sin cubrir

**Del alcance del sprint 1:** ninguno de los «Debe» queda sin caso. Parciales:

- **RF-VER-01:** solo la parte de medición, tipos y cordón (la lista de campos depende de P-07).
- **RF-VER-14:** solo el borrado en cascada de las roturas; eliminar una vereda que está en un paquete depende de P-09.
- **RNF-01:** solo el formato de los totales de medición (resto fuera de alcance, SPEC-001 §6).
- **RNF-14:** solo el tope de 1.000 ids (CP-163); el tiempo de carga de listados no se mide en este sprint.
- **HU-11 crit. 3 (sitio publicado):** no se prueba en el hosting porque las pruebas nunca corren contra el sitio publicado; se cubre revisando la configuración (CP-200). Ver O-06.

**«Debe» fuera del sprint 1, sin casos todavía** (backlog §5, ítem 11): RF-ACC-02 (cerrar sesión, solo se usa en CP-130), RF-ACC-04 (resto del ABM de usuarios), RF-VER-02, RF-VER-03, RF-VER-04, RF-VER-15, RN-02, RN-03, RN-12.

## 5. Observaciones sobre requisitos y specs

| ID | Dónde | Observación | Propuesta (a quién) |
|---|---|---|---|
| O-01 | SPEC-001 §2.2, §3.2 paso 8 | `[StringLength(500)]` valida lo que escribe el usuario, pero se guarda `TextoNormalizado`, que agrega 2 paréntesis por término. Una fórmula de 500 caracteres sin paréntesis (125 términos) da 750 y no entra en `nvarchar(500)`: error 500 o truncado. RF-VER-07 dice «sin límite de pozos». | Validar el largo del texto normalizado con el mismo mensaje, o ampliar la columna. Definir el esperado de CP-090 (Desarrollador). |
| O-02 | Numeración | SPEC-002 dice «HU-09 a HU-12», SPEC-003 «HU-13 a HU-15» y SPEC-004 «HU-16»; en el backlog son HU-07 a HU-09, HU-10 y HU-11, y HU-12. Los casos usan la del backlog. | Alinear las specs con el backlog (Desarrollador). |
| O-03 | HU-02 crit. 2, Docs/01 §5 | Dicen `3*2` → «6 m²»; la spec muestra `6,00 m²`. Los casos esperan `6,00 m²`. | Escribir `6,00 m²` en los criterios (Scrum Master). |
| O-04 | HU-03 crit. 2 vs SPEC-001 §5.3 | «Los pozos que siguen conservan el tipo elegido» se puede leer como «cada pozo conserva su tipo aunque se inserte uno antes». La spec (y el criterio provisorio) lo conserva **por posición**: al insertar en el medio los tipos se corren. | Redactar el criterio como «se conservan por posición» o resolver Q4 (PM). |
| O-05 | HU-06 crit. 5 vs SPEC-001 §3.3/§3.5 | El criterio dice que el Home muestra «lo mismo que hoy», pero la spec agrega los m³ del cordón (propuesta de P-16, que el backlog pone fuera del sprint). CP-144 espera lo de la spec. | Confirmar si los m³ del Home entran en el sprint (PM). |
| O-06 | HU-11 crit. 3 | «Dado el sitio publicado» no se puede verificar sin probar en el hosting, y CLAUDE.md lo prohíbe. | Reformular como revisión de `appsettings.Production.json` y de la publicación, o que Santiago lo verifique aparte (Scrum Master). |
| O-07 | SPEC-002 §4.4 vs §4.5 | El contador figura como «{n} seleccionadas» y como «{n} seleccionada(s)». Con 1 no está claro si dice «1 seleccionada». | Fijar el texto con singular/plural (Desarrollador). |
| O-08 | SPEC-002 §5 (salteadas) | No está definido el formato exacto de la lista (separador antes de «y {r} más», punto final) ni el formato de {Dirección} (calle y altura, ¿con entre calles?). | Dar un ejemplo literal completo (Desarrollador). |
| O-09 | SPEC-002 §5 | Si en un mismo envío hay salteadas, «ya estaban» e inexistentes, no se dice cómo se unen en el aviso (orden y separador). | Dar un ejemplo literal (Desarrollador). |
| O-10 | SPEC-001 §3.2 | `0,001*1` es válido y da 0,00 m²: la vereda queda «Medida» con total 0,00. | Decidir si un subtotal redondeado a 0 es error (PM). |
| O-11 | SPEC-001 §3.2 `FormatearM2` | No dice si hay separador de miles (`2.000,00 m²`). El JS usa `toLocaleString('es-AR')`, que sí lo pone. CP-062 lo espera. | Confirmar el formato con miles (Desarrollador). |
| O-12 | SPEC-001 §4.3 | «lo usan {n} vereda(s)» no aclara si concuerda con el número como «medida(s)» en §4. CP-134, CP-135 y CP-137 esperan el texto literal «vereda(s)». | Concordar («1 vereda» / «2 veredas») y actualizar los casos (Desarrollador). |
| O-13 | SPEC-001 §3.5 vs Docs/01 §5 | El boceto de Docs/01 muestra «Vainilla 0,20x0,20»; la spec usa `Descripcion` = «Vainilla (0,20x0,20)». Los casos siguen la spec. | Actualizar el boceto (PM). |
| O-14 | SPEC-001 §3.5 (Home) | El subtítulo está escrito como `veredas cargadas · …` sin decir qué va antes (la cantidad). | Dar el texto completo, p. ej. `5 veredas cargadas · 57,25 m² medidos` (Desarrollador). |
| O-15 | RN-13 | Está *A confirmar* y no tiene historia propia en el sprint (el backlog lo deja en el ítem 7), pero SPEC-001 lo implementa. Sus casos se asignaron a HU-05. | Confirmar RN-13 (Q1) y decidir a qué historia pertenece (PM / Scrum Master). |
| O-16 | SPEC-003 §4 | Los mensajes de SMTP van dentro de un string de JS con `Html.Raw`; cualquier apóstrofo futuro rompe la página. | Aplicar la sugerencia `@Json.Serialize` (Desarrollador). |

## 6. Resumen

| Tipo | Alta | Media | Baja | Total |
|---|---|---|---|---|
| Unitaria | 38 | 18 | 5 | 61 |
| Integración | 32 | 20 | 3 | 55 |
| E2E | 39 | 31 | 10 | 80 |
| Manual | 7 | 2 | 1 | 10 |
| **Total** | **116** | **71** | **19** | **206** |

Por historia: HU-12 13 · HU-01 7 propios (+2 compartidos) · HU-02 a HU-06 (SPEC-001) 127 · HU-07 a HU-09 (SPEC-002) 40 · HU-10 y HU-11 (SPEC-003) 19.
