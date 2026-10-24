using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class PilotosController(IPilotosService servicio, ILogger<PilotosController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || id <= 0)
            return BadRequest();

        var modelo = new PilotosViewModel();
        try
        {
            if (id.HasValue)
            {
                var piloto = await servicio.ObtenerAsync(id.Value, cancellationToken);
                if (piloto is null)
                    return NotFound();

                modelo.Formulario = new PilotoFormulario
                {
                    CodPiloto = piloto.CodPiloto,
                    CodMunicipalidad = piloto.CodMunicipalidad,
                    NombresPiloto = piloto.NombresPiloto,
                    NumeroLicencia = piloto.NumeroLicencia,
                    ApellidosPiloto = piloto.ApellidosPiloto,
                    TelefonoPiloto = piloto.TelefonoPiloto,
                    HistorialEducativo = piloto.HistorialEducativo,
                    TipoLicencia = piloto.TipoLicencia,
                    FechaVencimientoLicencia = piloto.FechaVencimientoLicencia,
                    Estado = piloto.Estado
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
    public async Task<IActionResult> Guardar(PilotosViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var formulario = modelo.Formulario;
                var resultado = await servicio.GuardarAsync(new Piloto
                {
                    CodPiloto = formulario.CodPiloto,
                    CodMunicipalidad = formulario.CodMunicipalidad!.Value,
                    NombresPiloto = formulario.NombresPiloto,
                    NumeroLicencia = formulario.NumeroLicencia,
                    ApellidosPiloto = formulario.ApellidosPiloto,
                    TelefonoPiloto = formulario.TelefonoPiloto,
                    HistorialEducativo = formulario.HistorialEducativo,
                    TipoLicencia = formulario.TipoLicencia,
                    FechaVencimientoLicencia = formulario.FechaVencimientoLicencia!.Value,
                    Estado = formulario.Estado!.Value
                }, cancellationToken);

                switch (resultado)
                {
                    case ResultadoGuardarPiloto.Guardado:
                        TempData["PilotoGuardado"] = formulario.CodPiloto == 0
                            ? "Piloto registrado correctamente."
                            : "Piloto actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    case ResultadoGuardarPiloto.LicenciaDuplicada:
                        ModelState.AddModelError("Formulario.NumeroLicencia", "Ya existe un piloto con esa licencia.");
                        break;
                    case ResultadoGuardarPiloto.MunicipalidadNoEncontrada:
                        ModelState.AddModelError("Formulario.CodMunicipalidad", "La municipalidad seleccionada no existe.");
                        break;
                    case ResultadoGuardarPiloto.NoEncontrado:
                        return NotFound();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("El piloto {Codigo} ya no estaba disponible al guardar.", modelo.Formulario.CodPiloto);
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

    private async Task CargarDatosAsync(PilotosViewModel modelo, CancellationToken cancellationToken)
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

    private void InformarError(Exception exception, string operacion, PilotosViewModel modelo)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al {Operacion} pilotos. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            operacion, exception.GetType().Name, sql?.Number, sql?.State);

        if (operacion == "consultar")
            modelo.ErrorCarga = true;

        ModelState.AddModelError(string.Empty, $"No se pudieron {operacion} los datos. Intenta nuevamente.");
    }
}
