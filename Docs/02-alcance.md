# 02 — Alcance

> Borrador v0.2 · 30/09/2026

## 1. Objetivo

Desarrollar una aplicación web para registrar las veredas rotas relevadas en Colón (Bs. As.), agruparlas en paquetes para su reparación y seguir el estado de cada vereda y de cada paquete.

## 2. Incluye

| # | Módulo | Qué abarca | Estado |
|---|---|---|---|
| A1 | Acceso y usuarios | Inicio y cierre de sesión. ABM de usuarios con un único rol: solo un usuario con sesión iniciada crea usuarios. Contraseñas de hasta 50 caracteres. | Confirmado |
| A2 | Veredas | Alta, consulta, edición y baja. Ubicación en mapa y Street View, fotos, roturas (cada una con sus medidas y su tipo de suelo) con el total en m², cordón opcional con su medición en m³, estado y prioridad. Búsqueda y filtros. | Confirmado |
| A3 | Tipos de suelo | Catálogo de baldosas con su medida. Alta rápida desde el formulario de vereda. | Confirmado |
| A4 | Paquetes | Alta, consulta, edición y baja. Asignar veredas ya guardadas, varias a la vez, y quitarlas. Totales y avance del paquete. | Confirmado (detalle en 03) |
| A5 | Home | Dos secciones, Veredas y Paquetes, con un resumen y acceso a cada listado. | Confirmado |
| A6 | Proveedores | Registro de albañiles o contratistas que reparan. | A confirmar (P-12) |
| A7 | Publicación | Sitio y base SQL Server en MonsterASP.NET (plan gratuito). | Confirmado |
| A8 | Documentación | Carpetas Docs y Specs, y un manual de uso breve. | Confirmado (Docs y Specs) · A confirmar (manual) |
| A9 | Pruebas | Casos de prueba escritos antes de programar, pruebas automatizadas y pruebas E2E. | Confirmado |

## 3. No incluye

| Fuera del alcance | Motivo |
|---|---|
| Importar la planilla de Excel actual | Se carga todo desde cero (D-03). |
| Roles o permisos distintos | Hay un solo tipo de usuario (D-01). |
| Registro público de usuarios | Los crea un usuario con sesión iniciada (D-02). |
| Versión para celular o app móvil | Se usa en PC (D-10). |
| Servicios pagos (p. ej., Google Maps JavaScript API) | Costo cero (D-08). |
| Recuperación de contraseña por mail en producción | El plan gratuito no permite enviar mails; la función queda como está (D-09). |

## 4. A definir

Pueden entrar o quedar afuera. Se decide en 05:

- Cálculo de la cantidad de baldosas a partir de la medición y la medida del tipo de suelo (P-05).
- Sector o barrio como dato de la vereda (P-10).
- Estado propio del paquete (P-14).
- Exportar un paquete a Excel o PDF para entregarlo al proveedor (P-15).
- Registro de quién creó o modificó cada dato (P-17).

## 5. Supuestos

- Se usa desde una PC con un navegador actualizado y conexión a internet.
- El volumen inicial es del orden de 500 veredas, con al menos una foto cada una.
- Las medidas se toman en metros.
- Una vereda está como máximo en un paquete a la vez (RN-10).

## 6. Restricciones

- **Tecnología:** ASP.NET Core MVC (.NET 9), EF Core 9 y SQL Server. Desarrollo en Visual Studio 2022; código en GitHub.
- **Costo cero:** hosting gratuito y servicios sin costo (OpenStreetMap, Nominatim, Overpass y Google Maps Embed API).
- **Plan gratuito de MonsterASP.NET** (según su página de precios): 5 GB de almacenamiento, una base de datos de 1 GB y 256 MB de RAM. No incluye email, backups diarios ni HTTPS para dominios propios. El subdominio `gestorveredas.runasp.net` sí responde por HTTPS.
- **Publicación:** por Web Deploy desde la PC (`publicar.bat`). No depende de que el repositorio de GitHub sea público o privado.
- **Compilación y pruebas:** en la PC de Santiago; el entorno de Claude no puede descargar el SDK de .NET ni paquetes NuGet (D-25).
- **Ubicación:** la búsqueda en el mapa se limita a Colón (Bs. As.).

## 7. Entregables

1. Aplicación web publicada en `gestorveredas.runasp.net`.
2. Código fuente en GitHub (`SantiagoCasi/GestionVeredas`).
3. Base de datos creada con migraciones de EF Core.
4. Pruebas automatizadas (`SistemaVeredas.Tests`) y casos de prueba (`Specs/pruebas`).
5. Documentación: carpetas Docs y Specs, y manual de uso (a confirmar).

## 8. Criterios de aceptación

- El flujo principal (01, sección 4) se completa de punta a punta en el sitio publicado.
- Todos los requisitos con prioridad **Debe** (03) están implementados y sus casos de prueba pasan.
