using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class ParqueosService(
    MunitransDbContext dbContext,
    ILogger<ParqueosService> logger) : IParqueosService
{
    public async Task<IReadOnlyList<Parqueo>> ConsultarAsync(CancellationToken cancellationToken)
        => await dbContext.Parqueos.AsNoTracking()
            .OrderBy(x => x.NombreParqueo).ThenBy(x => x.CodParqueo)
            .ToListAsync(cancellationToken);

    public Task<Parqueo?> ObtenerAsync(int codigo, CancellationToken cancellationToken)
        => dbContext.Parqueos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CodParqueo == codigo, cancellationToken);

    public async Task<IReadOnlyList<EstacionParqueoOpcion>> ObtenerEstacionesAsync(CancellationToken cancellationToken)
        => await (from estacion in dbContext.Estaciones.AsNoTracking()
                  join municipalidad in dbContext.Municipalidades.AsNoTracking()
                      on estacion.CodMunicipalidad equals municipalidad.CodMunicipalidad
                  orderby municipalidad.NombreMunicipalidad, estacion.NombreEstacion, estacion.CodEstacion
                  select new EstacionParqueoOpcion(estacion.CodEstacion,
                      estacion.NombreEstacion, municipalidad.NombreMunicipalidad))
            .ToListAsync(cancellationToken);

    public async Task<ResultadoGuardarParqueo> GuardarAsync(Parqueo datos, CancellationToken cancellationToken)
    {
        var nombre = datos.NombreParqueo?.Trim() ?? string.Empty;
        var direccion = datos.DireccionParqueo?.Trim() ?? string.Empty;
        if (datos.CodParqueo < 0 || string.IsNullOrWhiteSpace(nombre) || nombre.Length > 150
            || string.IsNullOrWhiteSpace(direccion) || direccion.Length > 250
            || datos.CapacidadTotal <= 0 || !Enum.IsDefined(datos.Estado))
            throw new ArgumentException("Los datos del parqueo no son válidos.");

        if (!await dbContext.Estaciones.AnyAsync(
            x => x.CodEstacion == datos.CodEstacion, cancellationToken))
            return ResultadoGuardarParqueo.EstacionNoEncontrada;

        var registro = datos.CodParqueo == 0 ? new Parqueo()
            : await dbContext.Parqueos.SingleOrDefaultAsync(
                x => x.CodParqueo == datos.CodParqueo, cancellationToken);
        if (registro is null)
            return ResultadoGuardarParqueo.NoEncontrado;

        registro.CodEstacion = datos.CodEstacion;
        registro.NombreParqueo = nombre;
        registro.DireccionParqueo = direccion;
        registro.CapacidadTotal = datos.CapacidadTotal;
        registro.Estado = datos.Estado;
        if (datos.CodParqueo == 0)
            dbContext.Parqueos.Add(registro);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Parqueo {Codigo}: {Operacion} realizada.",
            registro.CodParqueo, datos.CodParqueo == 0 ? "creación" : "actualización");
        return ResultadoGuardarParqueo.Guardado;
    }
}