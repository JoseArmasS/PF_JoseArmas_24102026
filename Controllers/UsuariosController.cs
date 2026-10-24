using Microsoft.AspNetCore.Mvc;
using TransMetroUMG.Models;
using TransMetroUMG.Application;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Domain;


namespace TransMetroUMG.Controllers;

public class UsuariosController(
    ICatalogosUsuariosService catalogosService,
    IUsuariosService usuariosService,
    ILogger<UsuariosController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Crear(CancellationToken cancellationToken)
    {
        var modelo = new CrearUsuarioViewModel();
        await CargarCatalogosAsync(modelo, cancellationToken);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(
    CrearUsuarioViewModel modelo,
    CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var resultado = await usuariosService.CrearAsync(
                    new CrearUsuarioDatos
                    {
                        NombreUsuario = modelo.NombreUsuario,
                        NombreCompleto = modelo.NombreCompleto,
                        CorreoElectronico = modelo.CorreoElectronico,
                        Contrasena = modelo.Contrasena,
                        CodMunicipalidad = modelo.CodMunicipalidad!.Value,
                        CodRol = modelo.CodRol!.Value,
                        Estado = (EstadoUsuario)modelo.CodEstadoUsuario!.Value,
                        FechaExpiracionContrasena = modelo.FechaExpiracionContrasena
                    },
                    cancellationToken);

                switch (resultado)
                {
                    case ResultadoCrearUsuario.Creado:
                        TempData["UsuarioCreado"] =
                            "Usuario registrado correctamente.";
                        return RedirectToAction(nameof(Crear));

                    case ResultadoCrearUsuario.NombreDuplicado:
                        ModelState.AddModelError(
                            nameof(modelo.NombreUsuario),
                            "Ya existe un usuario con ese nombre.");
                        break;

                    case ResultadoCrearUsuario.MunicipalidadNoEncontrada:
                        ModelState.AddModelError(
                            nameof(modelo.CodMunicipalidad),
                            "La municipalidad seleccionada ya no existe.");
                        break;

                    case ResultadoCrearUsuario.RolNoEncontrado:
                        ModelState.AddModelError(
                            nameof(modelo.CodRol),
                            "El rol seleccionado ya no existe.");
                        break;
                }
            }
            catch (ArgumentException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Revisa los datos ingresados; hay campos que no son válidos.");
            }
            catch (DbUpdateException exception)
            {
                var sql = exception.InnerException as SqlException;

                logger.LogError(
                    "Error al crear usuario. SQL {Numero}, estado {Estado}.",
                    sql?.Number, sql?.State);

                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar el usuario. Intenta nuevamente.");
            }
            catch (SqlException exception)
            {
                logger.LogError(
                    "Error al crear usuario. SQL {Numero}, estado {Estado}.",
                    exception.Number, exception.State);

                ModelState.AddModelError(
                    string.Empty,
                    "No se pudo guardar el usuario. Intenta nuevamente.");
            }
        }

        // Limpiar las contraseñas sin perder sus mensajes de validación.
        modelo.Contrasena = string.Empty;
        modelo.ConfirmarContrasena = string.Empty;

        ModelState.SetModelValue(
            nameof(modelo.Contrasena), string.Empty, string.Empty);
        ModelState.SetModelValue(
            nameof(modelo.ConfirmarContrasena), string.Empty, string.Empty);

        await CargarCatalogosAsync(modelo, cancellationToken);
        return View(modelo);
    }

    private async Task CargarCatalogosAsync(CrearUsuarioViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            var catalogos = await catalogosService.ObtenerAsync(cancellationToken);
            modelo.Municipalidades = catalogos.Municipalidades
                .Select(x => new SelectListItem(x.Nombre, x.Codigo.ToString())).ToList();
            modelo.Roles = catalogos.Roles
                .Select(x => new SelectListItem(x.Nombre, x.Codigo.ToString())).ToList();
        }
        catch (SqlException exception)
        {
            
            logger.LogError("No se pudieron cargar los catálogos de usuarios. SQL {Numero}, estado {Estado}.",
                exception.Number, exception.State);
            modelo.ErrorCatalogos = true;
            ModelState.AddModelError(string.Empty,
                "No se pudieron cargar las municipalidades y los roles. Intenta nuevamente o contacta al administrador.");
        }
    }
}
