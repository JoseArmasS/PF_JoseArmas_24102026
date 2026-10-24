using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class CuentaController(IAutenticacionService servicio, ILogger<CuentaController> logger) : Controller
{
    [HttpGet]
    public IActionResult IniciarSesion(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new IniciarSesionViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("InicioSesion")]
    public async Task<IActionResult> IniciarSesion(IniciarSesionViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var usuario = await servicio.ValidarAsync(modelo.NombreUsuario, modelo.Contrasena, cancellationToken);
                if (usuario is not null)
                {
                    var identidad = new ClaimsIdentity(new[] {
                        new Claim(ClaimTypes.NameIdentifier, usuario.Codigo.ToString()),
                        new Claim(ClaimTypes.Name, usuario.Nombre)
                    }, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identidad));
                    return Url.IsLocalUrl(modelo.ReturnUrl) ? LocalRedirect(modelo.ReturnUrl!)
                        : RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "No se pudo iniciar sesión. Revisa tus credenciales y la vigencia de tu cuenta.");
            }
            catch (Exception exception) when (exception is SqlException or DbUpdateException
                || exception is InvalidOperationException { InnerException: SqlException })
            {
                logger.LogError("Error de base de datos al iniciar sesión. Tipo {Tipo}.", exception.GetType().Name);
                ModelState.AddModelError(string.Empty, "No se pudo iniciar sesión. Intenta nuevamente.");
            }
        }
        modelo.Contrasena = string.Empty;
        ModelState.Remove(nameof(modelo.Contrasena));
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CerrarSesion()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(IniciarSesion));
    }
}
