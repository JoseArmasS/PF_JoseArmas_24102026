using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TransMetroUMG.Application;
using TransMetroUMG.Models;

namespace TransMetroUMG.Controllers;

public sealed class CatalogosController(
    ICatalogosUsuariosService catalogosService,
    ILogger<CatalogosController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        try
        {
            var catalogos = await catalogosService.ObtenerAsync(cancellationToken);
            return View(new CatalogosViewModel
            {
                Municipalidades = catalogos.Municipalidades,
                Roles = catalogos.Roles
            });
        }
        catch (SqlException exception)
        {
            logger.LogError("No se pudieron consultar los catálogos. SQL {Numero}, estado {Estado}.",
                exception.Number, exception.State);
            return View(new CatalogosViewModel { ErrorCarga = true });
        }
    }
}
