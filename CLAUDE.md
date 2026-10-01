# SistemaVeredas — guía para Claude y sus agentes

Aplicación web para registrar las veredas rotas relevadas en Colón (Bs. As.), agruparlas en paquetes para repararlas y seguir su estado. Responsable y Product Owner: **Santiago**.

## Stack

- ASP.NET Core MVC (.NET 9), EF Core 9 y SQL Server (base `GestionVeredas`, cadena de conexión `DBSV`).
- Visual Studio 2022 en Windows.
- Publicación por Web Deploy a MonsterASP.NET (plan gratuito) con `publicar.bat` y `Properties/PublishProfiles/MonsterASP.pubxml`.
- Las migraciones se aplican solas al arrancar (`db.Database.Migrate()` en `Program.cs`), también en el hosting.

## Dónde está cada cosa

| Carpeta | Contenido |
|---|---|
| `Docs/` | Planificación: contexto, alcance, requisitos, EDT, pendientes y decisiones, equipo y backlog. **Es la fuente de verdad de los requisitos.** |
| `Specs/` | Especificaciones técnicas (`SPEC-nnn-*.md`) y pruebas (`Specs/pruebas/`). |
| `.claude/agents/` | Agentes del equipo: `project-manager`, `scrum-master`, `desarrollador`, `programador`, `tester-qa`, `tester-qc`. |
| `Controllers/`, `Models/`, `Models/ViewModels/`, `Services/`, `Views/`, `wwwroot/js/` | Código de la aplicación. |
| `SistemaVeredas.Tests/` | Pruebas automatizadas (xUnit), cuando exista. |

## Reglas del proyecto (decididas)

- Un solo tipo de usuario, sin roles. Sin registro público: los usuarios los crea un usuario con sesión iniciada.
- Todo exige sesión, salvo `AccessController` (archivo `AccesController.cs`; se mantiene ese nombre).
- ViewModels (no DTOs) para los datos de las vistas.
- `Estado` y `Prioridad` son enums fijos que elige el usuario.
- **`TipoSuelo` es solo el catálogo de baldosas del mercado con su medida.** No guarda mediciones.
- **Una vereda tiene varias roturas** (tabla `Roturas`: orden, medidas, tipo de suelo obligatorio y subtotal). `Veredas` guarda la fórmula completa `(2*3)+(5*9)`, el total en m² y el cordón (opcional, se muestra con una casilla) con su medición y total en m³.
- Medición: suma de términos; cada término es un producto de medidas en metros, p. ej. `(2*3)+(5*9)`. Solo `+` y `*` (también `x`), coma o punto decimal, 2 medidas por término en m² y 3 en m³. El total lo calcula el servidor.
- Las veredas se asignan a paquetes después de guardarlas, varias a la vez.
- Sin servicios pagos. Mapas: OpenStreetMap/Leaflet para elegir la ubicación y Google Maps Embed API para verla.
- Uso en PC (no se optimiza para celular). Textos de la interfaz en español con voseo ("Elegí", "Podés").

## Forma de trabajo

- **No modificar archivos del proyecto de Santiago sin su OK.** Se trabaja en una copia y se le muestra qué cambia antes de aplicarlo.
- Nunca guardar secretos en el código ni en archivos versionados: User Secrets en desarrollo y `appsettings.Production.json` (ignorado por git) en producción.
- Flujo del equipo, Definición de Listo y Definición de Hecho: `Docs/06-equipo-y-flujo.md`.
- Todo requisito nuevo o cambio de alcance pasa por `Docs/05-pendientes-y-decisiones.md`.
- Las pruebas corren contra una base de pruebas aparte, nunca contra la base real ni el sitio publicado.

## Comandos

```
dotnet build SistemaVeredas.sln        (en la raíz hay .sln y .csproj: hay que nombrar uno)
dotnet test SistemaVeredas.Tests
dotnet ef migrations add <NombreDescriptivo>   (o Add-Migration en la Consola del Administrador de paquetes)
dotnet ef database update                     (o Update-Database)
```
