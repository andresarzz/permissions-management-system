using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaPermisos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SistemaPermisos.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // Ensure database is created and migrations are applied
                await context.Database.MigrateAsync();

                // Look for any users.
                if (context.Usuarios.Any())
                {
                    return;   // DB has been seeded
                }

                // Create roles if they don't exist
                var roles = new string[] { "Admin", "Supervisor", "Docente" };
                foreach (var roleName in roles)
                {
                    if (!context.UserPermissions.Any(p => p.PermissionName == roleName))
                    {
                        context.UserPermissions.Add(new UserPermission { PermissionName = roleName });
                    }
                }
                await context.SaveChangesAsync();

                // Hash password helper
                string HashPassword(string password)
                {
                    using (var sha256 = SHA256.Create())
                    {
                        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                        return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
                    }
                }

                // Las cuentas de ejemplo requieren contraseñas explícitas, no predeterminadas.
                // El programa invoca este método únicamente con SeedData:Enabled en Development.
                string RequireSeedPassword(string name)
                {
                    var password = Environment.GetEnvironmentVariable(name);
                    if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
                        throw new InvalidOperationException($"Configure {name} con al menos 12 caracteres antes de crear usuarios de prueba.");
                    return password;
                }
                var adminPassword = RequireSeedPassword("SEED_ADMIN_PASSWORD");
                var supervisorPassword = RequireSeedPassword("SEED_SUPERVISOR_PASSWORD");
                var docentePassword = RequireSeedPassword("SEED_DOCENTE_PASSWORD");

                // Create Admin User
                var adminUser = new Usuario
                {
                    Nombre = "Admin",
                    Apellidos = "User",
                    NombreUsuario = "admin",
                    Email = "admin@example.com",
                    EmailConfirmed = true,
                    Rol = "Admin",
                    Cedula = "123456789",
                    Puesto = "Administrador de Sistema",
                    Telefono = "8888-8888",
                    Departamento = "IT",
                    Direccion = "Calle Principal 123",
                    FechaNacimiento = new DateTime(1980, 1, 1),
                    FechaRegistro = DateTime.Now,
                    UltimaActualizacion = DateTime.Now,
                    IsActive = true,
                    UltimoAcceso = DateTime.Now
                };
                var passwordHasher = new PasswordHasher<Usuario>();
                adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);
                context.Usuarios.Add(adminUser);
                await context.SaveChangesAsync(); // Save admin first to get its ID

                // Assign all available permissions to Admin
                var allPermissions = new List<string>
                {
                    "CanManageUsers", "CanApprovePermits", "CanApproveOmisiones", "CanResolveReports",
                    "CanViewAuditLogs", "CanExportData", "CanCreateUsers", "CanEditUsers", "CanDeleteUsers",
                    "CanCreatePermits", "CanEditPermits", "CanDeletePermits", "CanCreateOmisiones",
                    "CanEditOmisiones", "CanDeleteOmisiones", "CanCreateReports", "CanEditReports", "CanDeleteReports"
                };

                foreach (var permName in allPermissions)
                {
                    context.UserPermissions.Add(new UserPermission { UsuarioId = adminUser.Id, PermissionName = permName });
                }
                await context.SaveChangesAsync();

                // Create Supervisor User
                var supervisorUser = new Usuario
                {
                    Nombre = "Supervisor",
                    Apellidos = "User",
                    NombreUsuario = "supervisor",
                    Email = "supervisor@example.com",
                    EmailConfirmed = true,
                    Rol = "Supervisor",
                    Cedula = "987654321",
                    Puesto = "Jefe de Departamento",
                    Telefono = "7777-7777",
                    Departamento = "Recursos Humanos",
                    Direccion = "Avenida Central 456",
                    FechaNacimiento = new DateTime(1985, 5, 10),
                    FechaRegistro = DateTime.Now,
                    UltimaActualizacion = DateTime.Now,
                    IsActive = true,
                    UltimoAcceso = DateTime.Now
                };
                supervisorUser.PasswordHash = passwordHasher.HashPassword(supervisorUser, supervisorPassword);
                context.Usuarios.Add(supervisorUser);
                await context.SaveChangesAsync(); // Save supervisor to get its ID

                // Assign supervisor permissions
                var supervisorPermissions = new List<string>
                {
                    "CanApprovePermits", "CanApproveOmisiones", "CanResolveReports", "CanViewAuditLogs"
                };
                foreach (var permName in supervisorPermissions)
                {
                    context.UserPermissions.Add(new UserPermission { UsuarioId = supervisorUser.Id, PermissionName = permName });
                }
                await context.SaveChangesAsync();

                // Create Docente User
                var docenteUser = new Usuario
                {
                    Nombre = "Docente",
                    Apellidos = "User",
                    NombreUsuario = "docente",
                    Email = "docente@example.com",
                    EmailConfirmed = true,
                    Rol = "Docente",
                    Cedula = "112233445",
                    Puesto = "Profesor",
                    Telefono = "6666-6666",
                    Departamento = "Matemáticas",
                    Direccion = "Calle Secundaria 789",
                    FechaNacimiento = new DateTime(1990, 8, 15),
                    FechaRegistro = DateTime.Now,
                    UltimaActualizacion = DateTime.Now,
                    IsActive = true,
                    UltimoAcceso = DateTime.Now
                };
                docenteUser.PasswordHash = passwordHasher.HashPassword(docenteUser, docentePassword);
                context.Usuarios.Add(docenteUser);
                await context.SaveChangesAsync(); // Save docente to get its ID

                // Assign docente permissions
                var docentePermissions = new List<string>
                {
                    "CanCreatePermits", "CanCreateOmisiones", "CanCreateReports"
                };
                foreach (var permName in docentePermissions)
                {
                    context.UserPermissions.Add(new UserPermission { UsuarioId = docenteUser.Id, PermissionName = permName });
                }
                await context.SaveChangesAsync();

                // Add some sample data for Permisos, Omisiones, Reportes
                context.Permisos.AddRange(
                    new Permiso
                    {
                        UsuarioId = docenteUser.Id,
                        TipoPermiso = "Vacaciones",
                        FechaInicio = DateTime.Now.AddDays(10),
                        FechaFin = DateTime.Now.AddDays(15),
                        JornadaCompleta = true,
                        Motivo = "Vacaciones anuales",
                        Estado = "Pendiente",
                        FechaSolicitud = DateTime.Now
                    },
                    new Permiso
                    {
                        UsuarioId = docenteUser.Id,
                        TipoPermiso = "Enfermedad",
                        FechaInicio = DateTime.Now.AddDays(-2),
                        FechaFin = DateTime.Now.AddDays(-1),
                        JornadaCompleta = true,
                        Motivo = "Gripe fuerte",
                        Estado = "Aprobado",
                        FechaSolicitud = DateTime.Now.AddDays(-3),
                        AprobadoPorId = supervisorUser.Id,
                        FechaResolucion = DateTime.Now.AddDays(-2),
                        ComentariosAprobador = "Aprobado por incapacidad médica."
                    }
                );

                context.OmisionesMarca.AddRange(
                    new OmisionMarca
                    {
                        UsuarioId = docenteUser.Id,
                        TipoOmision = "Entrada",
                        FechaOmision = DateTime.Now.Date,
                        HoraOmision = new TimeSpan(8, 0, 0),
                        Motivo = "Olvidé marcar al entrar",
                        Estado = "Pendiente",
                        FechaSolicitud = DateTime.Now
                    },
                    new OmisionMarca
                    {
                        UsuarioId = docenteUser.Id,
                        TipoOmision = "Salida",
                        FechaOmision = DateTime.Now.Date.AddDays(-1),
                        HoraOmision = new TimeSpan(17, 0, 0),
                        Motivo = "Emergencia familiar",
                        Estado = "Rechazado",
                        FechaSolicitud = DateTime.Now.AddDays(-1),
                        AprobadoPorId = supervisorUser.Id,
                        FechaResolucion = DateTime.Now.AddDays(-1),
                        ComentariosAprobador = "No se presentó evidencia válida."
                    }
                );

                context.ReportesDano.AddRange(
                    new ReporteDano
                    {
                        UsuarioId = docenteUser.Id,
                        TipoDano = "Hardware",
                        Descripcion = "Teclado de la sala de cómputo no funciona.",
                        FechaReporte = DateTime.Now,
                        Equipo = "Teclado Logitech K120",
                        Ubicacion = "Sala de Cómputo 1",
                        Estado = "Pendiente"
                    },
                    new ReporteDano
                    {
                        UsuarioId = docenteUser.Id,
                        TipoDano = "Software",
                        Descripcion = "Problema con la instalación de Office en PC 3.",
                        FechaReporte = DateTime.Now.AddDays(-5),
                        Equipo = "PC Aula 3",
                        Ubicacion = "Aula 3",
                        Estado = "Resuelto",
                        ObservacionesResolucion = "Reinstalación de Office 365."
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}
