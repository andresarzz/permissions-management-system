using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaPermisos.Models;
using SistemaPermisos.Services;
using SistemaPermisos.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaPermisos.Controllers
{
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IAuditService _auditService;

        public AccountController(IUserService userService, IAuditService auditService)
        {
            _userService = userService;
            _auditService = auditService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                Usuario? user = await _userService.GetUserByEmail(model.UsernameOrEmail);
                if (user == null)
                {
                    user = await _userService.GetUserByUsername(model.UsernameOrEmail);
                }

                if (user != null && user.IsActive && await _userService.CheckPasswordAsync(user, model.Password))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.NombreUsuario),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Role, user.Rol)
                    };

                    var claimsIdentity = new ClaimsIdentity(
                        claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(model.RememberMe ? 1440 : 30) // 24 hours or 30 minutes
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    await _userService.UpdateLastLogin(user.Id);
                    await _auditService.LogAction(user.Id, "Login", $"Usuario {user.NombreUsuario} ha iniciado sesión.");

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    // Redirect based on role
                    switch (user.Rol)
                    {
                        case "Admin":
                            return RedirectToAction("Dashboard", "Admin");
                        case "Supervisor":
                            return RedirectToAction("Dashboard", "Supervisor");
                        case "Docente":
                            return RedirectToAction("Dashboard", "Docente");
                        default:
                            return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError(string.Empty, "Intento de inicio de sesión inválido. Verifique sus credenciales o si su cuenta está activa.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userService.CreateUser(new UserCreateViewModel
                {
                    Nombre = model.Nombre,
                    Apellidos = model.Apellidos,
                    NombreUsuario = model.NombreUsuario,
                    Email = model.Email,
                    Password = model.Password,
                    ConfirmPassword = model.ConfirmPassword,
                    // El registro público nunca permite escoger roles privilegiados.
                    Rol = "Docente",
                    Cedula = model.Cedula,
                    Puesto = model.Puesto,
                    Telefono = model.Telefono,
                    Departamento = model.Departamento,
                    Direccion = model.Direccion,
                    FechaNacimiento = model.FechaNacimiento,
                    Activo = true // New registered users are active by default
                });

                if (result.Success)
                {
                    // Optionally, send a confirmation email here
                    TempData["SuccessMessage"] = "Registro exitoso. Por favor, inicie sesión.";
                    return RedirectToAction("Login", "Account");
                }
                ModelState.AddModelError(string.Empty, result.Message);
            }
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _auditService.LogAction(int.Parse(userId), "Logout", $"Usuario {User.Identity?.Name} ha cerrado sesión.");
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userService.ForgotPassword(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction("ForgotPasswordConfirmation");
                }
                ModelState.AddModelError(string.Empty, result.Message);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Token o correo electrónico inválido.");
            }
            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userService.ResetPassword(model);
                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    return RedirectToAction("ResetPasswordConfirmation");
                }
                ModelState.AddModelError(string.Empty, result.Message);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}
