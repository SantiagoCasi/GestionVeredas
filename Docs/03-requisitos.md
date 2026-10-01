# 03 — Requisitos

> Borrador v0.2 · 30/09/2026

**Estado:** `Confirmado` = definido y aprobado · `A confirmar` = propuesta pendiente de aprobación · `Vigente` = así funciona hoy el sistema, falta validarlo.
**Prioridad:** Debe · Debería · Podría · No por ahora.
**EDT:** paquete de trabajo (PT) de 04 donde se construye.

## 1. Requisitos funcionales

### 1.1 Acceso y usuarios (ACC)

| ID | Requisito | Prioridad | Estado | EDT |
|---|---|---|---|---|
| RF-ACC-01 | Iniciar sesión con email y contraseña. | Debe | Confirmado | 1.4.2 |
| RF-ACC-02 | Cerrar sesión. | Debe | Confirmado | 1.4.2 |
| RF-ACC-03 | Exigir sesión iniciada en todo el sistema, salvo en el inicio de sesión y la recuperación de contraseña. | Debe | Confirmado | 1.4.2 |
| RF-ACC-04 | Crear, consultar, editar (incluye activar y desactivar) y eliminar usuarios. | Debe | Confirmado | 1.4.2 |
| RF-ACC-05 | Asignar una contraseña nueva a un usuario desde su edición. Sirve cuando alguien la olvida, porque el mail no funciona en el hosting. | Debería | A confirmar | 1.4.2 |
| RF-ACC-06 | Recuperar la contraseña por mail. | Podría | Confirmado (queda como está; no funciona en el plan gratuito) | 1.4.2 |

### 1.2 Veredas (VER)

| ID | Requisito | Prioridad | Estado | EDT |
|---|---|---|---|---|
| RF-VER-01 | Cargar una vereda con un formulario: dirección (calle, altura, entre calles), ubicación en el mapa, fechas de reclamo y de relevamiento, fotos, tipos de suelo con su medición, cordón, estado, prioridad y observación. | Debe | Confirmado (lista de campos: P-07) | 1.4.4.1 |
| RF-VER-02 | Elegir la ubicación en un mapa: búsqueda por calle y altura o por entre calles, con pin arrastrable y limitada a Colón. | Debe | Confirmado (mapa a usar: P-11) | 1.4.4.4 |
| RF-VER-03 | Ver la ubicación en Google Maps y en Street View 360° desde el detalle. | Debe | Confirmado | 1.4.4.4 |
| RF-VER-04 | Subir una o varias fotos por vereda, verlas y quitarlas. | Debe | Confirmado | 1.4.4.3 |
| RF-VER-05 | Elegir el tipo de suelo de cada pozo (término) de la lista del catálogo; es obligatorio. | Debe | Confirmado | 1.4.4.1 |
| RF-VER-06 | Si el tipo de suelo no está en la lista, agregarlo desde el mismo formulario, sin perder lo cargado, y que quede elegido. | Debe | Confirmado | 1.4.3 |
| RF-VER-07 | Cargar la medición en dos campos: los términos (p. ej., `(2*3)+(5*9)`, sin límite de pozos) y el total en m², que el sistema calcula solo. | Debe | Confirmado | 1.4.4.2 |
| RF-VER-08 | Marcar la casilla «Cordón» para que aparezcan la medición del cordón (términos) y su total en m³. Sin la casilla, esos campos no se ven. | Debe | Confirmado | 1.4.4.2 |
| RF-VER-09 | Permitir guardar una vereda sin medición (puede no estar medida todavía). | Debe | Confirmado | 1.4.4.2 |
| RF-VER-10 | Listar las veredas con buscador por dirección y filtros por estado y por medidas / sin medir. | Debe | Confirmado | 1.4.4.5 |
| RF-VER-11 | Filtrar además por prioridad, tipo de suelo y con / sin paquete. | Debería | A confirmar | 1.4.4.5 |
| RF-VER-12 | Ver el detalle de la vereda con la foto como protagonista y los datos agrupados en un panel lateral. | Debe | Confirmado | 1.4.4.6 |
| RF-VER-13 | Editar una vereda. | Debe | Confirmado | 1.4.4.1 |
| RF-VER-14 | Eliminar una vereda junto con sus fotos. | Debe | A confirmar (P-09) | 1.4.4.1 |
| RF-VER-15 | Elegir el estado y la prioridad de listas fijas. | Debe | Confirmado | 1.4.4.1 |
| RF-VER-16 | Al escribir los términos, mostrar un renglón por pozo con sus medidas, su subtotal y un desplegable de tipo de suelo. | Debe | Confirmado | 1.4.4.2 |
| RF-VER-17 | Ver en el detalle cada pozo con su tipo de suelo, el total en m² y el total en m³ del cordón. | Debe | A confirmar | 1.4.4.6 |

### 1.3 Tipos de suelo (TSU)

| ID | Requisito | Prioridad | Estado | EDT |
|---|---|---|---|---|
| RF-TSU-01 | ABM del catálogo de tipos de suelo: nombre, medida de la baldosa, color y si está disponible. | Debe | Confirmado (nombre y medida) · A confirmar (color, disponible y formato de la medida: P-05) | 1.4.3 |

El alta rápida desde el formulario de vereda es RF-VER-06.

### 1.4 Paquetes (PAQ)

| ID | Requisito | Prioridad | Estado | EDT |
|---|---|---|---|---|
| RF-PAQ-01 | Crear un paquete con nombre, fecha y observación. | Debe | Confirmado (campos: Vigente) | 1.4.5.1 |
| RF-PAQ-02 | Asignar a un paquete veredas ya guardadas, en cualquier momento, eligiendo varias a la vez desde la pantalla del paquete. | Debe | Confirmado (qué veredas se muestran: P-13) | 1.4.5.2 |
| RF-PAQ-03 | Quitar una vereda de un paquete. | Debe | A confirmar | 1.4.5.2 |
| RF-PAQ-04 | Ver el detalle del paquete: sus veredas, la cantidad, el total de m² y de m³, y el avance (% de veredas finalizadas). | Debe | A confirmar (P-14) | 1.4.5.3 |
| RF-PAQ-05 | Listar los paquetes. | Debe | Confirmado | 1.4.5.1 |
| RF-PAQ-06 | Editar y eliminar paquetes. Al eliminar uno, sus veredas quedan sin paquete. | Debe | A confirmar | 1.4.5.1 |
| RF-PAQ-07 | Asociar el paquete a un proveedor. | Debería | A confirmar (P-12) | 1.4.5.1 |
| RF-PAQ-08 | Desde el listado de veredas, seleccionar varias y agregarlas a un paquete. | Debería | A confirmar | 1.4.5.2 |

### 1.5 Home (HOM)

| ID | Requisito | Prioridad | Estado | EDT |
|---|---|---|---|---|
| RF-HOM-01 | Mostrar dos secciones, Veredas y Paquetes, con un resumen y acceso a cada listado. | Debe | Confirmado (contenido del resumen: P-16) | 1.4.7 |

### 1.6 Proveedores (PRO)

| ID | Requisito | Prioridad | Estado | EDT |
|---|---|---|---|---|
| RF-PRO-01 | Registrar proveedores: nombre, apellido y teléfono. | Debería | A confirmar (P-12) | 1.4.6 |

## 2. Reglas de negocio

| ID | Regla | Estado |
|---|---|---|
| RN-01 | Hay un solo rol: todos los usuarios tienen los mismos permisos. | Confirmado |
| RN-02 | Solo un usuario con sesión iniciada puede crear usuarios. | Confirmado |
| RN-03 | El email del usuario no se repite. | Vigente |
| RN-04 | Tipo de suelo es solo un catálogo de baldosas con su medida y no interviene en ningún cálculo. Cada pozo tiene un tipo de suelo; varios pozos pueden tener el mismo. | Confirmado |
| RN-05 | La medición es de la rotura y no se calcula con la medida de la baldosa. | Confirmado |
| RN-06 | Unidades: la rotura de vereda se mide en m² y la de cordón en m³. | Confirmado |
| RN-07 | Una medición tiene uno o más términos. Cada término es el producto de medidas en metros y el total es la suma de los términos. | Confirmado |
| RN-08 | Se acepta coma o punto como separador decimal (`2,4` = `2.4`). | A confirmar (el ejemplo usa los dos) |
| RN-09 | Una vereda sin ninguna medición figura como "sin medir". | Confirmado |
| RN-10 | Una vereda está como máximo en un paquete. | Vigente |
| RN-11 | Al eliminar un paquete, sus veredas no se borran: quedan sin paquete. | Vigente |
| RN-12 | Estado y prioridad se eligen de listas fijas que el usuario no modifica. Estados actuales: Sin definir, Falta medir, Falta foto, Lista para arreglar, Aún no se arregla, En proceso, Finalizado, No corresponde y No se encontró. | Confirmado (listas fijas) · A confirmar (significado de cada estado: P-08) |
| RN-13 | No se puede eliminar un tipo de suelo que usa alguna vereda; se lo marca como no disponible. | A confirmar |
| RN-14 | Una vereda a cargo del frentista no lleva proveedor. | A confirmar (hoy está como comentario en el código, no se controla) |
| RN-15 | Solo se usan suma (`+`) y multiplicación (`*` o `x`). Los paréntesis son opcionales. | Confirmado |
| RN-16 | La contraseña que escribe el usuario tiene entre 8 y 50 caracteres. | Confirmado (máximo 50) · Vigente (mínimo 8) |
| RN-17 | Cada término lleva 2 medidas en m² y 3 en m³. Si no, el sistema indica el término con error y no deja guardar. | Confirmado |
| RN-18 | Una vereda se puede guardar sin paquete y asignarse a uno después. | Confirmado |

## 3. Requisitos no funcionales

| ID | Categoría | Requisito | Estado |
|---|---|---|---|
| RNF-01 | Usabilidad | Interfaz en español, con fechas dd/mm/aaaa y números con coma decimal. | A confirmar |
| RNF-02 | Usabilidad | Pensada para PC; no se optimiza para celular. | Confirmado |
| RNF-03 | Usabilidad | Al escribir una medición se ve el total calculado al instante. Si hay un error, se indica en qué término está. | Confirmado |
| RNF-04 | Seguridad | Todas las páginas exigen sesión iniciada, salvo el inicio de sesión y la recuperación de contraseña. | Confirmado |
| RNF-05 | Seguridad | Las contraseñas se guardan con un hash con sal (p. ej., `PasswordHasher` de ASP.NET Core). Hoy se usa SHA-256 sin sal. La columna queda en 100 caracteres para permitirlo. | A confirmar |
| RNF-06 | Seguridad | Claves y contraseñas fuera del código: User Secrets en desarrollo y `appsettings.Production.json` (ignorado por git) en producción. Incluye las credenciales de Gmail que hoy están en `AccesController`. | Confirmado |
| RNF-07 | Seguridad | Todos los formularios se validan en el servidor y usan token antifalsificación. Los totales de medición los calcula el servidor. | A confirmar |
| RNF-08 | Privacidad | Los datos personales que se carguen (p. ej., nombre y apellido) solo se ven con sesión iniciada. | A confirmar |
| RNF-09 | Costo | No se usan servicios pagos. | Confirmado |
| RNF-10 | Plataforma | ASP.NET Core MVC (.NET 9), EF Core 9 y SQL Server. Los cambios de base se hacen solo con migraciones. | Confirmado (stack) · A confirmar (solo migraciones) |
| RNF-11 | Hosting | El sistema funciona dentro de los límites del plan gratuito de MonsterASP.NET (5 GB, base de 1 GB, 256 MB de RAM). | Confirmado |
| RNF-12 | Almacenamiento | Las fotos se achican al subirlas (p. ej., lado mayor de 1600 px, JPEG al 80 %). Sin achicar, 500 veredas × 2 fotos × 4 MB ≈ 4 GB; achicadas, ≈ 0,3 GB. | A confirmar |
| RNF-13 | Respaldo | Copia manual periódica de la base y de la carpeta de fotos, porque el plan gratuito no incluye backups. Frecuencia a definir. | A confirmar |
| RNF-14 | Rendimiento | Los listados de hasta 1.000 veredas cargan en menos de 3 segundos (con paginación o miniaturas livianas). | A confirmar |
| RNF-15 | Compatibilidad | Funciona en las últimas versiones de Chrome, Edge y Firefox. | A confirmar |
| RNF-16 | Mantenibilidad | ViewModels para los datos de las vistas. El cálculo de la medición vive en un servicio con pruebas unitarias. | Confirmado |
| RNF-17 | Exactitud | Los totales se guardan con 2 decimales en m² y 3 en m³, calculados por el servidor. | A confirmar |
| RNF-18 | Diseño de datos | `Veredas` guarda la fórmula, el total en m² y el cordón; `Roturas` guarda una fila por rotura (orden, medidas, tipo de suelo y subtotal). `TiposSuelo` solo guarda el catálogo. | Confirmado |
| RNF-19 | Calidad | Casos de prueba escritos antes de programar; pruebas automatizadas (unitarias e integración) y E2E, siempre contra una base de pruebas. | Confirmado |

## 4. Trazabilidad con el flujo principal

| Paso del flujo (01, sección 4) | Requisitos |
|---|---|
| 1. Inicia sesión | RF-ACC-01, RF-ACC-03 |
| 2–3. Agrega la vereda, completa el formulario y la guarda | RF-VER-01 a RF-VER-09, RF-VER-15, RF-VER-16 |
| 4–5. Va a Paquetes y asigna varias veredas | RF-PAQ-01, RF-PAQ-02 |
| 6. Home y seguimiento | RF-HOM-01, RF-VER-10, RF-VER-12, RF-VER-17, RF-PAQ-04 |
