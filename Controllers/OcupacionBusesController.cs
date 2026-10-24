using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

[Authorize]
public sealed class OcupacionBusesController(IOcupacionBusesService servicio,
    IRegistroOperacionBusService operaciones, ILogger<OcupacionBusesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int operacion, int pagina = 1, CancellationToken cancellationToken = default, bool soloConsulta = false)
    {
        if (!ModelState.IsValid || operacion <= 0 || pagina is < 1 or > 1000000) return BadRequest();
        var modelo = new OcupacionBusViewModel { Operacion = operacion, Pagina = pagina, SoloConsulta = soloConsulta };
        await CargarAsync(modelo, cancellationToken);
        if (!modelo.ErrorCarga && modelo.Datos is null) return NotFound();
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Registrar(OcupacionBusViewModel modelo, CancellationToken cancellationToken, bool desdeOperacion = false)
    {
        if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var operador)) return Challenge();
        if (ModelState.IsValid)
        {
            try
            {
                var resultado = await servicio.RegistrarAsync(modelo.Operacion, modelo.Estacion!.Value,
                    modelo.Pasajeros!.Value, operador, cancellationToken);
                if (resultado == ResultadoMedicionBus.Guardado)
                {
                    if (desdeOperacion) return Json(new { guardado = true });
                    TempData["MedicionBusGuardada"] = "Ocupación del bus registrada correctamente.";
                    return RedirectToAction(nameof(Index), new { operacion = modelo.Operacion });
                }
                modelo.SolicitarMasBuses = resultado == ResultadoMedicionBus.CapacidadExcedida;
                ModelState.AddModelError(string.Empty, resultado switch
                {
                    ResultadoMedicionBus.OperacionNoValida => "La operación no existe o no tiene capacidad válida.",
                    ResultadoMedicionBus.EstacionFueraDeLinea => "La estación debe pertenecer a la línea de la operación.",
                    ResultadoMedicionBus.CapacidadExcedida => "La cantidad de pasajeros supera la capacidad registrada del bus.",
                    ResultadoMedicionBus.VisitaNoValida => "Registra esta medición desde Operación de buses, en la visita actual del ciclo.",
                    _ => "Tu cuenta no permite registrar la medición. Inicia sesión nuevamente."
                });
            }
            catch (Exception exception) when (EsErrorSql(exception))
            {
                RegistrarError(exception);
                ModelState.AddModelError(string.Empty, "No se pudo guardar la ocupación del bus. Intenta nuevamente.");
            }
        }
        if (desdeOperacion)
            return BadRequest(new { solicitarMasBuses = modelo.SolicitarMasBuses,
                errores = ModelState.Values.SelectMany(x => x.Errors).Select(x =>
                    string.IsNullOrEmpty(x.ErrorMessage) ? "Revisa los datos ingresados." : x.ErrorMessage).ToArray() });
        await CargarAsync(modelo, cancellationToken);
        return View("Index", modelo);
    }

    private async Task CargarAsync(OcupacionBusViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.Datos = await servicio.ObtenerOperacionAsync(modelo.Operacion, cancellationToken);
            if (modelo.Datos is null) return;
            modelo.Recorrido = await operaciones.ObtenerRecorridoAsync(modelo.Datos.Asignacion, cancellationToken);
            var registros = await servicio.ConsultarAsync(modelo.Operacion, modelo.Pagina, cancellationToken);
            modelo.Historial = registros.Take(20).ToList();
            modelo.HaySiguiente = registros.Count > 20;
            modelo.Ultima = modelo.Pagina == 1 ? registros.FirstOrDefault()
                : (await servicio.ConsultarAsync(modelo.Operacion, 1, cancellationToken)).FirstOrDefault();
        }
        catch (Exception exception) when (EsErrorSql(exception))
        {
            RegistrarError(exception);
            modelo.ErrorCarga = true;
        }
    }

    private static bool EsErrorSql(Exception exception) => exception is SqlException or DbUpdateException
        or InvalidOperationException { InnerException: SqlException };

    private void RegistrarError(Exception exception)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error en ocupación de buses. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            exception.GetType().Name, sql?.Number, sql?.State);
    }
}
