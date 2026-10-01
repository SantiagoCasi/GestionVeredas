# Specs — Especificaciones de SistemaVeredas

Acá se guardan las especificaciones técnicas (qué se construye y cómo) y todo lo relacionado con las pruebas. Los requisitos están en [`../Docs/03-requisitos.md`](../Docs/03-requisitos.md); cada spec indica cuáles cubre.

## Especificaciones

| ID | Tema | Sprint | Estado |
|---|---|---|---|
| [SPEC-001](SPEC-001-medicion-y-tipos-de-suelo.md) | Medición de roturas y tipos de suelo en la vereda | 1 | Borrador |
| [SPEC-002](SPEC-002-asignacion-de-veredas-a-paquetes.md) | Asignar veredas guardadas a paquetes | 1 | Borrador |
| [SPEC-003](SPEC-003-contrasenas-y-credenciales.md) | Largo de la contraseña y credenciales fuera del código | 1 | Borrador |
| [SPEC-004](SPEC-004-pruebas-automatizadas.md) | Proyecto de pruebas automatizadas | 1 | Borrador |

Estados: Borrador → Aprobada (OK de Santiago) → Implementada → Verificada (QC).

## Pruebas

| Archivo | Contenido | Lo mantiene |
|---|---|---|
| [pruebas/casos-de-prueba.md](pruebas/casos-de-prueba.md) | Casos de prueba `CP-nnn` y trazabilidad con los requisitos | Tester QA |
| [pruebas/defectos.md](pruebas/defectos.md) | Registro de defectos `DEF-nnn` | Tester QC |
| `pruebas/ejecuciones/` | Un archivo por ronda de pruebas, con el resultado de cada caso | Tester QC |

## Cómo se escribe una spec

La escribe el agente `desarrollador` con estas secciones: objetivo y requisitos que cubre, modelo de datos y migración, cambios por archivo, validaciones y mensajes exactos, casos borde, fuera de alcance y preguntas abiertas. Ver `.claude/agents/desarrollador.md` y `Docs/06-equipo-y-flujo.md`.
