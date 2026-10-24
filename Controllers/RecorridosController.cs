using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class RecorridosController(IRecorridosService servicio, ILogger<RecorridosController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? id = null, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid || id <= 0)
            return BadRequest();

        var modelo = new RecorridosViewModel();
        try
        {
            if (id.HasValue)
            {
                var recorrido = await servicio.ObtenerAsync(id.Value, cancellationToken);
                if (recorrido is null)
                    return NotFound();

                modelo.Formulario = new RecorridoFormulario
                {
                    CodLineaEstacion = recorrido.CodLineaEstacion,
                    CodLinea = recorrido.CodLinea,
                    CodEstacion = recorrido.CodEstacion,
                    OrdenEstacion = recorrido.OrdenEstacion,
                    DistanciaSiguienteKm = recorrido.DistanciaSiguienteKm
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
    public async Task<IActionResult> Guardar(RecorridosViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var formulario = modelo.Formulario;
                var resultado = await servicio.GuardarAsync(new LineaEstacion
                {
                    CodLineaEstacion = formulario.CodLineaEstacion,
                    CodLinea = formulario.CodLinea!.Value,
                    CodEstacion = formulario.CodEstacion!.Value,
                    OrdenEstacion = formulario.OrdenEstacion!.Value,
                    DistanciaSiguienteKm = formulario.DistanciaSiguienteKm
                }, cancellationToken);

                switch (resultado)
                {
                    case ResultadoGuardarRecorrido.Guardado:
                        TempData["RecorridoGuardado"] = formulario.CodLineaEstacion == 0
                            ? "Estación agregada al recorrido correctamente."
                            : "Recorrido actualizado correctamente.";
                        return RedirectToAction(nameof(Index));
                    case ResultadoGuardarRecorrido.LineaNoEncontrada:
                        ModelState.AddModelError("Formulario.CodLinea", "La línea seleccionada no existe.");
                        break;
                    case ResultadoGuardarRecorrido.EstacionNoEncontrada:
                        ModelState.AddModelError("Formulario.CodEstacion", "La estación seleccionada no existe.");
                        break;
                    case ResultadoGuardarRecorrido.EstacionDuplicada:
                        ModelState.AddModelError("Formulario.CodEstacion", "Esta estación ya pertenece a la línea.");
                        break;
                    case ResultadoGuardarRecorrido.OrdenDuplicado:
                        ModelState.AddModelError("Formulario.OrdenEstacion", "Ese orden ya está ocupado en la línea.");
                        break;
                    case ResultadoGuardarRecorrido.NoEncontrado:
                        return NotFound();
                    case ResultadoGuardarRecorrido.DistanciaNoValida:
                        ModelState.AddModelError("Formulario.DistanciaSiguienteKm", "La distancia debe ser positiva, de hasta 99999.999 km y con un máximo de tres decimales.");
                        break;
                    case ResultadoGuardarRecorrido.SinEstacionSiguiente:
                        ModelState.AddModelError("Formulario.DistanciaSiguienteKm", "La última estación no tiene siguiente. Deja su distancia vacía.");
                        break;
                    case ResultadoGuardarRecorrido.TramosConDistanciaAfectados:
                        ModelState.AddModelError(string.Empty, "El cambio modifica un tramo que ya tiene distancia. Primero deja vacía la distancia de la estación anterior afectada; después modifica el recorrido y registra la distancia del nuevo tramo.");
                        break;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("El recorrido {Codigo} ya no estaba disponible al guardar.", modelo.Formulario.CodLineaEstacion);
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

    private async Task CargarDatosAsync(RecorridosViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.Lineas = await servicio.ObtenerLineasAsync(cancellationToken);
            modelo.Estaciones = await servicio.ObtenerEstacionesAsync(cancellationToken);
            modelo.Registros = await servicio.ConsultarAsync(cancellationToken);
            modelo.EstacionesSiguientes = modelo.Registros.GroupBy(x => x.CodLinea)
                .SelectMany(grupo =>
                {
                    var ordenados = grupo.OrderBy(x => x.OrdenEstacion).ToArray();
                    return ordenados.Zip(ordenados.Skip(1), (actual, siguiente) =>
                        new { actual.CodLineaEstacion, siguiente.CodEstacion });
                }).ToDictionary(x => x.CodLineaEstacion, x => x.CodEstacion);
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

    private void InformarError(Exception exception, string operacion, RecorridosViewModel modelo)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al {Operacion} recorridos. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            operacion, exception.GetType().Name, sql?.Number, sql?.State);

        if (operacion == "consultar")
            modelo.ErrorCarga = true;

        ModelState.AddModelError(string.Empty, $"No se pudieron {operacion} los datos. Intenta nuevamente.");
    }
}
