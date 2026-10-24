using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class BusesController(IBusesService servicio, ILogger<BusesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || id <= 0)
            return BadRequest();

        var modelo = new BusesViewModel();
        try
        {
            if (id.HasValue)
            {
                var bus = await servicio.ObtenerAsync(id.Value, cancellationToken);
                if (bus is null)
                    return NotFound();

                modelo.Formulario = new BusFormulario
                {
                    CodBus = bus.CodBus,
                    CodMunicipalidad = bus.CodMunicipalidad,
                    NumeroBus = bus.NumeroBus,
                    NumeroPlaca = bus.NumeroPlaca,
                    CapacidadPasajeros = bus.CapacidadPasajeros,
                    ModeloBus = bus.ModeloBus,
                    AnioBus = bus.AnioBus,
                    Estado = bus.Estado
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
    public async Task<IActionResult> Guardar(BusesViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var formulario = modelo.Formulario;
                var resultado = await servicio.GuardarAsync(new Bus
                {
                    CodBus = formulario.CodBus,
                    CodMunicipalidad = formulario.CodMunicipalidad!.Value,
                    NumeroBus = formulario.NumeroBus,
                    NumeroPlaca = formulario.NumeroPlaca,
                    CapacidadPasajeros = formulario.CapacidadPasajeros!.Value,
                    ModeloBus = formulario.ModeloBus,
                    AnioBus = formulario.AnioBus,
                    Estado = formulario.Estado!.Value
                }, cancellationToken);

                switch (resultado)
                {
                    case ResultadoGuardarBus.Guardado:
                        TempData["BusGuardado"] = formulario.CodBus == 0
                            ? "Bus registrado correctamente."
                            : "Bus actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    case ResultadoGuardarBus.PlacaDuplicada:
                        ModelState.AddModelError("Formulario.NumeroPlaca", "Ya existe un bus con esa placa.");
                        break;
                    case ResultadoGuardarBus.MunicipalidadNoEncontrada:
                        ModelState.AddModelError("Formulario.CodMunicipalidad", "La municipalidad seleccionada no existe.");
                        break;
                    case ResultadoGuardarBus.NoEncontrado:
                        return NotFound();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("El bus {Codigo} ya no estaba disponible al guardar.", modelo.Formulario.CodBus);
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

    private async Task CargarDatosAsync(BusesViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.Municipalidades = await servicio.ObtenerMunicipalidadesAsync(cancellationToken);
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

    private void InformarError(Exception exception, string operacion, BusesViewModel modelo)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al {Operacion} buses. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            operacion, exception.GetType().Name, sql?.Number, sql?.State);

        if (operacion == "consultar")
            modelo.ErrorCarga = true;

        ModelState.AddModelError(string.Empty, $"No se pudieron {operacion} los datos. Intenta nuevamente.");
    }
}
