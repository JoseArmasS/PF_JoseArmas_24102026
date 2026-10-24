using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class EstacionesController(IEstacionesService servicio, ILogger<EstacionesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || id <= 0)
            return BadRequest();

        var modelo = new EstacionesViewModel();
        try
        {
            if (id.HasValue)
            {
                var estacion = await servicio.ObtenerAsync(id.Value, cancellationToken);
                if (estacion is null)
                    return NotFound();

                modelo.Formulario = new EstacionFormulario
                {
                    CodEstacion = estacion.CodEstacion,
                    CodMunicipalidad = estacion.CodMunicipalidad,
                    NombreEstacion = estacion.NombreEstacion,
                    DireccionEstacion = estacion.DireccionEstacion,
                    Latitud = estacion.Latitud,
                    Longitud = estacion.Longitud,
                    CapacidadPasajeros = estacion.CapacidadPasajeros,
                    Estado = estacion.Estado
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
    public async Task<IActionResult> Guardar(EstacionesViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var formulario = modelo.Formulario;
                var resultado = await servicio.GuardarAsync(new Estacion
                {
                    CodEstacion = formulario.CodEstacion,
                    CodMunicipalidad = formulario.CodMunicipalidad!.Value,
                    NombreEstacion = formulario.NombreEstacion,
                    DireccionEstacion = formulario.DireccionEstacion,
                    Latitud = formulario.Latitud,
                    Longitud = formulario.Longitud,
                    CapacidadPasajeros = formulario.CapacidadPasajeros,
                    Estado = formulario.Estado!.Value
                }, cancellationToken);

                switch (resultado)
                {
                    case ResultadoGuardarEstacion.Guardado:
                        TempData["EstacionGuardada"] = formulario.CodEstacion == 0
                            ? "Estación registrada correctamente."
                            : "Estación actualizada correctamente.";
                        return RedirectToAction(nameof(Index));
                    case ResultadoGuardarEstacion.MunicipalidadNoEncontrada:
                        ModelState.AddModelError("Formulario.CodMunicipalidad", "La municipalidad seleccionada no existe.");
                        break;
                    case ResultadoGuardarEstacion.NoEncontrado:
                        return NotFound();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("La estación {Codigo} ya no estaba disponible al guardar.", modelo.Formulario.CodEstacion);
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

    private async Task CargarDatosAsync(EstacionesViewModel modelo, CancellationToken cancellationToken)
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

    private void InformarError(Exception exception, string operacion, EstacionesViewModel modelo)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al {Operacion} estaciones. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            operacion, exception.GetType().Name, sql?.Number, sql?.State);

        if (operacion == "consultar")
            modelo.ErrorCarga = true;

        ModelState.AddModelError(string.Empty, $"No se pudieron {operacion} los datos. Intenta nuevamente.");
    }
}

