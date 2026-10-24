using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class MunicipalidadesService(MunitransDbContext dbContext, ILogger<MunicipalidadesService> logger)
    : IMunicipalidadesService
{
    public async Task<PaginaMunicipalidades> ConsultarAsync(string? buscar, int pagina, CancellationToken cancellationToken)
    {
        var consulta = dbContext.Municipalidades.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var termino = buscar.Trim();
            consulta = consulta.Where(x => x.NombreMunicipalidad.Contains(termino));
        }
        var total = await consulta.CountAsync(cancellationToken);
        pagina = Math.Clamp(pagina, 1, Math.Max(1, (int)Math.Ceiling(total / (double)IMunicipalidadesService.TamanoPagina)));
        var registros = await consulta.OrderBy(x => x.NombreMunicipalidad).ThenBy(x => x.CodMunicipalidad)
            .Skip((pagina - 1) * IMunicipalidadesService.TamanoPagina).Take(IMunicipalidadesService.TamanoPagina)
            .ToListAsync(cancellationToken);
        return new PaginaMunicipalidades(registros, total, pagina);
    }

    public Task<Municipalidad?> ObtenerAsync(int codigo, CancellationToken cancellationToken) =>
        dbContext.Municipalidades.AsNoTracking().SingleOrDefaultAsync(x => x.CodMunicipalidad == codigo, cancellationToken);

    public async Task<bool> GuardarAsync(Municipalidad datos, CancellationToken cancellationToken)
    {
        var registro = datos.CodMunicipalidad == 0 ? new Municipalidad()
            : await dbContext.Municipalidades.SingleOrDefaultAsync(x => x.CodMunicipalidad == datos.CodMunicipalidad, cancellationToken);
        if (registro is null) return false;

        registro.NombreMunicipalidad = datos.NombreMunicipalidad.Trim();
        registro.DireccionMunicipalidad = NormalizarOpcional(datos.DireccionMunicipalidad);
        registro.TelefonoMunicipalidad = NormalizarOpcional(datos.TelefonoMunicipalidad);
        registro.CorreoElectronico = NormalizarOpcional(datos.CorreoElectronico);
        registro.Estado = datos.Estado;
        if (datos.CodMunicipalidad == 0) dbContext.Municipalidades.Add(registro);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Municipalidad {Codigo}: {Operacion} realizada.", registro.CodMunicipalidad,
            datos.CodMunicipalidad == 0 ? "creación" : "actualización");
        return true;
    }

    private static string? NormalizarOpcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
