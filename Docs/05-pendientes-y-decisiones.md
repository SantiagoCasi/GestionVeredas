# 05 — Pendientes y decisiones

> Documento vivo · última actualización 30/09/2026 (v0.2)

Cada pregunta trae una **propuesta**. Si la propuesta sirve, alcanza con aprobarla; la respuesta pasa a "Decisiones tomadas" y los requisitos afectados pasan a `Confirmado`. Los IDs no se reutilizan.

## 1. Preguntas abiertas

### A. Medición y tipo de suelo

| ID | Pregunta | Propuesta | Afecta |
|---|---|---|---|
| P-05 | ¿La medida de la baldosa se usa para algún cálculo (p. ej., cuántas baldosas hacen falta)? | Por ahora es informativa y queda como texto (`40x40`). Si se usa para calcular, pasa a largo y ancho numéricos. | RF-TSU-01 |

### B. Veredas

| ID | Pregunta | Propuesta | Afecta |
|---|---|---|---|
| P-07 | Hoy la vereda tiene Nombre y Apellido: ¿de quién son (frentista, vecino que reclamó)? ¿Falta algún dato del relevamiento (p. ej., quién relevó)? | Repasar las columnas de la planilla actual y ajustar el formulario. | RF-VER-01 |
| P-08 | ¿Qué significa cada estado y cuándo se cambia? "Falta medir" y "Falta foto" también se deducen de los datos cargados. | Mantener la lista fija y que el sistema avise si no coincide (p. ej., "Falta medir" con la medición cargada). | RN-12 |
| P-09 | ¿Se puede eliminar una vereda que está en un paquete? | No: primero hay que quitarla del paquete. | RF-VER-14 |
| P-10 | ¿Hace falta registrar el sector o barrio de la vereda (para filtrar o armar paquetes por zona)? | A definir. | RF-VER-01, RF-VER-11 |
| P-11 | Decisión abierta desde el 25/09: para elegir la ubicación, ¿se usa Google Maps JavaScript API o se agrega una vista satelital gratuita al mapa de OpenStreetMap? | Mantener OpenStreetMap, para seguir con costo cero (D-08). | RF-VER-02 |

### C. Paquetes

| ID | Pregunta | Propuesta | Afecta |
|---|---|---|---|
| P-12 | ¿El paquete se asigna a un proveedor? Hoy es obligatorio y, además, cada vereda tiene su propio proveedor. | El proveedor se define en el paquete; la vereda solo indica si está a cargo del frentista. | RF-PAQ-07, RF-PRO-01 |
| P-13 | ¿Qué veredas se pueden asignar a un paquete? | Cualquiera que no esté en otro paquete, mostrando su estado y si está medida. | RF-PAQ-02 |
| P-14 | ¿El paquete tiene un estado propio (armado, en ejecución, terminado) o alcanza con el avance? | Alcanza con el avance (% de veredas finalizadas). | RF-PAQ-04 |
| P-15 | ¿Hace falta exportar un paquete (Excel o PDF) para entregarlo al proveedor? | Sí, pero en una segunda etapa (prioridad Podría). | Alcance (02, sección 4) |

### D. Home y general

| ID | Pregunta | Propuesta | Afecta |
|---|---|---|---|
| P-16 | ¿Qué muestra cada sección del Home? | Lo que muestra hoy (totales por estado, veredas con y sin paquete, m² medidos) más los m³. | RF-HOM-01 |
| P-17 | ¿Se registra quién creó o modificó cada vereda y cada paquete? | Sí, solo usuario y fecha de alta y de última modificación. | Alcance (02, sección 4) |
| P-18 | ¿Hay una fecha de entrega del proyecto? | Hace falta para armar el cronograma después de la EDT. | PT 1.1.1 |
| P-19 | La página de inicio de sesión publicada se titula "ISFDyT N°124" y el mail de recuperación dice "WebTech". ¿Se cambian por "SistemaVeredas"? | Sí, dentro del sprint 1 (SPEC-003). | RF-ACC-01, RF-ACC-06 |

### Respondidas

| ID | Pregunta | Respuesta |
|---|---|---|
| P-01 | ¿Una vereda puede tener a la vez rotura de vereda y de cordón? | Sí → D-15 |
| P-02 | ¿Cada rotura necesita datos propios o alcanza con el total? | Cada rotura tiene sus datos y una vereda puede tener dos tipos de suelo, cada uno con su medición → D-16 |
| P-03 | ¿Se controla la cantidad de medidas por término? | Sí → D-17 |
| P-04 | ¿Qué operaciones se usan? | Suma y multiplicación → D-18 |
| P-06 | ¿Hay mediciones reales en la tabla `Mediciones`? | No, solo de prueba: se pueden borrar → D-27 |

## 2. Decisiones tomadas

| ID | Decisión | Cuándo |
|---|---|---|
| D-01 | Un solo tipo de usuario (un rol). Puede haber varios usuarios. | Sesiones anteriores; ratificado el 30/09/2026 |
| D-02 | Sin registro público: a los usuarios los crea un usuario con sesión iniciada. | Sesiones anteriores |
| D-03 | No se importa la planilla de Excel: en el sistema publicado se carga todo desde cero. | Sesiones anteriores |
| D-04 | Tipo de suelo es solo un catálogo de baldosas con su medida. La vereda elige de la lista y, si falta uno, se agrega desde el formulario. | 30/09/2026 |
| D-05 | La medición es de la rotura (m² vereda, m³ cordón) y no tiene relación con el tipo de suelo. Puede tener uno o más términos. | 30/09/2026 |
| D-06 | La medición es opcional al crear la vereda, y se tiene que poder filtrar por medidas / sin medir. | Sesiones anteriores |
| D-07 | Mapas: OpenStreetMap (Leaflet) para elegir la ubicación; Google Maps Embed API (gratuita) para ver el mapa y Street View 360°. | Sesiones anteriores |
| D-08 | Sin servicios pagos. | Sesiones anteriores |
| D-09 | Hosting en MonsterASP.NET (plan gratuito). La recuperación por mail queda como está aunque ahí no funcione. | Sesiones anteriores |
| D-10 | Uso en PC; no está pensado para celular. | Sesiones anteriores |
| D-11 | Detalle de vereda con la foto como protagonista y los datos en un panel lateral. | Sesiones anteriores |
| D-12 | ViewModels (no DTOs) para los datos de las vistas. | Sesiones anteriores |
| D-13 | Estado y prioridad son listas fijas que elige el usuario. | Sesiones anteriores |
| D-14 | La planificación vive en la carpeta Docs. Primero se definen alcance, requisitos y EDT; después se toca el código. | 30/09/2026 |
| D-15 | Una vereda puede tener rotura de vereda y de cordón a la vez. El cordón se carga marcando la casilla «Cordón», que muestra su medición y su total en m³; sin la casilla no se ve, porque los cordones son minoría. | 30/09/2026 |
| D-16 | ~~Una vereda tiene hasta dos tipos de suelo, cada uno con su medición.~~ Reemplazada por D-26. | 30/09/2026 |
| D-17 | Cada medición tiene dos campos: los términos y el total, que calcula el sistema. Cada término lleva 2 medidas en m² y 3 en m³; si no, el sistema avisa y no deja guardar. | 30/09/2026 |
| D-18 | En la medición solo se suma y se multiplica. | 30/09/2026 |
| D-19 | ~~Las mediciones se guardan en la tabla Veredas, sin tabla aparte.~~ Ajustada por D-29. TipoSuelo es solo el catálogo de baldosas. | 30/09/2026 |
| D-20 | Las veredas se asignan a los paquetes después de guardarlas, de forma cómoda y varias a la vez. | 30/09/2026 |
| D-21 | La contraseña admite hasta 50 caracteres. | 30/09/2026 |
| D-22 | Las credenciales de Gmail (SMTP) salen del código. | 30/09/2026 |
| D-23 | Las especificaciones se guardan en la carpeta Specs. | 30/09/2026 |
| D-24 | Equipo de agentes con roles: Project Manager, Scrum Master, Desarrollador, Programador, Tester QA y Tester QC (ver 06). | 30/09/2026 |
| D-25 | La compilación y las pruebas corren en la PC de Santiago, manejadas por Claude con control de pantalla. | 30/09/2026 |
| D-26 | Cada término de la medición es un pozo con su propio tipo de suelo (obligatorio); la cantidad de pozos no tiene límite y varios pueden tener el mismo tipo. El tipo de suelo no calcula nada. | 30/09/2026 |
| D-27 | Los datos de la tabla Mediciones son de prueba: la migración los borra. | 30/09/2026 |
| D-28 | Base de desarrollo: `Server=DESKTOP-DTLN15N;Database=GestionVeredas;Trusted_Connection=True;Encrypt=False;`. Base de pruebas: la misma instancia con `Database=GestionVeredas_Pruebas`. | 30/09/2026 |
| D-29 | Se agrega la tabla `Roturas` (una vereda tiene varias): orden, medidas, tipo de suelo obligatorio y subtotal. `Veredas` guarda la fórmula completa, el total en m² y el cordón. | 30/09/2026 |

## 3. Riesgos

| ID | Riesgo | Probabilidad | Impacto | Respuesta |
|---|---|---|---|---|
| R-01 | La contraseña de aplicación de Gmail (cuenta casisantiagopablo@gmail.com) está escrita en el código y el repositorio es público. | Alta (ya ocurrió) | Alto | Sacarla del código (SPEC-003). Revocarla queda pendiente: el 30/09 Google pidió una verificación que no se pudo completar. Más adelante: revocarla y, si hace falta, borrar el historial de git o hacer privado el repositorio. |
| R-02 | Las fotos grandes llenan los 5 GB del hosting. | Media | Alto | Achicar las fotos al subirlas (RNF-12). |
| R-03 | El plan gratuito no tiene backups. | Media | Alto | Copia manual periódica de la base y de las fotos (RNF-13). |
| R-04 | La migración del sprint 1 borra la tabla Mediciones. | Baja | Bajo | Son datos de prueba (D-27). |
| R-05 | La migración se aplica sola al arrancar el sitio publicado. | Media | Alto | Probarla antes en la base de pruebas y hacer una copia de la base publicada antes de publicar. |
| R-06 | Las rondas de prueba dependen de que la PC de Santiago esté disponible. | Media | Medio | Coordinar las rondas; durante la ronda no se usa la PC. |
