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

var app = builder.Build();

// Primer usuario: si la tabla Usuarios está vacía, se crea con los datos de "UsuarioInicial"
// (guardalos en User Secrets). Una vez que existe al menos un usuario, esto no hace nada.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
