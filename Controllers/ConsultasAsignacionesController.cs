using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class ConsultasAsignacionesController(IConsultasAsignacionesService servicio,
    IConsultaBusesService catalogos, IParqueosBaseService parqueos, ILogger<ConsultasAsignacionesController> logger) : Controller
{
    [HttpGet]
    public Task<IActionResult> Pilotos(ConsultasAsignacionesViewModel modelo, CancellationToken ct)
        => ConsultarAsync(modelo, false, ct);

    [HttpGet]
    public Task<IActionResult> Parqueos(ConsultasAsignacionesViewModel modelo, CancellationToken ct)
        => ConsultarAsync(modelo, true, ct);

    private async Task<IActionResult> ConsultarAsync(ConsultasAsignacionesViewModel modelo, bool consultaParqueos, CancellationToken ct)
    {
        modelo.ConsultaParqueos = consultaParqueos;
        try
        {
            modelo.Catalogos = await catalogos.ObtenerCatalogosAsync(ct);
            Validar(modelo.Municipalidad, modelo.Catalogos.Municipalidades, nameof(modelo.Municipalidad), "La municipalidad seleccionada no existe.");
            Validar(modelo.Bus, modelo.Catalogos.Buses, nameof(modelo.Bus), "El bus seleccionado no existe.");
            if (consultaParqueos)
            {
                modelo.Parqueos = await parqueos.ObtenerParqueosAsync(ct);
                Validar(modelo.Estacion, modelo.Catalogos.Estaciones, nameof(modelo.Estacion), "La estación seleccionada no existe.");
                Validar(modelo.Parqueo, modelo.Parqueos, nameof(modelo.Parqueo), "El parqueo seleccionado no existe.");
                if (ModelState.IsValid)
                    modelo.EstacionesParqueos = await servicio.ConsultarParqueosAsync(new(modelo.Municipalidad, modelo.Estacion, modelo.Parqueo, modelo.Bus), modelo.Pagina, ct);
            }
            else
            {
                Validar(modelo.Piloto, modelo.Catalogos.Pilotos, nameof(modelo.Piloto), "El piloto seleccionado no existe.");
                if (ModelState.IsValid)
                    modelo.PilotosBuses = await servicio.ConsultarPilotosAsync(new(modelo.Municipalidad, modelo.Piloto, modelo.Bus, modelo.Fecha), modelo.Pagina, ct);
            }
        }
        catch (Exception exception) when (exception is SqlException or DbUpdateException
            || exception is InvalidOperationException { InnerException: SqlException })
        {
            var sql = exception as SqlException ?? exception.InnerException as SqlException;
            logger.LogError("Error al consultar asignaciones. Tipo {Tipo}, SQL {Numero}, estado {Estado}.", exception.GetType().Name, sql?.Number, sql?.State);
            modelo.ErrorCarga = true;
            ModelState.AddModelError(string.Empty, "No se pudo cargar la consulta. Intenta nuevamente.");
        }
        return View("Index", modelo);
    }

    private void Validar(int? codigo, IReadOnlyList<OpcionCatalogo> opciones, string campo, string mensaje)
    {
        if (codigo.HasValue && !opciones.Any(x => x.Codigo == codigo)) ModelState.AddModelError(campo, mensaje);
    }
}
