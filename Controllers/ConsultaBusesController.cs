using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class ConsultaBusesController(IConsultaBusesService servicio, IHoraConsultaService reloj,
    ILogger<ConsultaBusesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(ConsultaBusesViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            modelo.Catalogos = await servicio.ObtenerCatalogosAsync(cancellationToken);
            ValidarOpcion(modelo.Municipalidad, modelo.Catalogos.Municipalidades, nameof(modelo.Municipalidad), "La municipalidad seleccionada no existe.");
            ValidarOpcion(modelo.Estacion, modelo.Catalogos.Estaciones, nameof(modelo.Estacion), "La estación seleccionada no existe.");
            ValidarOpcion(modelo.Linea, modelo.Catalogos.Lineas, nameof(modelo.Linea), "La línea seleccionada no existe.");
            ValidarOpcion(modelo.Bus, modelo.Catalogos.Buses, nameof(modelo.Bus), "El bus seleccionado no existe.");
            ValidarOpcion(modelo.Piloto, modelo.Catalogos.Pilotos, nameof(modelo.Piloto), "El piloto seleccionado no existe.");
            if (modelo.Estado is not null && !EstadosConsultaBuses.Valores.Contains(modelo.Estado))
                ModelState.AddModelError(nameof(modelo.Estado), "Selecciona un estado de operación válido.");
            if (ModelState.IsValid)
            {
                modelo.Resultado = await servicio.ConsultarAsync(new(modelo.Estacion, modelo.Linea, modelo.Bus, modelo.Piloto, modelo.Estado, modelo.Municipalidad), modelo.Pagina, cancellationToken);
                modelo.Pagina = modelo.Resultado.Pagina;
                modelo.HoraReferencia = await reloj.ObtenerAsync(cancellationToken);
            }
        }
        catch (Exception exception) when (exception is SqlException or DbUpdateException
            || exception is InvalidOperationException { InnerException: SqlException })
        {
            var sql = exception as SqlException ?? exception.InnerException as SqlException;
            logger.LogError("Error al consultar buses en operación. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
                exception.GetType().Name, sql?.Number, sql?.State);
            modelo.ErrorCarga = true;
            ModelState.AddModelError(string.Empty, "No se pudieron consultar los buses en operación. Intenta nuevamente.");
        }
        return View(modelo);
    }

    private void ValidarOpcion(int? codigo, IReadOnlyList<OpcionCatalogo> opciones, string campo, string mensaje)
    {
        if (codigo.HasValue && !opciones.Any(x => x.Codigo == codigo)) ModelState.AddModelError(campo, mensaje);
    }
}
