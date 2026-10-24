using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

[Authorize]
public sealed class RegistroOperacionesController(IRegistroOperacionBusService servicio,
    IOperacionBusesService asignaciones, ILogger<RegistroOperacionesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Crear(int asignacion, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || asignacion <= 0) return BadRequest();
        var modelo = new InicioOperacionBusViewModel { Asignacion = asignacion };
        await CargarAsync(modelo, cancellationToken);
        if (!modelo.ErrorCarga && modelo.DatosAsignacion is null) return NotFound();
        if (modelo.ErrorCarga) return View(modelo);
        return RedirectToAction("Index", "CiclosBuses", new { asignacion, linea = modelo.DatosAsignacion!.CodigoLinea });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(InicioOperacionBusViewModel modelo, CancellationToken cancellationToken)
    {
        await CargarAsync(modelo, cancellationToken);
        if (modelo.ErrorCarga) return View(modelo);
        if (modelo.DatosAsignacion is null) return NotFound();
        // Un formulario abierto antes de actualizar no puede crear una operación sin itinerario.
        return RedirectToAction("Index", "CiclosBuses", new { asignacion = modelo.Asignacion,
            estacion = modelo.EstacionSalida, linea = modelo.DatosAsignacion.CodigoLinea });
    }

    private async Task CargarAsync(InicioOperacionBusViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.DatosAsignacion = (await asignaciones.ConsultarAsync(null, cancellationToken))
                .SingleOrDefault(x => x.Codigo == modelo.Asignacion);
            modelo.Recorrido = await servicio.ObtenerRecorridoAsync(modelo.Asignacion, cancellationToken);
        }
        catch (Exception exception) when (EsErrorSql(exception))
        {
            RegistrarError(exception);
            modelo.ErrorCarga = true;
            ModelState.AddModelError(string.Empty, "No se pudieron consultar los datos de la operación.");
        }
    }

    private static bool EsErrorSql(Exception exception) => exception is SqlException or DbUpdateException
        or InvalidOperationException { InnerException: SqlException };

    private void RegistrarError(Exception exception)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al registrar operación de bus. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            exception.GetType().Name, sql?.Number, sql?.State);
    }
}
