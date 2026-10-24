using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class LineasController(ILineasService servicio, ILogger<LineasController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || id <= 0)
            return BadRequest();

        var modelo = new LineasViewModel();
        try
        {
            if (id.HasValue)
            {
                var linea = await servicio.ObtenerAsync(id.Value, cancellationToken);
                if (linea is null)
                    return NotFound();

                modelo.Formulario = new LineaFormulario
                {
                    CodLinea = linea.CodLinea,
                    CodMunicipalidad = linea.CodMunicipalidad,
                    NombreLinea = linea.NombreLinea,
                    DescripcionLinea = linea.DescripcionLinea,
                    Estado = linea.Estado
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
    public async Task<IActionResult> Guardar(LineasViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var formulario = modelo.Formulario;
                var resultado = await servicio.GuardarAsync(new Linea
                {
                    CodLinea = formulario.CodLinea,
                    CodMunicipalidad = formulario.CodMunicipalidad!.Value,
                    NombreLinea = formulario.NombreLinea,
                    DescripcionLinea = formulario.DescripcionLinea,
                    Estado = formulario.Estado!.Value
                }, cancellationToken);

                switch (resultado)
                {
                    case ResultadoGuardarLinea.Guardado:
                        TempData["LineaGuardada"] = formulario.CodLinea == 0
                            ? "Línea registrada correctamente."
                            : "Línea actualizada correctamente.";
                        return RedirectToAction(nameof(Index));
                    case ResultadoGuardarLinea.MunicipalidadNoEncontrada:
                        ModelState.AddModelError("Formulario.CodMunicipalidad", "La municipalidad seleccionada no existe.");
                        break;
                    case ResultadoGuardarLinea.NoEncontrado:
                        return NotFound();
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("La línea {Codigo} ya no estaba disponible al guardar.", modelo.Formulario.CodLinea);
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

    private async Task CargarDatosAsync(LineasViewModel modelo, CancellationToken cancellationToken)
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

    private void InformarError(Exception exception, string operacion, LineasViewModel modelo)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al {Operacion} líneas. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            operacion, exception.GetType().Name, sql?.Number, sql?.State);

        if (operacion == "consultar")
            modelo.ErrorCarga = true;

        ModelState.AddModelError(string.Empty, $"No se pudieron {operacion} los datos. Intenta nuevamente.");
    }
}
