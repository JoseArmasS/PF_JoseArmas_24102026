using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class MunicipalidadesController(IMunicipalidadesService servicio, ILogger<MunicipalidadesController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? buscar, int pagina = 1, int? id = null, CancellationToken cancellationToken = default)
    {
        var modelo = new MunicipalidadesViewModel { Buscar = buscar, Pagina = pagina };
        if (buscar?.Length > 150) return BadRequest();
        try
        {
            if (id.HasValue)
            {
                var registro = await servicio.ObtenerAsync(id.Value, cancellationToken);
                if (registro is null) return NotFound();
                modelo.Formulario = new MunicipalidadFormulario
                {
                    CodMunicipalidad = registro.CodMunicipalidad,
                    NombreMunicipalidad = registro.NombreMunicipalidad,
                    DireccionMunicipalidad = registro.DireccionMunicipalidad,
                    TelefonoMunicipalidad = registro.TelefonoMunicipalidad,
                    CorreoElectronico = registro.CorreoElectronico,
                    Estado = registro.Estado
                };
            }
        }
        catch (SqlException exception) { InformarError(exception, "consultar", modelo); }
        await CargarTablaAsync(modelo, cancellationToken);
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(MunicipalidadesViewModel modelo, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var formulario = modelo.Formulario;
                var guardado = await servicio.GuardarAsync(new Municipalidad
                {
                    CodMunicipalidad = formulario.CodMunicipalidad,
                    NombreMunicipalidad = formulario.NombreMunicipalidad,
                    DireccionMunicipalidad = formulario.DireccionMunicipalidad,
                    TelefonoMunicipalidad = formulario.TelefonoMunicipalidad,
                    CorreoElectronico = formulario.CorreoElectronico,
                    Estado = formulario.Estado!.Value
                }, cancellationToken);
                if (!guardado) return NotFound();
                TempData["MunicipalidadGuardada"] = formulario.CodMunicipalidad == 0
                    ? "Municipalidad registrada correctamente." : "Municipalidad actualizada correctamente.";
                return RedirectToAction(nameof(Index), new { buscar = modelo.Buscar, pagina = modelo.Pagina });
            }
            catch (DbUpdateConcurrencyException)
            {
                logger.LogWarning("La municipalidad {Codigo} ya no estaba disponible al guardar.", modelo.Formulario.CodMunicipalidad);
                ModelState.AddModelError(string.Empty, "El registro ya no está disponible. Recarga la pantalla.");
            }
            catch (DbUpdateException exception) { InformarError(exception, "guardar", modelo); }
            catch (SqlException exception) { InformarError(exception, "guardar", modelo); }
        }
        await CargarTablaAsync(modelo, cancellationToken);
        return View("Index", modelo);
    }

    private async Task CargarTablaAsync(MunicipalidadesViewModel modelo, CancellationToken cancellationToken)
    {
        try
        {
            var resultado = await servicio.ConsultarAsync(modelo.Buscar, modelo.Pagina, cancellationToken);
            modelo.Registros = resultado.Registros;
            modelo.Total = resultado.Total;
            modelo.Pagina = resultado.Pagina;
        }
        catch (SqlException exception) { InformarError(exception, "consultar", modelo); }
    }

    private void InformarError(Exception exception, string operacion, MunicipalidadesViewModel modelo)
    {
        var sql = exception as SqlException ?? exception.InnerException as SqlException;
        logger.LogError("Error al {Operacion} municipalidades. Tipo {Tipo}, SQL {Numero}, estado {Estado}.",
            operacion, exception.GetType().Name, sql?.Number, sql?.State);
        if (operacion == "consultar") modelo.ErrorCarga = true;
        ModelState.AddModelError(string.Empty, $"No se pudieron {operacion} los datos. Intenta nuevamente o contacta al administrador.");
    }
}
