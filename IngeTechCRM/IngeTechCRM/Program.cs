using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IngeTechCRM.Models;

var builder = WebApplication.CreateBuilder(args);

// A�adir servicios al contenedor
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// Configurar la conexi�n a la base de datos
builder.Services.AddDbContext<IngeTechDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// A�adir servicios de Identity para autenticaci�n (opcional si prefieres manejar la autenticaci�n manualmente)
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Cookies";
})
.AddCookie("Cookies", options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// A�adir sesi�n para almacenar datos entre solicitudes
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configurar el pipeline de solicitudes HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();