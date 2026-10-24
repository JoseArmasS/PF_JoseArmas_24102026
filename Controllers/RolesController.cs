using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class RolesController(
    IRolesService servicio,
    ILogger<RolesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(
        int? id = null,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || id <= 0)
            return BadRequest();

        var modelo = new RolesViewModel();

        try
        {
            if (id.HasValue)
            {
                var rol = await servicio.ObtenerAsync(
                    id.Value, cancellationToken);

                if (rol is null)
                    return NotFound();

                modelo.Formulario = new RolFormulario
                {
                    CodRol = rol.CodRol,
                    NombreRol = rol.NombreRol,
                    DescripcionRol = rol.DescripcionRol,
                    Estado = rol.Estado
                };
            }
        }
        catch (SqlException exception)
        {
            InformarError(exception, "consultar", modelo);
        }

        await CargarTablaAsync(modelo, cancellationToken);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(
        RolesViewModel modelo,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var formulario = modelo.Formulario;

                var resultado = await servicio.GuardarAsync(new Rol
                {
                    CodRol = formulario.CodRol,
                    NombreRol = formulario.NombreRol,
                    DescripcionRol = formulario.DescripcionRol,
                    Estado = formulario.Estado!.Value
                }, cancellationToken);

                switch (resultado)
                {
                    case ResultadoGuardarRol.Guardado:
                        TempData["RolGuardado"] = formulario.CodRol == 0
                            ? "Rol registrado correctamente."
                            : "Rol actualizado correctamente.";

                        return RedirectToAction(nameof(Index));

                    case ResultadoGuardarRol.NombreDuplicado:
                        ModelState.AddModelError(
                            "Formulario.NombreRol",
                            "Ya existe un rol con ese nombre.");
                        break;

                    case ResultadoGuardarRol.NoEncontrado:
                        return NotFound();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning(
                    "El rol {Codigo} ya no estaba disponible al guardar.",
                    modelo.Formulario.CodRol);

                ModelState.AddModelError(
                    string.Empty,
                    "El registro ya no está disponible. Recarga la pantalla.");
            }
            catch (DbUpdateException exception)
            {
                InformarError(exception, "guardar", modelo);
            }
            catch (SqlException exception)
            {
                InformarError(exception, "guardar", modelo);
            }
        }

        await CargarTablaAsync(modelo, cancellationToken);
        return View("Index", modelo);
    }

    private async Task CargarTablaAsync(
        RolesViewModel modelo,
        CancellationToken cancellationToken)
    {
        try
        {
            modelo.Registros = await servicio.ConsultarAsync(
                cancellationToken);
        }
        catch (SqlException exception)
        {
            InformarError(exception, "consultar", modelo);
        }
    }

    private void InformarError(
        Exception exception,
        string operacion,
        RolesViewModel modelo)
    {
        var sql = exception as SqlException
            ?? exception.InnerException as SqlException;

        logger.LogError(
            "Error al {Operacion} roles. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            operacion,
            exception.GetType().Name,
            sql?.Number,
            sql?.State);

        if (operacion == "consultar")
            modelo.ErrorCarga = true;

        ModelState.AddModelError(
            string.Empty,
            $"No se pudieron {operacion} los datos. Intenta nuevamente.");
    }
}