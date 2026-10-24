using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TransMetroUMG.Application;
using TransMetroUMG.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace TransMetroUMG.Controllers;

public sealed class OperacionBusesController(
    IOperacionBusesService servicio, IOcupacionEstacionesService ocupacion,
    IRegistroOperacionBusService operaciones,
    IHoraConsultaService horaConsulta,
    ILogger<OperacionBusesController> logger) : Controller
{
    // Maqueta visual aislada: no consulta ni modifica datos operativos.
    [HttpGet]
    public IActionResult VistaPrevia() => View();

    [HttpGet]
    public async Task<IActionResult> Index(OperacionBusesViewModel modelo,
        CancellationToken cancellationToken, int? estacion = null, int pagina = 1)
    {
        if (estacion is <= 0 || pagina is < 1 or > 1000000) return BadRequest();
        await CargarOcupacionAsync(modelo, estacion, pagina, cancellationToken);
        await CargarOperacionesAsync(modelo, cancellationToken);
        return View(modelo);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarOcupacion(
        [Bind(Prefix = "Ocupacion.Formulario")] MedicionEstacionFormulario formulario,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var operador))
            return Challenge();
        if (ModelState.IsValid)
        {
            try
            {
                var resultado = await ocupacion.RegistrarAsync(formulario.CodEstacion!.Value,
                    formulario.Pasajeros!.Value, operador, cancellationToken);
                if (resultado == ResultadoRegistrarOcupacion.Guardado)
                {
                    TempData["OcupacionGuardada"] = "Medición de ocupación registrada correctamente.";
                    return RedirectToAction(nameof(Index), new { estacion = formulario.CodEstacion });
                }
                ModelState.AddModelError(string.Empty, resultado switch
                {
                    ResultadoRegistrarOcupacion.SinCapacidad => "Configura la capacidad de la estación antes de registrar pasajeros.",
                    ResultadoRegistrarOcupacion.EstacionNoEncontrada => "La estación seleccionada ya no existe.",
                    _ => "Tu cuenta no permite registrar la medición. Inicia sesión nuevamente."
                });
            }
            catch (Exception exception) when (EsErrorSql(exception))
            {
                RegistrarErrorOcupacion(exception);
                ModelState.AddModelError(string.Empty, "No se pudo guardar la medición. Intenta nuevamente.");
            }
        }
        var modelo = new OperacionBusesViewModel();
        await CargarLineasAsync(modelo, cancellationToken);
        await CargarOcupacionAsync(modelo, formulario.CodEstacion, 1, cancellationToken);
        await CargarOperacionesAsync(modelo, cancellationToken);
        modelo.Ocupacion.Formulario = formulario;
        try { modelo.Registros = await servicio.ConsultarAsync(null, cancellationToken); }
        catch (Exception exception) when (EsErrorSql(exception)) { InformarError(exception, modelo); }
        return View("Index", modelo);
    }

    private async Task CargarOcupacionAsync(OperacionBusesViewModel modelo, int? estacion,
        int pagina, CancellationToken cancellationToken)
    {
        var datos = modelo.Ocupacion;
        datos.CodEstacion = estacion;
        datos.Pagina = pagina;
        datos.Formulario.CodEstacion = estacion;
        try
        {
            datos.Estaciones = await ocupacion.ConsultarEstacionesAsync(cancellationToken);
            datos.Seleccionada = datos.Estaciones.SingleOrDefault(x => x.Codigo == estacion);
            if (estacion.HasValue && datos.Seleccionada is null)
                ModelState.AddModelError(string.Empty, "La estación seleccionada no existe.");
            if (datos.Seleccionada is not null)
            {
                var registros = await ocupacion.ConsultarHistorialAsync(estacion!.Value, pagina, cancellationToken);
                datos.HaySiguiente = registros.Count > 20;
                datos.Historial = registros.Take(20).ToList();
                datos.Ultima = pagina == 1 ? datos.Historial.FirstOrDefault()
                    : (await ocupacion.ConsultarHistorialAsync(estacion.Value, 1, cancellationToken)).FirstOrDefault();
            }
        }
        catch (Exception exception) when (EsErrorSql(exception))
        {
            RegistrarErrorOcupacion(exception);
            datos.ErrorCarga = true;
        }
    }

    private static bool EsErrorSql(Exception exception) => exception is SqlException
        or Microsoft.EntityFrameworkCore.DbUpdateException
        or InvalidOperationException { InnerException: SqlException };

    private async Task CargarOperacionesAsync(OperacionBusesViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.Operaciones = modelo.Ocupacion.Seleccionada is { } estacion
                ? await operaciones.ConsultarEnEstacionAsync(estacion.Codigo, cancellationToken) : [];
            modelo.HoraReferencia = await horaConsulta.ObtenerAsync(cancellationToken);
        }
        catch (Exception exception) when (EsErrorSql(exception))
        {
            RegistrarErrorOcupacion(exception);
            modelo.ErrorOperaciones = true;
        }
    }

    private async Task CargarLineasAsync(OperacionBusesViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.Lineas = await servicio.ObtenerLineasAsync(cancellationToken);
            if (modelo.CodigoLinea.HasValue && !modelo.Lineas.Any(x => x.Codigo == modelo.CodigoLinea))
                ModelState.AddModelError(nameof(modelo.CodigoLinea), "La línea seleccionada no existe.");
        }
        catch (Exception exception) when (EsErrorSql(exception)) { InformarError(exception, modelo); }
    }

    private void RegistrarErrorOcupacion(Exception exception)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error en ocupación de estaciones. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            exception.GetType().Name, sql?.Number, sql?.State);
    }

    private void InformarError(Exception exception, OperacionBusesViewModel modelo)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al consultar asignaciones de buses. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            exception.GetType().Name, sql?.Number, sql?.State);
        modelo.ErrorCarga = true;
    }
}
