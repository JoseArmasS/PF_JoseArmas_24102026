using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;

namespace TransMetroUMG.Infrastructure;

public sealed class ParqueosBaseService(MunitransDbContext dbContext, ILogger<ParqueosBaseService> logger)
    : IParqueosBaseService
{
    public async Task<IReadOnlyList<BusParqueoBaseConsulta>> ConsultarAsync(CancellationToken cancellationToken)
        => await dbContext.Buses.AsNoTracking()
            .OrderBy(x => x.NumeroBus).ThenBy(x => x.CodBus)
            .Select(x => new BusParqueoBaseConsulta(x.CodBus, x.NumeroBus, x.NumeroPlaca, x.CodParqueoBase))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerParqueosAsync(CancellationToken cancellationToken)
        => await (from parqueo in dbContext.Parqueos.AsNoTracking()
                  join estacion in dbContext.Estaciones on parqueo.CodEstacion equals estacion.CodEstacion
                  join municipalidad in dbContext.Municipalidades on estacion.CodMunicipalidad equals municipalidad.CodMunicipalidad
                  orderby parqueo.NombreParqueo, parqueo.CodParqueo
                  select new OpcionCatalogo(parqueo.CodParqueo,
                      parqueo.NombreParqueo + " — " + estacion.NombreEstacion + " — " + municipalidad.NombreMunicipalidad))
            .ToListAsync(cancellationToken);

    public async Task<ResultadoAsignarParqueoBase> AsignarAsync(int bus, int parqueo, CancellationToken cancellationToken)
    {
        if (bus <= 0 || parqueo <= 0)
            throw new ArgumentException("Selecciona un bus y un parqueo válidos.");

        if (!await dbContext.Parqueos.AnyAsync(x => x.CodParqueo == parqueo, cancellationToken))
            return ResultadoAsignarParqueoBase.ParqueoNoEncontrado;

        var registro = await dbContext.Buses.SingleOrDefaultAsync(x => x.CodBus == bus, cancellationToken);
        if (registro is null)
            return ResultadoAsignarParqueoBase.BusNoEncontrado;

        var parqueoAnterior = registro.CodParqueoBase;
        registro.CodParqueoBase = parqueo;
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Parqueo base del bus {Bus}: {Anterior} -> {Actual}.", bus, parqueoAnterior, parqueo);
        return ResultadoAsignarParqueoBase.Guardado;
    }
}
