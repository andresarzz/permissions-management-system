using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SistemaPermisos.Data;
using SistemaPermisos.Middleware;
using SistemaPermisos.Repositories;
using SistemaPermisos.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml; // Required for EPPlus

var builder = WebApplication.CreateBuilder(args);

// Configure EPPlus LicenseContext
ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Or LicenseContext.Commercial for commercial use

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Make the session cookie HTTP only
    options.Cookie.IsEssential = true; // Make the session cookie essential
});

// Add HttpContextAccessor for accessing HttpContext in services
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Register Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Services
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IUserService, UserService>();

// Configure Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Duración de la cookie de autenticación
        options.SlidingExpiration = true; // Renovar la cookie en cada solicitud
    });

// Configure Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SupervisorOnly", policy => policy.RequireRole("Supervisor"));
    options.AddPolicy("DocenteOnly", policy => policy.RequireRole("Docente"));
    options.AddPolicy("AdminOrSupervisor", policy => policy.RequireRole("Admin", "Supervisor"));

    // Políticas basadas en permisos granulares (si se usan UserPermissions)
    options.AddPolicy("CanManageUsers", policy => policy.RequireClaim("Permission", "ManageUsers"));
    options.AddPolicy("CanManagePermits", policy => policy.RequireClaim("Permission", "ManagePermits"));
    options.AddPolicy("CanManageOmisiones", policy => policy.RequireClaim("Permission", "ManageOmisiones"));
    options.AddPolicy("CanManageReports", policy => policy.RequireClaim("Permission", "ManageReports"));
    options.AddPolicy("CanViewAuditLogs", policy => policy.RequireClaim("Permission", "ViewAuditLogs"));

    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SupervisorPolicy", policy => policy.RequireRole("Supervisor", "Admin"));
    options.AddPolicy("DocentePolicy", policy => policy.RequireRole("Docente", "Supervisor", "Admin"));
});

// Add Razor Runtime Compilation for development
// Razor views are compiled normally; runtime compilation needs an optional extra package.

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage(); // Use developer exception page in development
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Enable static files (CSS, JS, images)
app.UseRouting(); // Enable routing

app.UseSession(); // Enable session middleware
app.UseMiddleware<ErrorHandlingMiddleware>(); // Custom error handling middleware

app.UseAuthentication(); // Enable authentication
app.UseAuthorization(); // Enable authorization

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Set Login as default route

// Database migration and seeding
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        if (context.Database.CanConnect())
        {
            logger.LogInformation("Conexión a la base de datos establecida correctamente.");

            var pendingMigrations = context.Database.GetPendingMigrations();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("Aplicando migraciones pendientes...");
                context.Database.Migrate();
                logger.LogInformation("Migraciones aplicadas correctamente.");
            }

            // Sample accounts are never seeded automatically or in production.
            if (app.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("SeedData:Enabled"))
            {
                await SeedData.Initialize(services);
            }
            logger.LogInformation("Datos semilla inicializados correctamente.");
        }
        else
        {
            logger.LogWarning("No se pudo establecer conexión con la base de datos.");
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error durante la inicialización de la base de datos: {Message}", ex.Message);
    }
}

app.Run();
