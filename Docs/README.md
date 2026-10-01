# Docs — Planificación de SistemaVeredas

En esta carpeta se guarda todo lo relacionado con la planificación del proyecto: contexto, alcance, requisitos, EDT, decisiones, equipo y backlog. Las especificaciones técnicas y las pruebas están en [`../Specs`](../Specs). La regla de trabajo es **primero se define acá, después se toca el código**.

## Documentos

| # | Documento | Qué contiene | Estado |
|---|---|---|---|
| 01 | [Contexto](01-contexto.md) | Proceso actual, flujo del sistema, glosario y la lógica corregida de medición y tipo de suelo | Borrador v0.2 |
| 02 | [Alcance](02-alcance.md) | Objetivo, qué incluye y qué no, supuestos, restricciones y entregables | Borrador v0.2 |
| 03 | [Requisitos](03-requisitos.md) | Requisitos funcionales, no funcionales y reglas de negocio | Borrador v0.2 |
| 04 | [EDT](04-edt.md) | Estructura de Desglose del Trabajo y su diccionario | Borrador v0.2 (se ajusta al aprobar 02 y 03) |
| 05 | [Pendientes y decisiones](05-pendientes-y-decisiones.md) | Preguntas abiertas con propuesta, decisiones y riesgos | Documento vivo |
| 06 | [Equipo y flujo](06-equipo-y-flujo.md) | Roles (agentes), flujo de trabajo, Definición de Listo y de Hecho, defectos y entorno de pruebas | v0.1 |
| 07 | [Backlog](07-backlog.md) | Historias de usuario y sprint en curso | Documento vivo |

## Convenciones

- **Estado de cada ítem:** `Confirmado` = definido y aprobado · `A confirmar` = propuesta pendiente de aprobación · `Vigente` = así funciona hoy el sistema, falta validarlo.
- **Prioridad (MoSCoW):** Debe · Debería · Podría · No por ahora.
- **Identificadores:** `RF-XXX-nn` requisito funcional (XXX = módulo) · `RNF-nn` requisito no funcional · `RN-nn` regla de negocio · `P-nn` pregunta · `D-nn` decisión · `R-nn` riesgo · `HU-nn` historia de usuario · `PT x.y.z` paquete de trabajo de la EDT · `SPEC-nnn` especificación · `CP-nnn` caso de prueba · `DEF-nnn` defecto.
- Un ítem pasa a `Confirmado` cuando se aprueba, y la decisión se anota en 05.

## Orden de trabajo

1. Cerrar alcance (02) y requisitos (03) respondiendo las preguntas de 05.
2. Aprobar la EDT (04).
3. Especificar (Specs), escribir los casos de prueba y recién entonces programar, siguiendo el flujo de 06.

## Historial

| Versión | Fecha | Cambio |
|---|---|---|
| 0.1 | 30/09/2026 | Primer borrador, a partir de lo definido el 30/09 y en sesiones anteriores. |
| 0.2 | 30/09/2026 | Respuestas P-01 a P-04 y nuevas decisiones (D-15 a D-25): roturas con su tipo de suelo (tabla Roturas), casilla Cordón, asignación a paquetes después de guardar, contraseña hasta 50 y credenciales fuera del código. Se suman riesgos, equipo (06) y backlog (07). |
