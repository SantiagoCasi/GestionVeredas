# SPEC-004 — Proyecto de pruebas automatizadas

> Borrador v0.1 · 30/09/2026 · Autor: agente `desarrollador` · Sprint 1 · Historia HU-16 (va primero)

## 1. Objetivo y requisitos que cubre

Crear `SistemaVeredas.Tests` (xUnit) con pruebas **unitarias** y de **integración** que corran contra una **base de pruebas aparte**, y dejar definido cómo QC levanta la app contra esa misma base para las pruebas **E2E** con el navegador. La Definición de Hecho de todas las historias pide las pruebas en verde, por eso esta va primero.

| ID | Qué se cubre acá |
|---|---|
| RNF-19 | Pruebas automatizadas (unitarias e integración) y E2E, siempre contra una base de pruebas. |
| RNF-16 | El cálculo de la medición (`MedicionService`, SPEC-001) tiene pruebas unitarias. |
| D-25 | Compilación y pruebas en la PC de Santiago; la salida queda en `TestResults/`. |
| HU-16 | Criterios 1 a 4 del backlog. |

## 2. Base de datos de pruebas

- Nombre: **`GestionVeredas_Pruebas`**. Nunca `GestionVeredas` ni la base del hosting.
- Cadena de conexión: variable de entorno **`SISTEMAVEREDAS_TEST_DB`**. Si no está, se usa la misma instancia de SQL Server de la base de desarrollo (D-28), con otra base:

  ```
  Server=DESKTOP-DTLN15N;Database=GestionVeredas_Pruebas;Trusted_Connection=True;Encrypt=False;
  ```

  La base de desarrollo es `Server=DESKTOP-DTLN15N;Database=GestionVeredas;Trusted_Connection=True;Encrypt=False;` (D-28): misma instancia, **otra base**. Usa autenticación de Windows, sin contraseña, así que la cadena puede estar en el código de las pruebas. Cuidado: como comparten instancia, la traba de abajo es lo que evita borrar `GestionVeredas`.
- **Traba de seguridad:** antes de tocar la base, el fixture lee el nombre de la base con `SqlConnectionStringBuilder.InitialCatalog` y, si **no termina en `_Pruebas`**, lanza `InvalidOperationException("La base de pruebas tiene que terminar en _Pruebas. Revisá SISTEMAVEREDAS_TEST_DB.")` y no corre nada. Esto impide borrar la base real por un error de configuración.
- **Ciclo de vida:** en cada corrida de `dotnet test` el fixture **borra** la base (`EnsureDeletedAsync`) y la app la **vuelve a crear con las migraciones** del proyecto al arrancar (`db.Database.Migrate()` en `Program.cs`, que ya existe). Así cada corrida prueba también que las migraciones se aplican desde cero (criterio 3 de HU-16; ayuda con R-05, pero no reemplaza probar la migración sobre una copia de la base publicada).
- **Modelo de datos y migración de la app:** esta spec **no** agrega tablas ni migraciones.

## 3. Estructura

```
SistemaVeredas/                         ← raíz del repo
├─ SistemaVeredas.csproj                ← web (se le excluye la carpeta de pruebas)
├─ SistemaVeredas.sln                   ← se le agrega el proyecto de pruebas
├─ TestResults/                         ← salida de dotnet test (ignorada por git)
└─ SistemaVeredas.Tests/
   ├─ SistemaVeredas.Tests.csproj
   ├─ xunit.runner.json
   ├─ Unitarias/
   │  ├─ MedicionServiceTests.cs
   │  ├─ PaqueteTotalesTests.cs          (cuando exista SPEC-002)
   │  └─ ContrasenaValidacionTests.cs    (cuando exista SPEC-003)
   └─ Integracion/
      ├─ AppFactory.cs                   (WebApplicationFactory + base de pruebas)
      ├─ BaseDePruebasCollection.cs
      ├─ ClienteExtensions.cs            (login y token antifalsificación)
      ├─ InfraestructuraTests.cs
      └─ AccesoTests.cs
```

## 4. Cambios por archivo

### 4.1 `SistemaVeredas.csproj` (web)

El csproj web está en la raíz, y el SDK Web incluye **todos** los `.cs`, `.json`, `.cshtml`, etc. de las subcarpetas. Sin excluirla, la carpeta de pruebas se compilaría dentro de la app (errores por `Xunit` sin referencia) y sus `.json` (incluidos los de su `bin/`) se **publicarían** en el hosting. Se agrega:

```xml
<ItemGroup>
  <Compile Remove="SistemaVeredas.Tests\**" />
  <Content Remove="SistemaVeredas.Tests\**" />
  <EmbeddedResource Remove="SistemaVeredas.Tests\**" />
  <None Remove="SistemaVeredas.Tests\**" />

  <Content Remove="TestResults\**" />
  <None Remove="TestResults\**" />
</ItemGroup>
```

### 4.2 `SistemaVeredas.Tests/SistemaVeredas.Tests.csproj` (nuevo)

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.x.x" />
    <PackageReference Include="xunit" Version="2.9.x" />
    <PackageReference Include="xunit.runner.visualstudio" Version="x.x.x">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.x" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\SistemaVeredas.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <None Update="xunit.runner.json" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

</Project>
```

**Versiones:** se indican las familias; el programador elige la última de cada una al restaurar y deja el número exacto (sin comodines):

| Paquete | Familia | Nota |
|---|---|---|
| `xunit` | 2.9.x | xUnit v2 (no v3). |
| `Microsoft.NET.Test.Sdk` | 17.x | |
| `xunit.runner.visualstudio` | la última que soporte xUnit v2 y net9.0 | Para el Explorador de pruebas de VS y `dotnet test`. |
| `Microsoft.AspNetCore.Mvc.Testing` | 9.0.x | Misma línea 9.0 que EF Core 9.0.20 y el runtime de la app. No usar 10.x. |

EF Core y `Microsoft.Data.SqlClient` llegan por la referencia al proyecto web; no se agregan de nuevo.

`xunit.runner.json`:

```json
{ "parallelizeTestCollections": true, "diagnosticMessages": false }
```

Las unitarias corren en paralelo; todas las de integración van en **una sola colección** (sección 4.6) y por lo tanto de a una, porque comparten la base.

### 4.3 `SistemaVeredas.sln`

Se agrega el proyecto (desde la raíz): `dotnet sln SistemaVeredas.sln add SistemaVeredas.Tests/SistemaVeredas.Tests.csproj` (o en VS: *Agregar → Proyecto existente*).

### 4.4 `.gitignore`

Se agrega:

```
# Resultados de las pruebas
TestResults/
```

### 4.5 `Program.cs`

1. Al final del archivo, para que `WebApplicationFactory<Program>` pueda ver la clase que generan las instrucciones de nivel superior:

   ```csharp
   // Necesario para las pruebas de integración (WebApplicationFactory<Program>).
   public partial class Program { }
   ```

2. Dentro del bloque que ya hace `db.Database.Migrate()`, **antes** de migrar, un log que diga contra qué base arranca la app (solo servidor y nombre, **nunca** la cadena completa, que puede tener contraseña):

   ```csharp
   var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(db.Database.GetConnectionString());
   app.Logger.LogInformation("Base de datos: {Base} en {Servidor}", csb.InitialCatalog, csb.DataSource);
   ```

   QC lo usa para confirmar que la app levantó contra `GestionVeredas_Pruebas` antes de probar (sección 6).

No cambia nada más en `Program.cs`. La lectura de `DBSV` dentro de `AddDbContext` es diferida (lambda), y `UsuarioInicial` se lee después de `Build()`, así que la configuración de las pruebas les llega.

### 4.6 `Integracion/AppFactory.cs`

```csharp
public class AppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string BaseDefault =
        "Server=DESKTOP-DTLN15N;Database=GestionVeredas_Pruebas;Trusted_Connection=True;Encrypt=False;";

    public string CadenaConexion { get; } =
        Environment.GetEnvironmentVariable("SISTEMAVEREDAS_TEST_DB") is { Length: > 0 } cs ? cs : BaseDefault;

    // Usuario de prueba: se siembra con UsuarioInicial (Program.cs lo crea si la tabla Usuarios está vacía).
    public string EmailPrueba => "pruebas@sistemaveredas.local";
    public string ContrasenaPrueba { get; } = Guid.NewGuid().ToString("N"); // 32 caracteres, distinta en cada corrida

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Entorno Development (el default de WebApplicationFactory): así funcionan los archivos estáticos
        // de MapStaticAssets. Como en Development se leen los User Secrets de Santiago, TODO lo que importa
        // se pisa acá abajo.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DBSV"] = CadenaConexion,
                ["UsuarioInicial:Email"] = EmailPrueba,
                ["UsuarioInicial:Contrasena"] = ContrasenaPrueba,
                ["UsuarioInicial:Nombre"] = "Usuario",
                ["UsuarioInicial:Apellido"] = "Pruebas",
                ["Smtp:Usuario"] = "",       // sin credenciales reales (SPEC-003)
                ["Smtp:Contrasena"] = "",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Doble seguro: se reemplaza el DbContext por uno con la cadena de pruebas.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>(); // EF Core 9 acumula configuraciones
            services.AddDbContext<AppDbContext>(o => o.UseSqlServer(CadenaConexion));

            // Nunca mandar mails reales (cuando exista IEmailService, SPEC-003).
            // services.RemoveAll<IEmailService>();
            // services.AddSingleton<IEmailService, EmailServiceFalso>();
        });
    }

    public async Task InitializeAsync()
    {
        VerificarQueEsBaseDePruebas(CadenaConexion); // lanza si no termina en _Pruebas
        var opciones = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(CadenaConexion).Options;
        await using var db = new AppDbContext(opciones);
        await db.Database.EnsureDeletedAsync();
        _ = Server; // arranca la app: Program.cs aplica las migraciones y siembra el usuario
    }

    // Implementación explícita: WebApplicationFactory ya tiene un DisposeAsync que devuelve ValueTask.
    // La base no se borra al terminar: queda para revisarla si algo falló.
    Task IAsyncLifetime.DisposeAsync() => base.DisposeAsync().AsTask();

    public AppDbContext CrearDbContext() =>
        Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
}
```

`BaseDePruebasCollection.cs`:

```csharp
[CollectionDefinition("Base de pruebas")]
public class BaseDePruebasCollection : ICollectionFixture<AppFactory> { }
```

Todas las clases de integración llevan `[Collection("Base de pruebas")]` y reciben `AppFactory` por constructor.

**Aislamiento de datos:** la base se recrea una vez por corrida, no por prueba. Cada prueba crea sus propios datos con valores únicos (p. ej., `Calle = $"Prueba {Guid.NewGuid():N}"`) y solo afirma sobre esos datos; ninguna prueba supone tablas vacías, salvo el usuario sembrado.

### 4.7 `Integracion/ClienteExtensions.cs` (login real)

El login es el real: GET del formulario, se toma el token antifalsificación del HTML y se hace el POST. La cookie de sesión y la del token quedan en el `HttpClient` (maneja cookies por defecto).

```csharp
public static class ClienteExtensions
{
    static readonly Regex Token = new(
        "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"", RegexOptions.Compiled);

    public static HttpClient CrearCliente(this AppFactory f) => f.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,              // para afirmar sobre los 302
        BaseAddress = new Uri("https://localhost")
    });

    public static async Task<string> ObtenerTokenAsync(this HttpClient c, string url)
    {
        var html = await c.GetStringAsync(url);
        var m = Token.Match(html);
        Assert.True(m.Success, $"No se encontró el token antifalsificación en {url}");
        return WebUtility.HtmlDecode(m.Groups[1].Value);
    }

    public static async Task<HttpResponseMessage> PostFormAsync(this HttpClient c, string urlFormulario,
        string urlPost, IDictionary<string, string> campos)
    {
        var token = await c.ObtenerTokenAsync(urlFormulario);
        var datos = new Dictionary<string, string>(campos) { ["__RequestVerificationToken"] = token };
        return await c.PostAsync(urlPost, new FormUrlEncodedContent(datos));
    }

    public static async Task<HttpClient> CrearClienteConSesionAsync(this AppFactory f)
    {
        var c = f.CrearCliente();
        var r = await c.PostFormAsync("/Access/Login", "/Access/Login", new Dictionary<string, string>
        {
            ["UsEmail"] = f.EmailPrueba,
            ["UsContrasena"] = f.ContrasenaPrueba,
            ["Recordarme"] = "false"
        });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        return c;
    }
}
```

El token se toma **después** de iniciar sesión, del formulario que se va a enviar (el token queda ligado al usuario).

### 4.8 Pruebas iniciales (mínimo para HU-16)

**`Unitarias/MedicionServiceTests.cs`** — sobre `MedicionService` de SPEC-001. La firma exacta la fija SPEC-001; los casos son estos (con `[Theory]`; como `InlineData` no acepta `decimal`, el esperado va como `string` o `double` y se convierte):

| Entrada | Unidad | Esperado |
|---|---|---|
| `(2*3)+(5*9)` | m² | 51,00 (subtotales 6,00 y 45,00, uno por pozo) |
| `2*3+5*9` (sin paréntesis) | m² | 51,00 |
| `2,4x3` | m² | 7,20 |
| `2.4*3` | m² | 7,20 |
| `1*0,5*0,3` | m³ | 0,150 |
| ` 2 * 3 ` (espacios) | m² | 6,00 |
| vacío o solo espacios | m² | sin medición (no es error) |
| `2*3*4` | m² | error en el término 1 (3 medidas en m²) |
| `(2*3)+(4*5*6)` | m³ | error en el término 1 (2 medidas en m³) |
| `2*3+` | m² | error (término vacío) |
| `2-3`, `2/3`, `abc` | m² | error (solo `+`, `*` y `x`) |

Redondeo: 2 decimales en m² y 3 en m³ (RNF-17, *A confirmar*).

**`Integracion/InfraestructuraTests.cs`**

1. `UsaLaBaseDePruebas`: `CrearDbContext().Database.GetDbConnection().Database` termina en `_Pruebas`.
2. `NoQuedanMigracionesPendientes`: `GetPendingMigrationsAsync()` está vacío.
3. (Después de SPEC-001) `NoExisteLaTablaMediciones`: `SELECT OBJECT_ID('dbo.Mediciones')` es `NULL`.

**`Integracion/RoturasTests.cs`** (después de SPEC-001; tabla `Roturas`, D-26 y D-29). Se crean veredas por el formulario real (POST a `/Veredas/Create` y `/Veredas/Edit` con token) y se afirma sobre la base:

1. `Crear_GuardaUnaRoturaPorTermino`: fórmula `(2*3)+(5*9)` con dos tipos de suelo → 2 filas en `Roturas` con `Orden` 1 y 2, `Medidas` de cada término, su `TipoSueloId` y `SubtotalM2` 6,00 y 45,00; `Veredas.TotalM2` = 51,00 y `Veredas.Medicion` guarda la fórmula.
2. `Crear_MismoTipoEnVariosPozos`: tres términos con el mismo tipo de suelo → 3 filas, se acepta.
3. `Crear_TerminoSinTipoDeSuelo_NoGuarda`: un término sin tipo → 200 con el error del término y ni la vereda ni sus roturas se guardan.
4. `Editar_ReemplazaLasRoturas`: de 2 términos a 1 → queda 1 fila en `Roturas` (no quedan huérfanas) y `TotalM2` se recalcula.
5. `Crear_SinMedicion`: vereda sin medición → 0 filas en `Roturas`, `TotalM2` `NULL` y figura como sin medir.
6. `Cordon_SeGuardaEnVeredas`: `TieneCordon` con `MedicionCordon` `1*0,5*0,3` → `TotalCordonM3` = 0,150 y no crea filas en `Roturas`; sin la casilla, `MedicionCordon` y `TotalCordonM3` quedan `NULL`.
7. `Eliminar_BorraSusRoturas`: al eliminar la vereda, sus filas de `Roturas` desaparecen (cascada).
8. `TipoSueloEnUso_NoSeBorra`: la FK `Roturas.TipoSueloId` impide borrar un tipo de suelo usado (RN-13; el comportamiento exacto lo fija SPEC-001).
9. `ElTotalLoCalculaElServidor`: un POST que manda un `TotalM2` o `SubtotalM2` falso → se ignora y se guarda el calculado (RNF-07).

Los campos del formulario (nombres de los inputs de cada término) los fija SPEC-001; las pruebas se ajustan a esos nombres.

**`Integracion/AccesoTests.cs`**

1. `SinSesion_RedirigeAlLogin`: GET `/Veredas` → 302 con `Location` que empieza con `/Access/Login`.
2. `LoginCorrecto_Entra`: `CrearClienteConSesionAsync` → GET `/Veredas` → 200.
3. `LoginIncorrecto_MuestraMensaje`: POST con contraseña equivocada → 200 y el HTML contiene "Email o contraseña incorrectos."
4. `PostSinToken_Rechaza`: POST a `/Access/Login` sin `__RequestVerificationToken` → 400.

Cada spec siguiente agrega sus pruebas (ver sección 7 de SPEC-002 y SPEC-003).

## 5. Cómo se corre (PC de Santiago, D-25)

Desde la raíz del repo:

```
dotnet build SistemaVeredas.sln
dotnet test SistemaVeredas.Tests --logger "trx;LogFileName=ultima.trx" --logger "console;verbosity=normal" --results-directory TestResults
```

- La salida completa queda en `TestResults/ultima.trx` (se pisa en cada corrida, así Claude siempre lee el mismo archivo).
- La forma corta del pedido (`dotnet test SistemaVeredas.Tests --logger trx --results-directory TestResults`) también sirve; genera un `.trx` con fecha en el nombre.
- Otra base: antes, en la misma terminal, `$env:SISTEMAVEREDAS_TEST_DB = "<cadena>"` (el nombre de la base tiene que terminar en `_Pruebas`).
- Se usa `dotnet build SistemaVeredas.sln` y no `dotnet build` a secas: en la raíz hay un `.csproj` y un `.sln`, y `dotnet build` sin argumento falla con MSB1011 ("hay más de un archivo de proyecto o solución"). `CLAUDE.md` dice `dotnet build`; el PM debería corregirlo.
- En Visual Studio: *Prueba → Explorador de pruebas → Ejecutar todas*.

## 6. Cómo QC levanta la app contra la base de pruebas (E2E)

En una terminal de PowerShell nueva, desde la raíz del repo. Las variables valen solo para esa terminal y, como las de entorno pisan a los User Secrets, la app no toca la base real:

```powershell
# 1) (Opcional) Empezar con la base vacía: la borra por nombre, sin tocar otra.
sqlcmd -S "DESKTOP-DTLN15N" -E -Q "IF DB_ID('GestionVeredas_Pruebas') IS NOT NULL BEGIN ALTER DATABASE GestionVeredas_Pruebas SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE GestionVeredas_Pruebas; END"

# 2) Base de pruebas y usuario de QC (el usuario se crea solo si la tabla Usuarios está vacía).
$env:ConnectionStrings__DBSV = "Server=DESKTOP-DTLN15N;Database=GestionVeredas_Pruebas;Trusted_Connection=True;Encrypt=False;"
$env:UsuarioInicial__Email = "qc@sistemaveredas.local"
$env:UsuarioInicial__Contrasena = "<contraseña de prueba que elige QC; nunca la real>"
$env:UsuarioInicial__Nombre = "QC"
$env:UsuarioInicial__Apellido = "Pruebas"

# 3) Que la recuperación no mande mails reales: un servidor SMTP que no existe (falla enseguida).
$env:Smtp__Host = "localhost"
$env:Smtp__Puerto = "2525"

# 4) Levantar la app en https://localhost:7243
dotnet run --launch-profile https
```

- **Antes de probar**, QC confirma en la consola la línea `Base de datos: GestionVeredas_Pruebas en DESKTOP-DTLN15N` (sección 4.5). Si dice `GestionVeredas` (la de desarrollo) u otra cosa, corta con Ctrl+C y no prueba.
- En PowerShell no se puede "vaciar" una variable con `$env:X = ""` (eso la borra y volvería a valer el User Secret); por eso el paso 3 apunta a un servidor inexistente en lugar de dejar vacías las credenciales. El caso "mail enviado" se cubre con la prueba de integración con el servicio falso (SPEC-003).
- Si `sqlcmd` no está instalado, la base se borra desde VS: *Ver → Explorador de objetos de SQL Server → DESKTOP-DTLN15N → Bases de datos → GestionVeredas_Pruebas → Eliminar* (con "Cerrar conexiones existentes").
- La primera vez puede hacer falta confiar en el certificado de desarrollo: `dotnet dev-certs https --trust`.
- `dotnet test` también borra y recrea `GestionVeredas_Pruebas`: no correr las pruebas automatizadas mientras QC está en una ronda E2E.

## 7. Validaciones y mensajes

| Situación | Mensaje |
|---|---|
| La cadena de pruebas no apunta a una base `*_Pruebas` | La base de pruebas tiene que terminar en _Pruebas. Revisá SISTEMAVEREDAS_TEST_DB. |
| No se encontró el token en el HTML | No se encontró el token antifalsificación en {url} |

(No hay mensajes nuevos para el usuario de la app.)

## 8. Casos borde

1. **`SISTEMAVEREDAS_TEST_DB` apunta a `GestionVeredas` por error:** la traba lanza antes de borrar nada.
2. **Santiago tiene en User Secrets la cadena de la base real y las credenciales SMTP:** las pisan la configuración en memoria y el reemplazo del `DbContext` y de `IEmailService`. La prueba `UsaLaBaseDePruebas` lo confirma.
3. **La instancia DESKTOP-DTLN15N no responde** (servicio de SQL Server detenido): falla `InitializeAsync` con el error de conexión de SqlClient; se inicia el servicio o se usa la variable de entorno con otra instancia.
4. **La base de pruebas está abierta en SSMS/VS:** `EnsureDeletedAsync` puede fallar por conexiones abiertas; hay que cerrarlas.
5. **Una migración nueva está mal:** la app falla al arrancar en la prueba y todas las de integración fallan con ese error: es el aviso buscado (R-05).
6. **Pruebas corriendo en paralelo sobre la base:** no pasa, porque todas las de integración están en la misma colección.
7. **`MapStaticAssets` en el entorno de pruebas:** por eso se usa `Development`. Si igual diera problemas, se puede usar `builder.UseEnvironment("Pruebas")`, pero hay que verificar que se sirvan los estáticos.
8. **Misma instancia que la base de desarrollo:** el usuario de Windows de Santiago necesita permiso para crear y borrar bases en la instancia (ya lo tiene si creó `GestionVeredas` con migraciones). La traba `_Pruebas` impide borrar `GestionVeredas`.

## 9. Fuera de alcance

- Automatizar las E2E con Playwright o Selenium: QC las ejecuta con el navegador integrado de la app de Claude (Docs/06).
- Integración continua (GitHub Actions): no hay SQL Server ni hosting de CI gratuito definido.
- Cobertura de código (coverlet) y Respawn para limpiar datos entre pruebas.
- Probar la migración sobre una copia de la base publicada (R-05): tarea aparte antes de publicar.

## 10. Preguntas abiertas para el PM

- **Q-01:** ¿se aprueba agregar al `Program.cs` el log con el nombre de la base al arrancar (también aparece en el log del hosting, sin contraseña)?
- **Q-02:** corregir en `CLAUDE.md` el comando `dotnet build` por `dotnet build SistemaVeredas.sln` (sección 5).
- **Q-03:** las pruebas de `MedicionService` dependen de la firma que fije SPEC-001; se coordinan cuando esa spec esté aprobada.
