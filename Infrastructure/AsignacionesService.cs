using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class AsignacionesService(MunitransDbContext dbContext, ILogger<AsignacionesService> logger)
    : IAsignacionesService
{
    public async Task<IReadOnlyList<AsignacionBus>> ConsultarAsync(CancellationToken cancellationToken)
        => await dbContext.AsignacionesBuses.AsNoTracking()
            .OrderByDescending(x => x.FechaAsignacion).ThenByDescending(x => x.CodAsignacionBus)
            .ToListAsync(cancellationToken);

    public Task<AsignacionBus?> ObtenerAsync(int codigo, CancellationToken cancellationToken)
        => dbContext.AsignacionesBuses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CodAsignacionBus == codigo, cancellationToken);

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerBusesAsync(CancellationToken cancellationToken)
        => await dbContext.Buses.AsNoTracking().OrderBy(x => x.NumeroBus).ThenBy(x => x.CodBus)
            .Select(x => new OpcionCatalogo(x.CodBus, x.NumeroBus + " — " + x.NumeroPlaca))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerPilotosAsync(CancellationToken cancellationToken)
        => await dbContext.Pilotos.AsNoTracking().OrderBy(x => x.ApellidosPiloto).ThenBy(x => x.NombresPiloto)
            .Select(x => new OpcionCatalogo(x.CodPiloto,
                x.NombresPiloto + " " + x.ApellidosPiloto + " — " + x.NumeroLicencia))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerLineasAsync(CancellationToken cancellationToken)
        => await dbContext.Lineas.AsNoTracking().OrderBy(x => x.NombreLinea).ThenBy(x => x.CodLinea)
            .Select(x => new OpcionCatalogo(x.CodLinea, x.NombreLinea + " (" + x.CodLinea + ")"))
            .ToListAsync(cancellationToken);

    public async Task<ResultadoGuardarAsignacion> GuardarAsync(AsignacionBus datos, CancellationToken cancellationToken)
    {
        if (datos.CodAsignacionBus < 0 || datos.CodBus <= 0 || datos.CodPiloto <= 0 || datos.CodLinea <= 0)
            throw new ArgumentException("Los datos de la asignación no son válidos.");

        if (!await dbContext.Buses.AnyAsync(x => x.CodBus == datos.CodBus, cancellationToken))
            return ResultadoGuardarAsignacion.BusNoEncontrado;
        if (!await dbContext.Pilotos.AnyAsync(x => x.CodPiloto == datos.CodPiloto, cancellationToken))
            return ResultadoGuardarAsignacion.PilotoNoEncontrado;
        if (!await dbContext.Lineas.AnyAsync(x => x.CodLinea == datos.CodLinea, cancellationToken))
            return ResultadoGuardarAsignacion.LineaNoEncontrada;

        await using var transaccion = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var registro = datos.CodAsignacionBus == 0 ? new AsignacionBus()
            : await dbContext.AsignacionesBuses.FromSqlInterpolated($"""
                SELECT COD_ASIGNACION_BUS, FECHA_ASIGNACION, COD_BUS, COD_PILOTO, COD_LINEA
                FROM dbo.ASIGNACION_BUS WITH (UPDLOCK, HOLDLOCK)
                WHERE COD_ASIGNACION_BUS = {datos.CodAsignacionBus}
                """).SingleOrDefaultAsync(
                x => x.CodAsignacionBus == datos.CodAsignacionBus, cancellationToken);
        if (registro is null)
            return ResultadoGuardarAsignacion.NoEncontrado;

        if (datos.CodAsignacionBus > 0 && (registro.FechaAsignacion != datos.FechaAsignacion
            || registro.CodBus != datos.CodBus || registro.CodPiloto != datos.CodPiloto || registro.CodLinea != datos.CodLinea)
            && await dbContext.OperacionesBuses.AnyAsync(x => x.CodAsignacionBus == datos.CodAsignacionBus, cancellationToken))
            return ResultadoGuardarAsignacion.AsignacionUtilizada;

        registro.FechaAsignacion = datos.FechaAsignacion;
        registro.CodBus = datos.CodBus;
        registro.CodPiloto = datos.CodPiloto;
        registro.CodLinea = datos.CodLinea;
        if (datos.CodAsignacionBus == 0)
            dbContext.AsignacionesBuses.Add(registro);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
        logger.LogInformation("Asignación {Codigo}: {Operacion} realizada.",
            registro.CodAsignacionBus, datos.CodAsignacionBus == 0 ? "creación" : "actualización");
        return ResultadoGuardarAsignacion.Guardado;
    }
}
