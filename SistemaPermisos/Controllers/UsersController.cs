using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPermisos.Services;
using SistemaPermisos.ViewModels;
using System.Threading.Tasks;
using SistemaPermisos.Models;
using System.Security.Claims;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace SistemaPermisos.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;
        private readonly SistemaPermisos.Data.ApplicationDbContext _context;

        public UsersController(IUserService userService, IAuditService auditService, SistemaPermisos.Data.ApplicationDbContext context)
        {
            _userService = userService;
            _auditService = auditService;
            _context = context;
        }

        public async Task<IActionResult> Index(int? pageNumber)
        {
            int pageSize = 10;
            var users = await _userService.GetPaginatedUsers(pageNumber ?? 1, pageSize, null, null);
            return View(users);
        }

        public IActionResult Create()
        {
            ViewBag.Roles = new[] { "Admin", "Supervisor", "Docente" };
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "Admin", "Supervisor", "Docente" };
                return View(model);
            }

            var result = await _userService.CreateUser(model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Error al crear el usuario.");
            ViewBag.Roles = new[] { "Admin", "Supervisor", "Docente" };
            return View(model);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetUserById(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new UserEditViewModel
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
                FechaRegistro = user.FechaRegistro,
                Activo = user.IsActive,
                FechaNacimiento = user.FechaNacimiento,
                FotoPerfilActual = user.FotoPerfilUrl
            };

            ViewBag.Roles = new[] { "Admin", "Supervisor", "Docente" };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userToUpdate = await _userService.GetUserById(model.Id);
            if (userToUpdate == null)
            {
                return NotFound();
            }

            var result = await _userService.UpdateUser(model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Error al actualizar el usuario.");
            return View(model);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetUserById(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetUserById(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var result = await _userService.DeleteUser(id);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Error al eliminar el usuario.");
            return View(await _userService.GetUserById(id)); // Return to view with error
        }

        public async Task<IActionResult> ChangeRole(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetUserById(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            var viewModel = new ChangeRoleViewModel
            {
                UserId = user.Id,
                NewRole = user.Rol,
                UserName = user.NombreUsuario
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(ChangeRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new[] { "Admin", "Supervisor", "Docente" };
                return View(model);
            }

            var result = await _userService.ChangeUserRole(model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Error al cambiar el rol del usuario.");
            ViewBag.Roles = new[] { "Admin", "Supervisor", "Docente" };
            return View(model);
        }

        public async Task<IActionResult> ManagePermissions(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userService.GetUserById(id.Value);
            if (user == null)
            {
                return NotFound();
            }

            var userPermissions = await _userService.GetUserPermissions(id.Value);
            var allPermissions = await _userService.GetAllPermissions();

            var viewModel = new ManagePermissionsViewModel
            {
                UserId = user.Id,
                UserName = user.NombreUsuario ?? user.Nombre,
                UserPermissions = userPermissions,
                AllPermissions = allPermissions
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManagePermissions(ManagePermissionsViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If SelectedPermissions is null due to no checkboxes being checked, initialize it
                model.SelectedPermissions ??= new List<string>();
                model.AllPermissions = await _userService.GetAllPermissions(); // Re-populate AllPermissions for view
                return View(model);
            }

            var result = await _userService.ManagePermissions(model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.Message ?? "Error al gestionar los permisos.");
            model.AllPermissions = await _userService.GetAllPermissions(); // Re-populate AllPermissions for view
            return View(model);
        }

        public async Task<IActionResult> AuditLog(int? pageNumber)
        {
            int pageSize = 10;
            var auditLogs = await SistemaPermisos.Models.PaginatedList<AuditLog>.CreateAsync(_context.AuditLogs.Include(a => a.Usuario).OrderByDescending(a => a.FechaHora).AsNoTracking(), pageNumber ?? 1, pageSize);
            return View(auditLogs);
        }
    }
}
