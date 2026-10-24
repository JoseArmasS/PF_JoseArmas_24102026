using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class ParqueosController(IParqueosService servicio, ILogger<ParqueosController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || id <= 0)
            return BadRequest();

        var modelo = new ParqueosViewModel();
        try
        {
            if (id.HasValue)
            {
                var parqueo = await servicio.ObtenerAsync(id.Value, cancellationToken);
                if (parqueo is null)
                    return NotFound();

                modelo.Formulario = new ParqueoFormulario
                {
                    CodParqueo = parqueo.CodParqueo,
                    CodEstacion = parqueo.CodEstacion,
                    NombreParqueo = parqueo.NombreParqueo,
                    DireccionParqueo = parqueo.DireccionParqueo,
                    CapacidadTotal = parqueo.CapacidadTotal,
                    Estado = parqueo.Estado
                };
            }
        }
        catch (SqlException exception)
        {
            InformarError(exception, "consultar", modelo);
        }
        catch (InvalidOperationException exception) when (exception.InnerException is SqlException)
        {
            InformarError(exception, "consultar", modelo);
        }

        await CargarDatosAsync(modelo, cancellationToken);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ParqueosViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var formulario = modelo.Formulario;
                var resultado = await servicio.GuardarAsync(new Parqueo
                {
                    CodParqueo = formulario.CodParqueo,
                    CodEstacion = formulario.CodEstacion!.Value,
                    NombreParqueo = formulario.NombreParqueo,
                    DireccionParqueo = formulario.DireccionParqueo,
                    CapacidadTotal = formulario.CapacidadTotal!.Value,
                    Estado = formulario.Estado!.Value
                }, cancellationToken);

                switch (resultado)
                {
                    case ResultadoGuardarParqueo.Guardado:
                        TempData["ParqueoGuardado"] = formulario.CodParqueo == 0
                            ? "Parqueo registrado correctamente."
                            : "Parqueo actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    case ResultadoGuardarParqueo.EstacionNoEncontrada:
                        ModelState.AddModelError("Formulario.CodEstacion", "La estación seleccionada no existe.");
                        break;
                    case ResultadoGuardarParqueo.NoEncontrado:
                        return NotFound();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("El parqueo {Codigo} ya no estaba disponible al guardar.", modelo.Formulario.CodParqueo);
                ModelState.AddModelError(string.Empty, "El registro ya no está disponible. Recarga la pantalla.");
            }
            catch (DbUpdateException exception)
            {
                InformarError(exception, "guardar", modelo);
            }
            catch (SqlException exception)
            {
                InformarError(exception, "guardar", modelo);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is SqlException)
            {
                InformarError(exception, "guardar", modelo);
            }
        }

        // Recargar también las opciones cuando el formulario contiene errores.
        await CargarDatosAsync(modelo, cancellationToken);
        return View("Index", modelo);
    }

    private async Task CargarDatosAsync(ParqueosViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.Estaciones = await servicio.ObtenerEstacionesAsync(cancellationToken);
            modelo.Registros = await servicio.ConsultarAsync(cancellationToken);
        }
        catch (SqlException exception)
        {
            InformarError(exception, "consultar", modelo);
        }
        catch (InvalidOperationException exception) when (exception.InnerException is SqlException)
        {
            InformarError(exception, "consultar", modelo);
        }
    }

    private void InformarError(Exception exception, string operacion, ParqueosViewModel modelo)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al {Operacion} parqueos. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            operacion, exception.GetType().Name, sql?.Number, sql?.State);

        if (operacion == "consultar")
            modelo.ErrorCarga = true;

        ModelState.AddModelError(string.Empty, $"No se pudieron {operacion} los datos. Intenta nuevamente.");
    }
}

