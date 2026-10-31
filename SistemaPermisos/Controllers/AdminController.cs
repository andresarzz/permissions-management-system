using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPermisos.Repositories;
using SistemaPermisos.Services;
using SistemaPermisos.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaPermisos.Controllers
{
    [Authorize(Policy = "AdminPolicy")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditService _auditService;

        public AdminController(IUserService userService, IUnitOfWork unitOfWork, IAuditService auditService)
        {
            _userService = userService;
            _unitOfWork = unitOfWork;
            _auditService = auditService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel
            {
                TotalUsuarios = await _unitOfWork.Usuarios.CountAsync(),
                PermisosPendientes = await _unitOfWork.Permisos.CountAsync(p => p.Estado == "Pendiente"),
                PermisosAprobados = await _unitOfWork.Permisos.CountAsync(p => p.Estado == "Aprobado"),
                PermisosRechazados = await _unitOfWork.Permisos.CountAsync(p => p.Estado == "Rechazado"),
                OmisionesPendientes = await _unitOfWork.OmisionesMarca.CountAsync(o => o.Estado == "Pendiente"),
                OmisionesAprobadas = await _unitOfWork.OmisionesMarca.CountAsync(o => o.Estado == "Aprobado"),
                OmisionesRechazadas = await _unitOfWork.OmisionesMarca.CountAsync(o => o.Estado == "Rechazado"),
                ReportesPendientes = await _unitOfWork.ReportesDano.CountAsync(r => r.Estado == "Pendiente"),
                ReportesResueltos = await _unitOfWork.ReportesDano.CountAsync(r => r.Estado == "Resuelto"),
                ReportesEnProceso = await _unitOfWork.ReportesDano.CountAsync(r => r.Estado == "En Proceso"),
                ActividadReciente = _unitOfWork.AuditLogs.GetAll().OrderByDescending(a => a.FechaHora).Take(10).ToList()
            };

            // Eager load related user for audit logs
            foreach (var log in model.ActividadReciente)
            {
                log.Usuario = await _userService.GetUserById(log.UsuarioId);
            }

            return View(model);
        }
    }
}
