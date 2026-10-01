using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using SistemaVeredas.Data;
using SistemaVeredas.Models;
using SistemaVeredas.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Todo el sistema requiere haber iniciado sesión, salvo lo marcado con [AllowAnonymous] (AccessController).
builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Access/Login";
        options.LogoutPath = "/Access/Logout";
        options.AccessDeniedPath = "/Access/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DBSV")));

builder.Services.AddScoped<FotoService>();
builder.Services.AddScoped<PaqueteService>();

// Correo para la recuperación de contraseña: la cuenta y la contraseña van en User Secrets
// (desarrollo) o en appsettings.Production.json (hosting), nunca en el código.
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.Seccion));
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Primer usuario: si la tabla Usuarios está vacía, se crea con los datos de "UsuarioInicial"
// (guardalos en User Secrets). Una vez que existe al menos un usuario, esto no hace nada.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Contra qué base arranca la app: solo servidor y nombre, nunca la cadena completa (puede tener contraseña).
    var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(db.Database.GetConnectionString() ?? string.Empty);
    app.Logger.LogInformation("Base de datos: {Base} en {Servidor}", csb.InitialCatalog, csb.DataSource);

    // Crea las tablas o aplica las migraciones nuevas al arrancar (necesario en el hosting,
    // donde no se corre "dotnet ef database update"). Si la base ya está al día, no hace nada.
    db.Database.Migrate();

    var email = app.Configuration["UsuarioInicial:Email"];
    var contrasena = app.Configuration["UsuarioInicial:Contrasena"];

    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(contrasena) && !db.Usuarios.Any())
    {
        db.Usuarios.Add(new Usuario
        {
            UsNombre = app.Configuration["UsuarioInicial:Nombre"] ?? "Administrador",
            UsApellido = app.Configuration["UsuarioInicial:Apellido"] ?? "Sistema",
            UsEmail = email.Trim(),
            UsContrasena = PasswordService.HashPassword(contrasena),
            UsActivo = true,
            FechaCreacion = DateTime.Now
        });
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// Sirve los archivos que se agregan en tiempo de ejecución (fotos subidas en wwwroot/uploads).
// MapStaticAssets solo sirve los archivos que existían al compilar/publicar.
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

// Necesario para las pruebas de integración (WebApplicationFactory<Program>).
public partial class Program { }
