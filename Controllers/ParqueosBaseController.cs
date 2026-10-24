using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class ParqueosBaseController(IParqueosBaseService servicio, ILogger<ParqueosBaseController> logger)
    : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || id <= 0)
            return BadRequest();

        var modelo = new ParqueosBaseViewModel();
        await CargarDatosAsync(modelo, cancellationToken);
        if (id.HasValue && !modelo.ErrorCarga)
        {
            var bus = modelo.Buses.SingleOrDefault(x => x.CodBus == id.Value);
            if (bus is null)
                return NotFound();
            modelo.Formulario = new ParqueoBaseFormulario { CodBus = bus.CodBus, CodParqueoBase = bus.CodParqueoBase };
        }
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(ParqueosBaseViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var resultado = await servicio.AsignarAsync(modelo.Formulario.CodBus!.Value,
                    modelo.Formulario.CodParqueoBase!.Value, cancellationToken);
                switch (resultado)
                {
                    case ResultadoAsignarParqueoBase.Guardado:
                        TempData["ParqueoBaseGuardado"] = "Parqueo base asignado correctamente.";
                        return RedirectToAction(nameof(Index));
                    case ResultadoAsignarParqueoBase.BusNoEncontrado:
                        ModelState.AddModelError("Formulario.CodBus", "El bus seleccionado ya no existe.");
                        break;
                    case ResultadoAsignarParqueoBase.ParqueoNoEncontrado:
                        ModelState.AddModelError("Formulario.CodParqueoBase", "El parqueo seleccionado ya no existe.");
                        break;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("El bus {Bus} ya no estaba disponible al asignar su parqueo base.", modelo.Formulario.CodBus);
                ModelState.AddModelError(string.Empty, "El bus ya no está disponible. Recarga la pantalla.");
            }
            catch (DbUpdateException exception)
            {
                InformarError(exception);
            }
            catch (SqlException exception)
            {
                InformarError(exception);
            }
            catch (InvalidOperationException exception) when (exception.InnerException is SqlException)
            {
                InformarError(exception);
            }
        }
        await CargarDatosAsync(modelo, cancellationToken);
        return View("Index", modelo);
    }

    private async Task CargarDatosAsync(ParqueosBaseViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.Buses = await servicio.ConsultarAsync(cancellationToken);
            modelo.Parqueos = await servicio.ObtenerParqueosAsync(cancellationToken);
        }
        catch (SqlException exception)
        {
            modelo.ErrorCarga = true;
            InformarError(exception);
        }
        catch (InvalidOperationException exception) when (exception.InnerException is SqlException)
        {
            modelo.ErrorCarga = true;
            InformarError(exception);
        }
    }

    private void InformarError(Exception exception)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error en parqueos base. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            exception.GetType().Name, sql?.Number, sql?.State);
        ModelState.AddModelError(string.Empty, "No se pudieron procesar los datos del parqueo base. Intenta nuevamente.");
    }
}
