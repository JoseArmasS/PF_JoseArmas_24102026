using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

[Authorize]
public sealed class CiclosBusesController(ICiclosBusesService ciclos, IOcupacionEstacionesService estaciones,
    IOcupacionBusesService ocupacion, IHoraConsultaService horaConsulta, IEsperaBusesService esperas,
    ILogger<CiclosBusesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int? estacion, int? linea, int? operacion, int? asignacion, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || estacion is <= 0 || linea is <= 0 || operacion is <= 0 || asignacion is <= 0) return BadRequest();
        var modelo = new CiclosBusesViewModel { Estacion = estacion, Linea = linea, Asignacion = asignacion };
        try
        {
            modelo.Estaciones = await estaciones.ConsultarEstacionesAsync(cancellationToken);
            modelo.EstacionSeleccionada = modelo.Estaciones.SingleOrDefault(x => x.Codigo == estacion);
            if (estacion.HasValue && modelo.EstacionSeleccionada is null) return NotFound();
            if (estacion.HasValue)
            {
                modelo.Lineas = await ciclos.ObtenerLineasAsync(estacion.Value, cancellationToken);
                if (linea.HasValue && !modelo.Lineas.Any(x => x.Codigo == linea))
                {
                    modelo.Linea = null;
                    ModelState.AddModelError(string.Empty, "Selecciona una línea que pase por esta estación.");
                }
                modelo.UltimaEstacion = (await estaciones.ConsultarHistorialAsync(estacion.Value, 1, cancellationToken)).FirstOrDefault();
                modelo.Ciclos = await ciclos.ConsultarActivosAsync(estacion.Value, cancellationToken);
            }
            if (modelo.Estacion.HasValue && modelo.Linea.HasValue)
            {
                modelo.Asignaciones = await ciclos.ObtenerAsignacionesAsync(modelo.Estacion.Value, modelo.Linea.Value, cancellationToken);
                modelo.Recorrido = await ciclos.ObtenerRecorridoAsync(modelo.Linea.Value, cancellationToken);
            }
            if (operacion.HasValue)
            {
                modelo.Seleccionado = await ciclos.ObtenerAsync(operacion.Value, cancellationToken);
                if (modelo.Seleccionado is null) return NotFound();
                modelo.Mediciones = (await ocupacion.ConsultarAsync(operacion.Value, 1, cancellationToken)).Take(20).ToList();
            }
            modelo.HoraReferencia = await horaConsulta.ObtenerAsync(cancellationToken);
            var consultas = modelo.Ciclos.ToList();
            if (modelo.Seleccionado is { } seleccionado && !consultas.Any(x => x.Operacion.CodOperacionBus == seleccionado.Operacion.CodOperacionBus))
                consultas.Add(seleccionado);
            modelo.Esperas = await esperas.ConsultarAsync(consultas, modelo.HoraReferencia.Value, cancellationToken);
        }
        catch (Exception exception) when (EsErrorSql(exception))
        {
            modelo.ErrorCarga = true;
            ModelState.AddModelError(string.Empty, InformarError(exception));
        }
        return View(modelo);
    }

    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Esperas(int estacion, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || estacion <= 0) return BadRequest();
        try
        {
            var activos = await ciclos.ConsultarActivosAsync(estacion, cancellationToken);
            var ahora = await horaConsulta.ObtenerAsync(cancellationToken);
            return Json(await esperas.ConsultarAsync(activos, ahora, cancellationToken));
        }
        catch (Exception exception) when (EsErrorSql(exception))
        { return StatusCode(503, new { mensaje = InformarError(exception) }); }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Iniciar(InicioCicloFormulario formulario, CancellationToken cancellationToken)
    {
        if (!TryOperador(out var operador)) return Challenge();
        if (!ModelState.IsValid) return EntradaInvalida(formulario.Estacion, formulario.Linea);
        return await EjecutarAsync(() => ciclos.IniciarAsync(formulario.Estacion!.Value, formulario.Linea!.Value,
            formulario.Asignacion!.Value, formulario.Sentido!.Value, operador, cancellationToken), formulario.Estacion, formulario.Linea);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirmar(MovimientoCicloFormulario formulario, AccionCiclo? accion, CancellationToken cancellationToken)
    {
        if (!TryOperador(out var operador)) return Challenge();
        if (!ModelState.IsValid || !accion.HasValue || !Enum.IsDefined(accion.Value)) return BadRequest();
        return await EjecutarAsync(() => ciclos.ConfirmarAsync(formulario.Operacion, formulario.Visita, formulario.Estacion,
            accion.Value, operador, cancellationToken), formulario.Estacion, operacion: formulario.Operacion);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Medir(MedicionCicloFormulario formulario, CancellationToken cancellationToken)
    {
        if (!TryOperador(out var operador)) return Challenge();
        if (!ModelState.IsValid) return EntradaInvalida(formulario.Estacion, operacion: formulario.Operacion);
        return await EjecutarAsync(() => ciclos.MedirAsync(formulario.Operacion, formulario.Visita, formulario.Estacion,
            formulario.Pasajeros!.Value, operador, cancellationToken), formulario.Estacion, operacion: formulario.Operacion);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MedirEstacion(MedicionEstacionFormulario formulario, CancellationToken cancellationToken)
    {
        if (!TryOperador(out var operador)) return Challenge();
        if (!ModelState.IsValid) return EntradaInvalida(formulario.CodEstacion);
        return await EjecutarAsync(async () =>
        {
            var resultado = await estaciones.RegistrarAsync(formulario.CodEstacion!.Value, formulario.Pasajeros!.Value, operador, cancellationToken);
            return new ResultadoCiclo(resultado == ResultadoRegistrarOcupacion.Guardado, resultado switch
            {
                ResultadoRegistrarOcupacion.Guardado => "Ocupación de la estación registrada.",
                ResultadoRegistrarOcupacion.SinCapacidad => "Configura la capacidad de la estación antes de registrar pasajeros.",
                ResultadoRegistrarOcupacion.EstacionNoEncontrada => "La estación ya no existe.",
                _ => "Tu cuenta no permite registrar la medición."
            });
        }, formulario.CodEstacion);
    }

    private bool TryOperador(out int operador) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out operador);

    private IActionResult EntradaInvalida(int? estacion, int? linea = null, int? operacion = null)
    {
        TempData["CicloError"] = "Revisa los campos obligatorios y los valores ingresados.";
        return RedirectToAction(nameof(Index), new { estacion, linea, operacion });
    }

    private async Task<IActionResult> EjecutarAsync(Func<Task<ResultadoCiclo>> accion, int? estacion, int? linea = null, int? operacion = null)
    {
        try
        {
            var resultado = await accion();
            TempData[resultado.Guardado ? "CicloMensaje" : "CicloError"] = resultado.Mensaje;
            if (resultado.Guardado) operacion = resultado.Operacion ?? operacion;
        }
        catch (Exception exception) when (EsErrorSql(exception)) { TempData["CicloError"] = InformarError(exception); }
        return RedirectToAction(nameof(Index), new { estacion, linea, operacion });
    }

    private static bool EsErrorSql(Exception exception) => exception is SqlException or DbUpdateException
        or InvalidOperationException { InnerException: SqlException };

    private string InformarError(Exception exception)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error de ciclo. Tipo {Tipo}, SQL {Numero}, estado {Estado}.", exception.GetType().Name, sql?.Number, sql?.State);
        return sql?.Number is 207 or 208
            ? "La estructura de operación no está actualizada. Solicita al administrador aplicar el script de ciclos y sus prerrequisitos."
            : "No se pudo confirmar la operación. Recarga y revisa el historial antes de intentarlo nuevamente.";
    }
}
