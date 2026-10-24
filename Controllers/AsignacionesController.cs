using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class AsignacionesController(IAsignacionesService servicio, ILogger<AsignacionesController> logger)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || id <= 0)
            return BadRequest();

        var modelo = new AsignacionesViewModel();
        try
        {
            if (id.HasValue)
            {
                var asignacion = await servicio.ObtenerAsync(id.Value, cancellationToken);
                if (asignacion is null)
                    return NotFound();
                modelo.Formulario = new AsignacionFormulario
                {
                    CodAsignacionBus = asignacion.CodAsignacionBus,
                    FechaAsignacion = asignacion.FechaAsignacion,
                    CodBus = asignacion.CodBus,
                    CodPiloto = asignacion.CodPiloto,
                    CodLinea = asignacion.CodLinea
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
    public async Task<IActionResult> Guardar(AsignacionesViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var formulario = modelo.Formulario;
                var resultado = await servicio.GuardarAsync(new AsignacionBus
                {
                    CodAsignacionBus = formulario.CodAsignacionBus,
                    FechaAsignacion = formulario.FechaAsignacion!.Value,
                    CodBus = formulario.CodBus!.Value,
                    CodPiloto = formulario.CodPiloto!.Value,
                    CodLinea = formulario.CodLinea!.Value
                }, cancellationToken);

                switch (resultado)
                {
                    case ResultadoGuardarAsignacion.Guardado:
                        TempData["AsignacionGuardada"] = formulario.CodAsignacionBus == 0
                            ? "Asignación registrada correctamente." : "Asignación actualizada correctamente.";
                        return RedirectToAction(nameof(Index));
                    case ResultadoGuardarAsignacion.BusNoEncontrado:
                        ModelState.AddModelError("Formulario.CodBus", "El bus seleccionado no existe.");
                        break;
                    case ResultadoGuardarAsignacion.PilotoNoEncontrado:
                        ModelState.AddModelError("Formulario.CodPiloto", "El piloto seleccionado no existe.");
                        break;
                    case ResultadoGuardarAsignacion.LineaNoEncontrada:
                        ModelState.AddModelError("Formulario.CodLinea", "La línea seleccionada no existe.");
                        break;
                    case ResultadoGuardarAsignacion.NoEncontrado:
                        return NotFound();
                    case ResultadoGuardarAsignacion.AsignacionUtilizada:
                        ModelState.AddModelError(string.Empty, "Esta asignación ya tiene operaciones. Registra una nueva para cambiar bus, piloto, línea o fecha.");
                        break;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("La asignación {Codigo} ya no estaba disponible al guardar.", modelo.Formulario.CodAsignacionBus);
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

        await CargarDatosAsync(modelo, cancellationToken);
        return View("Index", modelo);
    }

    private async Task CargarDatosAsync(AsignacionesViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.Buses = await servicio.ObtenerBusesAsync(cancellationToken);
            modelo.Pilotos = await servicio.ObtenerPilotosAsync(cancellationToken);
            modelo.Lineas = await servicio.ObtenerLineasAsync(cancellationToken);
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

    private void InformarError(Exception exception, string operacion, AsignacionesViewModel modelo)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al {Operacion} asignaciones. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            operacion, exception.GetType().Name, sql?.Number, sql?.State);
        if (operacion == "consultar")
            modelo.ErrorCarga = true;
        ModelState.AddModelError(string.Empty, $"No se pudieron {operacion} los datos. Intenta nuevamente.");
    }
}
