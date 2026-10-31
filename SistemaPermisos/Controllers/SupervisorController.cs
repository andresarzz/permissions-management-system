using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaPermisos.Data;
using SistemaPermisos.ViewModels;

namespace SistemaPermisos.Controllers
{
    [Authorize(Policy = "SupervisorPolicy")]
    public class SupervisorController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SupervisorController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Dashboard()
        {
            var permisos = _context.Permisos.AsNoTracking()
                .Where(x => x.Estado == "Pendiente");
            var omisiones = _context.OmisionesMarca.AsNoTracking()
                .Where(x => x.Estado == "Pendiente");
            var reportes = _context.ReportesDano.AsNoTracking()
                .Where(x => x.Estado == "Pendiente");

            var model = new SupervisorDashboardViewModel
            {
                TotalPermisosPendientes = await permisos.CountAsync(),
                TotalOmisionesPendientes = await omisiones.CountAsync(),
                TotalReportesPendientes = await reportes.CountAsync(),
                PermisosPendientes = await permisos.Include(x => x.Usuario)
                    .OrderByDescending(x => x.FechaSolicitud).Take(5).ToListAsync(),
                OmisionesPendientes = await omisiones.Include(x => x.Usuario)
                    .OrderByDescending(x => x.FechaSolicitud).Take(5).ToListAsync(),
                ReportesPendientes = await reportes.Include(x => x.Usuario)
                    .OrderByDescending(x => x.FechaReporte).Take(5).ToListAsync(),
                ActividadReciente = await _context.AuditLogs.AsNoTracking()
                    .Include(x => x.Usuario)
                    .OrderByDescending(x => x.FechaHora).Take(10).ToListAsync()
            };

            return View(model);
        }
    }
}
