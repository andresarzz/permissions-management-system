using Microsoft.EntityFrameworkCore;
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPermisos.Repositories;
using SistemaPermisos.Services;
using SistemaPermisos.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaPermisos.Controllers
{
    [Authorize(Policy = "DocentePolicy")]
    public class DocenteController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;

        public DocenteController(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var currentUserId = int.Parse(userId);

            var model = new DocenteDashboardViewModel
            {
                TotalPermisosSolicitados = await _unitOfWork.Permisos.CountAsync(p => p.UsuarioId == currentUserId),
                PermisosAprobados = await _unitOfWork.Permisos.CountAsync(p => p.UsuarioId == currentUserId && p.Estado == "Aprobado"),
                PermisosRechazados = await _unitOfWork.Permisos.CountAsync(p => p.UsuarioId == currentUserId && p.Estado == "Rechazado"),
                PermisosPendientes = await _unitOfWork.Permisos.CountAsync(p => p.UsuarioId == currentUserId && p.Estado == "Pendiente"),

                TotalOmisionesSolicitadas = await _unitOfWork.OmisionesMarca.CountAsync(o => o.UsuarioId == currentUserId),
                OmisionesAprobadas = await _unitOfWork.OmisionesMarca.CountAsync(o => o.UsuarioId == currentUserId && o.Estado == "Aprobado"),
                OmisionesRechazadas = await _unitOfWork.OmisionesMarca.CountAsync(o => o.UsuarioId == currentUserId && o.Estado == "Rechazado"),
                OmisionesPendientes = await _unitOfWork.OmisionesMarca.CountAsync(o => o.UsuarioId == currentUserId && o.Estado == "Pendiente"),

                TotalReportesCreados = await _unitOfWork.ReportesDano.CountAsync(r => r.UsuarioId == currentUserId),
                ReportesResueltos = await _unitOfWork.ReportesDano.CountAsync(r => r.UsuarioId == currentUserId && r.Estado == "Resuelto"),
                ReportesEnProceso = await _unitOfWork.ReportesDano.CountAsync(r => r.UsuarioId == currentUserId && r.Estado == "En Proceso"),
                ReportesPendientes = await _unitOfWork.ReportesDano.CountAsync(r => r.UsuarioId == currentUserId && r.Estado == "Pendiente"),

                MisPermisosRecientes = (await _unitOfWork.Permisos.Find(p => p.UsuarioId == currentUserId).OrderByDescending(p => p.FechaSolicitud).Take(5).ToListAsync()),
                MisOmisionesRecientes = (await _unitOfWork.OmisionesMarca.Find(o => o.UsuarioId == currentUserId).OrderByDescending(o => o.FechaSolicitud).Take(5).ToListAsync()),
                MisReportesRecientes = (await _unitOfWork.ReportesDano.Find(r => r.UsuarioId == currentUserId).OrderByDescending(r => r.FechaReporte).Take(5).ToListAsync()),
                ActividadReciente = (await _unitOfWork.AuditLogs.Find(a => a.UsuarioId == currentUserId).OrderByDescending(a => a.FechaHora).Take(10).ToListAsync())
            };

            // Eager load related user for display
            foreach (var permiso in model.MisPermisosRecientes)
            {
                permiso.Usuario = await _userService.GetUserById(permiso.UsuarioId);
            }
            foreach (var omision in model.MisOmisionesRecientes)
            {
                omision.Usuario = await _userService.GetUserById(omision.UsuarioId);
            }
            foreach (var reporte in model.MisReportesRecientes)
            {
                reporte.Usuario = await _userService.GetUserById(reporte.UsuarioId);
            }
            foreach (var log in model.ActividadReciente)
            {
                log.Usuario = await _userService.GetUserById(log.UsuarioId);
            }

            return View(model);
        }
    }
}
