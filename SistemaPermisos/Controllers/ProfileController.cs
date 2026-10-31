using Microsoft.EntityFrameworkCore;
﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPermisos.Models;
using SistemaPermisos.Repositories;
using SistemaPermisos.Services;
using SistemaPermisos.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaPermisos.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;
        private readonly IUnitOfWork _unitOfWork;

        public ProfileController(IUserService userService, IAuditService auditService, IUnitOfWork unitOfWork)
        {
            _userService = userService;
            _auditService = auditService;
            _unitOfWork = unitOfWork;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserById(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            var model = new ProfileViewModel
            {
                Id = user.Id,
                Nombre = user.Nombre,
                Apellidos = user.Apellidos,
                NombreUsuario = user.NombreUsuario,
                Email = user.Email,
                Rol = user.Rol,
                Cedula = user.Cedula,
                Puesto = user.Puesto,
                Telefono = user.Telefono,
                Departamento = user.Departamento,
                Direccion = user.Direccion,
                FechaNacimiento = user.FechaNacimiento,
                FechaRegistro = user.FechaRegistro,
                UltimoAcceso = user.UltimoAcceso,
                Activo = user.IsActive,
                FotoPerfilUrl = user.FotoPerfilUrl
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserById(int.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserEditViewModel
            {
                Id = user.Id,
                Nombre = user.Nombre,
                Apellidos = user.Apellidos,
                NombreUsuario = user.NombreUsuario,
                Email = user.Email,
                Rol = user.Rol, // Role is not editable by user in profile edit
                Cedula = user.Cedula,
                Puesto = user.Puesto,
                Telefono = user.Telefono,
                Departamento = user.Departamento,
                Direccion = user.Direccion,
                Activo = user.IsActive,
                FechaRegistro = user.FechaRegistro,
                FechaNacimiento = user.FechaNacimiento,
                FotoPerfilActual = user.FotoPerfilUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || model.Id != int.Parse(userId))
            {
                return Unauthorized();
            }

            // Remove role from model state validation as it's not editable by user
            ModelState.Remove("Rol");

            if (ModelState.IsValid)
            {
                // Ensure the role from the original user is preserved
                var originalUser = await _userService.GetUserById(model.Id);
                if (originalUser == null)
                {
                    return NotFound();
                }
                model.Rol = originalUser.Rol; // Set the role back to the original user's role

                var result = await _userService.UpdateUser(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Perfil actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.Message);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized();
                }

                var result = await _userService.ChangePassword(int.Parse(userId), model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = "Contraseña cambiada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, result.Message);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Activity(int pageNumber = 1, int pageSize = 10)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            var auditLogs = _unitOfWork.AuditLogs.Find(a => a.UsuarioId == int.Parse(userId)).OrderByDescending(a => a.FechaHora);
            var paginatedList = await PaginatedList<Models.AuditLog>.CreateAsync(auditLogs.AsNoTracking(), pageNumber, pageSize);

            // Eager load related user for display
            foreach (var log in paginatedList)
            {
                log.Usuario = await _userService.GetUserById(log.UsuarioId);
            }

            return View(paginatedList);
        }
    }
}
