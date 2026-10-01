# 07 — Backlog

> Documento vivo · v0.2 · 30/09/2026 · Lo mantiene el agente `scrum-master`. Santiago (Product Owner) prioriza y acepta.

**Estados de una historia:** Por hacer → En curso (programación y revisión) → En prueba (QC) → Hecha.
**Puntos:** 1, 2, 3, 5 u 8. Estimación inicial; se ajusta con el Desarrollador al aprobar cada spec.

## 1. Sprint 1

**Objetivo:** corregir la medición y los tipos de suelo de la vereda, asignar veredas a paquetes de forma cómoda (varias a la vez), sacar las credenciales del código con contraseñas de 8 a 50 caracteres, y dejar armada la base de pruebas automatizadas.

**Fechas y duración:** a definir (impedimentos, sección 3). **Capacidad:** primer sprint, sin velocidad de referencia. **Total:** 12 historias, 50 puntos (sección 6).

| Spec | Qué cubre | Requisitos | PT | Historias |
|---|---|---|---|---|
| SPEC-001 | Medición (Veredas) y roturas con su tipo de suelo (Roturas) | RF-VER-05 a RF-VER-09, RF-VER-16, RF-VER-17, RF-TSU-01, RN-15, RN-17 | 1.3.1, 1.3.3, 1.4.3, 1.4.4.1, 1.4.4.2, 1.4.4.5, 1.4.4.6, 1.4.7 | HU-01 a HU-06 |
| SPEC-002 | Asignación de veredas a paquetes | RF-PAQ-02, RF-PAQ-03, RF-PAQ-04, RF-PAQ-06, RF-PAQ-08, RN-18 | 1.4.5.1, 1.4.5.2, 1.4.5.3 | HU-07 a HU-09 |
| SPEC-003 | Contraseñas y credenciales | RN-16, RNF-06, P-19 | 1.3.4, 1.4.2 | HU-10, HU-11 |
| SPEC-004 | Pruebas automatizadas | RNF-16, RNF-19 | 1.5.2 | HU-12 |

Los PT 1.5.1 (casos de prueba) y 1.5.3 (ejecución y regresión) van dentro del flujo de cada historia: QA escribe los casos antes de programar y QC los ejecuta antes de cerrarla.

## 2. Historias del sprint 1

Los requisitos marcados *(A confirmar)*, *(Vigente)* o con una pregunta *(P-nn)* todavía no cumplen la Definición de Listo. Lo que falta por historia está en la tabla de la sección 6.

### SPEC-001 — Medición y tipos de suelo

#### HU-01 — Migración RoturasEnVeredas (técnica)

Como equipo necesitamos la migración `RoturasEnVeredas`, que crea la tabla Roturas y elimina Mediciones, para que cada vereda guarde su fórmula y sus roturas y Tipo de suelo quede solo como catálogo.

- **Requisitos:** RN-04 · RNF-18 · D-19, D-27, D-29
- **PT:** 1.3.1, 1.4.4.2 · **Spec:** SPEC-001 · **Casos de prueba:** CP-014 a CP-020, CP-091, CP-138
- **Puntos:** 5 · **Estado:** Por hacer · **Depende de:** HU-12 (probar la migración en la base de pruebas)

Criterios de aceptación:

1. **Dado** la base de pruebas con veredas cargadas, **cuando** se aplica `RoturasEnVeredas`, **entonces** se aplica sin errores; Veredas tiene la fórmula, el total en m² y el cordón (términos y total en m³); existe la tabla Roturas (vereda, orden, medidas, tipo de suelo obligatorio y subtotal), y las veredas conservan el resto de sus datos.
2. **Dado** que había filas en Mediciones, **cuando** se aplica la migración, **entonces** la tabla se elimina con sus datos, sin convertirlos (son de prueba, D-27).
3. **Dado** una rotura, **cuando** se intenta guardar sin tipo de suelo, **entonces** la base no lo permite.
4. **Dado** el sistema migrado, **cuando** recorro el menú, **entonces** ya no está Mediciones y Tipos de suelo muestra solo el catálogo.

#### HU-02 — Medición con total automático

Como usuario quiero escribir la medición de las roturas de la vereda para que el sistema calcule solo el total en m² y me avise si la escribí mal.

- **Requisitos:** RF-VER-07, RF-VER-09 · RN-05, RN-06, RN-07, RN-08 *(A confirmar)*, RN-09, RN-15, RN-17 · RNF-03, RNF-16, RNF-17 *(A confirmar)*
- **PT:** 1.3.3, 1.4.4.1, 1.4.4.2 · **Spec:** SPEC-001 · **Casos de prueba:** CP-021 a CP-062, CP-066, CP-067, CP-075, CP-076, CP-079, CP-082 a CP-084, CP-086 a CP-100
- **Puntos:** 8 · **Estado:** Por hacer · **Depende de:** HU-01

Criterios de aceptación:

1. **Dado** el formulario de alta o edición de una vereda, **cuando** escribo `(2*3)+(5*9)` en «Medición», **entonces** veo al instante «Total» = 51,00 m².
2. **Dado** los ejemplos de `Docs/01` (sección 5), **cuando** los escribo, **entonces** dan el total esperado: `(2*3)+(5*9)+(4.5*8)+(2,4*4)+(3,75*9)` → 130,35 m² y `3*2` → 6 m². Con `x` en lugar de `*` da lo mismo.
3. **Dado** que escribo `(2*3)+59`, **cuando** intento guardar, **entonces** el sistema indica que el término 2 tiene 1 medida y lleva 2, y no guarda. Tampoco guarda con una operación que no sea `+`, `*` o `x`.
4. **Dado** que dejo la medición vacía, **cuando** guardo, **entonces** la vereda se guarda sin roturas y figura como «sin medir».
5. **Dado** una medición válida, **cuando** guardo, **entonces** el servidor la vuelve a validar y guarda la fórmula, una rotura por término con su subtotal y el total que calcula él, con 2 decimales, aunque el navegador mande otro valor.
6. **Dado** una vereda guardada con medición, **cuando** la edito, **entonces** veo la fórmula, las roturas y el total como los guardé.
7. **Dado** el servicio de cálculo, **cuando** corren sus pruebas unitarias, **entonces** pasan, incluidos todos los ejemplos de `Docs/01`.

#### HU-03 — Elegir el tipo de suelo de cada rotura

Como usuario quiero que al escribir la medición aparezca un renglón por rotura con su desplegable de tipo de suelo para indicar qué baldosa lleva cada pozo.

- **Requisitos:** RF-VER-05, RF-VER-16 · RN-04 · D-26, D-29
- **PT:** 1.4.4.1, 1.4.4.2 · **Spec:** SPEC-001 · **Casos de prueba:** CP-063 a CP-065, CP-068, CP-074, CP-077, CP-078, CP-080, CP-081, CP-085, CP-087, CP-101 a CP-110
- **Puntos:** 3 · **Estado:** Por hacer · **Depende de:** HU-02

Criterios de aceptación:

1. **Dado** el formulario, **cuando** escribo `(2*3)+(5*9)+(0,5*0,5)`, **entonces** aparecen tres renglones (Pozo 1, 2 y 3) con sus medidas, su subtotal (6,00, 45,00 y 0,25 m²) y un desplegable de tipo de suelo con los tipos del catálogo; el total es 51,25 m².
2. **Dado** que agrego o borro un término, **cuando** cambia la fórmula, **entonces** los renglones se actualizan y los pozos que siguen conservan el tipo elegido.
3. **Dado** una medición con un pozo sin tipo de suelo, **cuando** intento guardar, **entonces** el sistema indica qué pozo le falta y no guarda.
4. **Dado** dos pozos con el mismo tipo de suelo, **cuando** guardo, **entonces** se guarda sin error.
5. **Dado** la misma fórmula con otros tipos de suelo, **cuando** veo el total, **entonces** es el mismo: el tipo de suelo no interviene en el cálculo.
6. **Dado** una vereda guardada, **cuando** la edito, **entonces** cada renglón muestra el tipo que le elegí.

#### HU-04 — Cordón con casilla

Como usuario quiero marcar «Cordón» para cargar la rotura del cordón en m³ solo cuando la vereda la tiene.

- **Requisitos:** RF-VER-08 · RN-06, RN-07, RN-17 · RNF-03, RNF-17 *(A confirmar)* · D-15
- **PT:** 1.4.4.2 · **Spec:** SPEC-001 · **Casos de prueba:** CP-069 a CP-073, CP-111 a CP-119
- **Puntos:** 3 · **Estado:** Por hacer · **Depende de:** HU-02

Criterios de aceptación:

1. **Dado** el formulario, **cuando** la casilla «Cordón» está sin marcar, **entonces** no se ven sus campos; **cuando** la marco, aparecen «Medición cordón» y «Total cordón».
2. **Dado** la casilla marcada, **cuando** escribo `(5*0,15*0,30)`, **entonces** veo al instante 0,225 m³.
3. **Dado** que escribo `(5*0,15)`, **cuando** intento guardar, **entonces** el sistema indica que el término 1 tiene 2 medidas y lleva 3, y no guarda.
4. **Dado** una vereda con tipo de suelo y cordón medidos, **cuando** la guardo y la edito, **entonces** conserva las dos mediciones y «Cordón» aparece marcada.

#### HU-05 — Alta rápida de tipo de suelo

Como usuario quiero agregar un tipo de suelo desde el formulario de la vereda para no perder lo que estoy cargando cuando el tipo no está en la lista.

- **Requisitos:** RF-VER-06 · RF-TSU-01 (nombre y medida; formato de la medida: *P-05*) · D-04
- **PT:** 1.4.3 · **Spec:** SPEC-001 · **Casos de prueba:** CP-120 a CP-137
- **Puntos:** 3 · **Estado:** Por hacer · **Depende de:** HU-03

Criterios de aceptación:

1. **Dado** el renglón de un pozo, **cuando** uso «+ Nuevo» junto a su tipo de suelo y cargo nombre y medida, **entonces** el tipo se agrega al catálogo y queda elegido en ese pozo.
2. **Dado** que ya cargué dirección, ubicación, fotos y medición, **cuando** agrego un tipo nuevo, **entonces** todo lo cargado sigue como estaba, incluidos los tipos de los demás pozos.
3. **Dado** un tipo recién agregado, **cuando** abro el desplegable de otro pozo, **entonces** también figura.
4. **Dado** un tipo agregado desde la vereda, **cuando** abro Tipos de suelo, **entonces** figura en el catálogo.

#### HU-06 — Filtro y totales con la nueva medición

Como usuario quiero que el listado, el detalle y el Home usen la medición nueva para saber qué veredas faltan medir y cuánto hay que reparar sin hacer cuentas.

- **Requisitos:** RF-VER-17 *(A confirmar)* · RF-VER-10, RF-VER-12, RF-HOM-01 (se adaptan; el contenido nuevo del Home es *P-16*, fuera del sprint) · RN-09
- **PT:** 1.4.4.5, 1.4.4.6, 1.4.7 · **Spec:** SPEC-001 · **Casos de prueba:** CP-139 a CP-147
- **Puntos:** 5 · **Estado:** Por hacer · **Depende de:** HU-02 a HU-04

Criterios de aceptación:

1. **Dado** veredas con y sin medición, **cuando** filtro «medidas», **entonces** veo solo las que tienen al menos una medición (en m² o de cordón); **cuando** filtro «sin medir», solo las que no tienen ninguna.
2. **Dado** el listado, **cuando** uso el buscador por dirección o el filtro por estado, **entonces** funcionan como antes.
3. **Dado** una vereda con `(2*3)+(5*9)+(0,5*0,5)` y `(5*0,15*0,30)` en el cordón, **cuando** abro el detalle, **entonces** veo cada pozo con su tipo de suelo y su subtotal (6,00, 45,00 y 0,25 m²), 51,25 m² en total y 0,225 m³ de cordón; la foto sigue como protagonista.
4. **Dado** una vereda sin medición, **cuando** abro el detalle, **entonces** figura «sin medir».
5. **Dado** el Home, **cuando** lo abro, **entonces** muestra lo mismo que hoy (totales por estado, con y sin paquete, m² medidos), calculado con la medición nueva.

### SPEC-002 — Asignación de veredas a paquetes

#### HU-07 — Asignar varias veredas desde el paquete

Como usuario quiero asignar desde el paquete varias veredas ya guardadas a la vez para armarlo cuando ya junté las que van juntas.

- **Requisitos:** RF-PAQ-02 (qué veredas se muestran: *P-13*) · RN-18 · RN-10 *(Vigente)* · D-20
- **PT:** 1.4.5.2 · **Spec:** SPEC-002 · **Casos de prueba:** CP-148 a CP-166
- **Puntos:** 5 · **Estado:** Por hacer · **Depende de:** HU-01 · **Bloqueada por:** P-13, P-12

Criterios de aceptación:

1. **Dado** un paquete y veredas guardadas sin paquete, **cuando** abro la asignación desde el paquete, en cualquier momento, **entonces** veo las veredas que puedo asignar, con su estado y si están medidas (propuesta de P-13).
2. **Dado** esa lista, **cuando** marco varias y confirmo, **entonces** quedan todas en el paquete de una vez y aparecen en su detalle.
3. **Dado** una vereda que ya está en otro paquete, **cuando** abro la asignación, **entonces** no se ofrece.
4. **Dado** una vereda recién cargada, **cuando** la guardo sin paquete, **entonces** se guarda y queda disponible para asignarla después.

#### HU-08 — Quitar una vereda y ver los totales del paquete

Como usuario quiero quitar veredas de un paquete y ver en su detalle la cantidad, los totales y el avance para corregirlo y saber cuánto trabajo tiene y cuánto falta.

- **Requisitos:** RF-PAQ-03 *(A confirmar)*, RF-PAQ-04 *(A confirmar; P-14)*, RF-PAQ-06 *(A confirmar)* · RN-11 *(Vigente)*, RN-18
- **PT:** 1.4.5.1, 1.4.5.2, 1.4.5.3 · **Spec:** SPEC-002 · **Casos de prueba:** CP-167 a CP-180
- **Puntos:** 5 · **Estado:** Por hacer · **Depende de:** HU-07 y HU-02 a HU-04 · **Bloqueada por:** P-12 (editar el paquete)

Criterios de aceptación:

1. **Dado** un paquete con veredas, **cuando** quito una, **entonces** sale del paquete, queda guardada sin paquete con todos sus datos y vuelve a aparecer para asignarla.
2. **Dado** un paquete con veredas medidas, **cuando** abro su detalle, **entonces** veo sus veredas, la cantidad, el total de m² (suma de los totales de sus veredas) y el de m³ (suma de sus cordones).
3. **Dado** un paquete con 4 veredas y 1 en estado Finalizado, **cuando** abro su detalle, **entonces** el avance es 25 %.
4. **Dado** un paquete, **cuando** asigno o quito una vereda o cambio su estado, **entonces** la cantidad, los totales y el avance se actualizan.
5. **Dado** un paquete con veredas, **cuando** lo edito, **entonces** conserva sus veredas; **cuando** lo elimino, **entonces** sus veredas no se borran y quedan sin paquete.

#### HU-09 — Agregar veredas al paquete desde el listado (Debería)

Como usuario quiero marcar varias veredas en el listado y agregarlas a un paquete para armar paquetes mientras reviso el listado.

- **Requisitos:** RF-PAQ-08 *(Debería; A confirmar)* · RN-10 *(Vigente)*, RN-18
- **PT:** 1.4.5.2 · **Spec:** SPEC-002 (parte B) · **Casos de prueba:** CP-181 a CP-187
- **Puntos:** 3 · **Estado:** Por hacer · **Depende de:** HU-07

Criterios de aceptación:

1. **Dado** el listado de veredas, **cuando** marco varias y elijo un paquete, **entonces** quedan asignadas a ese paquete.
2. **Dado** que marqué una vereda que ya está en otro paquete, **cuando** confirmo, **entonces** no queda en dos paquetes y el sistema me lo indica.

Es la primera candidata a pasar al sprint 2 si no alcanza el tiempo. Lo decide Santiago.

### SPEC-003 — Contraseñas y credenciales

#### HU-10 — Contraseña de 8 a 50 caracteres

Como usuario quiero usar contraseñas de 8 a 50 caracteres para poder elegir claves largas.

- **Requisitos:** RN-16 *(máximo 50: Confirmado · mínimo 8: Vigente)* · D-21
- **PT:** 1.4.2 · **Spec:** SPEC-003 · **Casos de prueba:** CP-188 a CP-198
- **Puntos:** 2 · **Estado:** Por hacer · **Depende de:** —

Criterios de aceptación:

1. **Dado** el alta o la edición de un usuario, **cuando** escribo una contraseña de 7 o de 51 caracteres, **entonces** el sistema avisa y no guarda.
2. **Dado** el alta o la edición de un usuario, **cuando** escribo una de 8 o de 50, **entonces** guarda.
3. **Dado** un usuario con una contraseña de 50 caracteres, **cuando** inicia sesión con ella, **entonces** entra.
4. **Dado** la recuperación de contraseña, **cuando** escribo la nueva, **entonces** se aplica la misma regla.

#### HU-11 — Credenciales SMTP fuera del código y textos «SistemaVeredas»

Como usuario quiero que la cuenta y la contraseña de Gmail no estén en el código, y que el inicio de sesión y el mail digan «SistemaVeredas», para que nadie use las credenciales desde el repositorio público y para reconocer el sistema.

- **Requisitos:** RNF-06 · D-22 · riesgo R-01 · textos sujetos a *P-19*
- **PT:** 1.3.4, 1.4.2 · **Spec:** SPEC-003 · **Casos de prueba:** CP-199 a CP-206
- **Puntos:** 3 · **Estado:** Por hacer · **Depende de:** —

Criterios de aceptación:

1. **Dado** los archivos versionados, **cuando** se busca la cuenta o la contraseña de aplicación de Gmail, **entonces** no aparecen en ninguno (hoy están en `AccesController.cs`).
2. **Dado** las credenciales en User Secrets en la PC, **cuando** pido recuperar la contraseña, **entonces** la app las lee de ahí y el mail sale como hasta ahora.
3. **Dado** el sitio publicado, **cuando** la app lee las credenciales, **entonces** las toma de `appsettings.Production.json`, que git ignora.
4. **Dado** la página de inicio de sesión, **cuando** la abro, **entonces** el título dice «SistemaVeredas» y no «ISFDyT N°124» (si se aprueba P-19).
5. **Dado** el mail de recuperación, **cuando** llega, **entonces** dice «SistemaVeredas» y no «WebTech» (si se aprueba P-19).

La recuperación sigue sin funcionar en el hosting (D-09): no es un defecto de esta historia. Sacar la contraseña del código no la revoca (R-01, impedimento 4).

### SPEC-004 — Pruebas automatizadas

#### HU-12 — Proyecto de pruebas y base de pruebas (técnica)

Como equipo necesitamos un proyecto de pruebas automatizadas (xUnit) que corra contra una base de pruebas aparte para verificar cada historia sin tocar la base real ni el sitio publicado.

- **Requisitos:** RNF-16, RNF-19 · D-25
- **PT:** 1.5.2 · **Spec:** SPEC-004 · **Casos de prueba:** CP-001 a CP-013
- **Puntos:** 5 · **Estado:** Por hacer · **Depende de:** — (va primero: la Definición de Hecho de todas pide las pruebas en verde)

Criterios de aceptación:

1. **Dado** la solución en la PC de Santiago, **cuando** se corre `dotnet test SistemaVeredas.Tests`, **entonces** compila, corre las pruebas y deja la salida completa en `TestResults/`, ignorada por git.
2. **Dado** el proyecto de pruebas, **cuando** se corre, **entonces** tiene al menos una prueba unitaria y una de integración, y pasan.
3. **Dado** una prueba de integración, **cuando** usa la base, **entonces** usa la base de pruebas creada con las migraciones del proyecto; nunca `GestionVeredas` ni el sitio publicado.
4. **Dado** que QC levanta la app en la PC para las pruebas E2E (`https://localhost:7243`), **cuando** se conecta a la base, **entonces** usa la base de pruebas.

### Orden de trabajo sugerido

1. **HU-12**: sin ella ninguna historia cumple la Definición de Hecho, y hace falta para probar la migración (R-05).
2. **HU-11** (urgente por R-01) y **HU-10**.
3. **SPEC-001:** HU-01 → HU-02 → HU-03 y HU-04 → HU-05 → HU-06.
4. **SPEC-002:** HU-07 → HU-08 → HU-09.

## 3. Impedimentos del sprint 1

| # | Impedimento | Afecta | Qué hace falta | Quién lo destraba |
|---|---|---|---|---|
| 1 | **P-12** — Hoy el paquete exige proveedor y cada vereda tiene el suyo. No está definido dónde va el proveedor. | HU-07 (qué pasa con el proveedor de la vereda al asignarla) y HU-08 (editar el paquete) | Responder P-12 (propuesta: el proveedor va en el paquete y la vereda solo indica si está a cargo del frentista). | Santiago responde; PM registra |
| 2 | **P-13** — ¿Qué veredas se pueden asignar a un paquete? | HU-07, HU-09 | Aprobar la propuesta (cualquiera que no esté en otro paquete, mostrando estado y si está medida) y confirmar RN-10. | Santiago responde; PM registra |
| 3 | **R-01** — La contraseña de aplicación de Gmail sigue válida y está en el historial del repositorio público. Revocarla exige verificar la cuenta, y por ahora Santiago no puede hacerlo. HU-11 la saca del código, pero no la revoca. | HU-11 (probar el envío con credenciales en User Secrets) | Cuando Santiago pueda verificar la cuenta: revocarla, generar una nueva para User Secrets y decidir si se borra el historial o se hace privado el repositorio. Mientras tanto, HU-11 sigue igual. | Santiago |
| 4 | **Dependencia de la PC de Santiago** (D-25, R-06) — Este entorno no puede descargar el SDK de .NET ni paquetes NuGet: compilar, `dotnet test` y E2E solo corren en su PC, y durante cada ronda no la puede usar. | La Definición de Hecho de todas las historias | Coordinar rondas. Propuesta: 1) SPEC-004 y SPEC-003; 2) SPEC-001; 3) SPEC-002; más las de regresión. | Santiago y Scrum Master |

Otros bloqueos para la Definición de Listo (los controla el Scrum Master historia por historia):

- Las specs SPEC-001 a SPEC-004 se están escribiendo y no existe todavía `Specs/pruebas/casos-de-prueba.md`.
- Requisitos del sprint sin confirmar: RF-VER-17, RF-PAQ-03, RF-PAQ-04, RF-PAQ-06, RF-PAQ-08, RN-08, RN-10, RN-11, RN-16 (mínimo), RNF-17 y P-05 (formato de la medida, para HU-05), P-14 (para HU-08) y P-19 (para HU-11). RN-08 y el total calculado por el servidor ya figuran como decididos en `CLAUDE.md`: falta registrarlos en 03.
- Falta definir la duración y las fechas del sprint (P-18 es la fecha de entrega del proyecto, no la del sprint).

## 4. Definición de Listo y de Hecho

Son las de [`06-equipo-y-flujo.md`](06-equipo-y-flujo.md): Listo en la sección 3 y Hecho en la sección 4. El Scrum Master controla la de Listo antes de pasar una historia a En curso y la de Hecho antes de marcarla Hecha.

## 5. Backlog del producto (fuera del sprint 1)

Orden propuesto por el Scrum Master; lo decide Santiago. Criterio: primero lo «Debe» y lo que conviene resolver antes de empezar la carga real desde cero (D-03); después seguridad, respaldo y calidad; después lo «Debería» y lo que todavía no tiene requisito. Los RNF no tienen prioridad en 03: se ordenan por el riesgo asociado.

| # | Ítem | Requisitos o pregunta | Prioridad | PT | Qué falta |
|---|---|---|---|---|---|
| 1 | Achicar las fotos al subirlas | RNF-12 · R-02 | Alta (Debe por riesgo: 5 GB del hosting) | 1.4.4.3 | Confirmar RNF-12. |
| 2 | Respaldo de la base y de las fotos | RNF-13 · R-03 | Alta | 1.6.2 | Confirmar RNF-13 y definir la frecuencia. |
| 3 | Campos del formulario de vereda (Nombre y Apellido, quién relevó) | RF-VER-01, RNF-08 · **P-07** | Debe | 1.4.4.1 | Responder P-07; confirmar RNF-08. |
| 4 | Significado de cada estado y aviso si no coincide con los datos | RF-VER-15, RN-12 · **P-08** | Debe | 1.4.4.1 | Responder P-08. |
| 5 | Eliminar una vereda que está en un paquete | RF-VER-14 · **P-09** | Debe | 1.4.4.1 | Responder P-09. |
| 6 | Mapa para elegir la ubicación y mapas en el sitio publicado | RF-VER-02, RF-VER-03 · **P-11** | Debe | 1.4.4.4 | Responder P-11; verificar en el sitio publicado. |
| 7 | Catálogo de tipos de suelo: color, disponible, formato de la medida y baja de un tipo en uso | RF-TSU-01, RN-13 · **P-05** | Debe | 1.4.3 | Responder P-05; confirmar RN-13. |
| 8 | Contraseñas guardadas con hash con sal | RNF-05 | Alta (seguridad) | 1.3.4, 1.4.2 | Confirmar RNF-05. |
| 9 | Revocar la contraseña de Gmail y limpiar el historial o hacer privado el repositorio | R-01 | Alta | 1.3.4 | Que Santiago pueda verificar su cuenta. |
| 10 | Validación en el servidor y token antifalsificación en todos los formularios | RNF-07 | Media | 1.3.4 | Confirmar RNF-07. El sprint 1 cubre los totales de medición. |
| 11 | Casos de prueba y regresión de lo ya construido | RF-ACC-01 a 04, RF-VER-13, RF-PAQ-01, RF-PAQ-05, RN-01 a RN-03, RNF-04 | Debe | 1.5.1, 1.5.3 | Lo exige 02, sección 8. |
| 12 | Fechas dd/mm/aaaa y coma decimal, rendimiento de listados y navegadores | RNF-01, RNF-14, RNF-15 | Media | Varios | Confirmar. |
| 13 | Contenido del Home (sumar m³ al resumen) | RF-HOM-01 · **P-16** | Debe | 1.4.7 | Responder P-16. El sprint 1 solo adapta lo que ya muestra. |
| 14 | Estado propio del paquete | RF-PAQ-04 · **P-14** | Podría | 1.4.5.3 | Responder P-14 (propuesta: alcanza con el avance). |
| 15 | Asignar una contraseña nueva desde la edición del usuario | RF-ACC-05 | Debería | 1.4.2 | Confirmar. Es la forma de reponer una contraseña en el hosting (D-09). |
| 16 | Filtros por prioridad, tipo de suelo y con / sin paquete; sector o barrio | RF-VER-11 · **P-10** | Debería | 1.4.4.5 | Confirmar RF-VER-11; responder P-10. |
| 17 | Proveedor del paquete, registro de proveedores y veredas a cargo del frentista | RF-PAQ-07, RF-PRO-01, RN-14 · **P-12** | Debería | 1.4.5.1, 1.4.6 | Responder P-12 (también es impedimento del sprint 1). |
| 18 | Exportar un paquete a Excel o PDF | **P-15** (sin requisito en 03) | Podría | — | Si se aprueba, el PM lo pasa a 03. |
| 19 | Registro de quién creó o modificó cada vereda y paquete | **P-17** (sin requisito en 03) | Podría | — | Si se aprueba, el PM lo pasa a 03. |
| 20 | Cronograma y fecha de entrega del proyecto | **P-18** | Alta (planificación) | 1.1.1 | Que Santiago dé la fecha; el PM arma el cronograma. |
| 21 | Puesta en marcha: usuarios iniciales y catálogo de tipos de suelo | — (EDT) | Media | 1.6.3 | Ordenar con el PM. |
| 22 | Prueba de aceptación del flujo principal en el sitio publicado | — (EDT; 02, sección 8) | Media | 1.5.4 | Al final de los sprints «Debe». |
| 23 | Manual de usuario | A8 *(A confirmar)* | Baja | 1.7.1 | Confirmar que entra en el alcance. |
| 24 | Documentación técnica y capacitación | — (EDT) | Baja | 1.7.2, 1.7.3 | Ordenar con el PM cuando haya cronograma. |

Sin trabajo pendiente: RF-ACC-06 (queda como está, D-09) y las restricciones RNF-02, RNF-09 y RNF-11, que se controlan en cada revisión.

## 6. Resumen del sprint 1

| Historia | Spec | PT | Pts | Estado | Falta para la Definición de Listo |
|---|---|---|---|---|---|
| HU-01 Migración RoturasEnVeredas (técnica) | SPEC-001 | 1.3.1, 1.4.4.2 | 5 | Por hacer | spec · casos |
| HU-02 Medición con total automático | SPEC-001 | 1.3.3, 1.4.4.1, 1.4.4.2 | 8 | Por hacer | RN-08, RNF-17 · spec · casos |
| HU-03 Elegir el tipo de suelo de cada rotura | SPEC-001 | 1.4.4.1, 1.4.4.2 | 3 | Por hacer | spec · casos |
| HU-04 Cordón con casilla | SPEC-001 | 1.4.4.2 | 3 | Por hacer | RNF-17 · spec · casos |
| HU-05 Alta rápida de tipo de suelo | SPEC-001 | 1.4.3 | 3 | Por hacer | P-05 · spec · casos |
| HU-06 Filtro y totales con la nueva medición | SPEC-001 | 1.4.4.5, 1.4.4.6, 1.4.7 | 5 | Por hacer | RF-VER-17 · spec · casos |
| HU-07 Asignar varias veredas desde el paquete | SPEC-002 | 1.4.5.2 | 5 | Por hacer | P-13, P-12, RN-10 · spec · casos |
| HU-08 Quitar una vereda y ver los totales del paquete | SPEC-002 | 1.4.5.1, 1.4.5.2, 1.4.5.3 | 5 | Por hacer | RF-PAQ-03, 04, 06, RN-11, P-14, P-12 · spec · casos |
| HU-09 Agregar al paquete desde el listado (Debería) | SPEC-002 | 1.4.5.2 | 3 | Por hacer | RF-PAQ-08, RN-10 · spec · casos |
| HU-10 Contraseña de 8 a 50 | SPEC-003 | 1.4.2 | 2 | Por hacer | RN-16 (mínimo) · spec · casos |
| HU-11 Credenciales SMTP fuera del código y textos | SPEC-003 | 1.3.4, 1.4.2 | 3 | Por hacer | P-19 · spec · casos |
| HU-12 Proyecto y base de pruebas (técnica) | SPEC-004 | 1.5.2 | 5 | Por hacer | spec · casos |
| **Total** | | | **50** | | |

Por spec: SPEC-001 = 27 · SPEC-002 = 13 · SPEC-003 = 5 · SPEC-004 = 5. Sin HU-09 (Debería): 47.

Al cerrar el sprint se agregan acá la revisión (qué se terminó, qué no y por qué) y una retrospectiva breve.
