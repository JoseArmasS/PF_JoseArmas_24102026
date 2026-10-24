using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class EstacionesService(
    MunitransDbContext dbContext,
    ILogger<EstacionesService> logger) : IEstacionesService
{
    public async Task<IReadOnlyList<Estacion>> ConsultarAsync(CancellationToken cancellationToken)
        => await dbContext.Estaciones.AsNoTracking()
            .OrderBy(x => x.NombreEstacion).ThenBy(x => x.CodEstacion)
            .ToListAsync(cancellationToken);

    public Task<Estacion?> ObtenerAsync(int codigo, CancellationToken cancellationToken)
        => dbContext.Estaciones.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CodEstacion == codigo, cancellationToken);

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerMunicipalidadesAsync(CancellationToken cancellationToken)
        => await dbContext.Municipalidades.AsNoTracking()
            .OrderBy(x => x.NombreMunicipalidad)
            .Select(x => new OpcionCatalogo(x.CodMunicipalidad, x.NombreMunicipalidad))
            .ToListAsync(cancellationToken);

    public async Task<ResultadoGuardarEstacion> GuardarAsync(Estacion datos, CancellationToken cancellationToken)
    {
        var nombre = datos.NombreEstacion.Trim();
        var direccion = datos.DireccionEstacion.Trim();
        if (datos.CodEstacion < 0 || string.IsNullOrWhiteSpace(nombre) || nombre.Length > 150
            || string.IsNullOrWhiteSpace(direccion) || direccion.Length > 250)
            throw new ArgumentException("Los datos de la estación no son válidos.");
        if (!Enum.IsDefined(datos.Estado))
            throw new ArgumentException("El estado no es válido.");
        if (datos.CapacidadPasajeros is <= 0)
            throw new ArgumentException("La capacidad debe ser mayor que cero.");
        if (datos.Latitud.HasValue && (datos.Latitud.Value < -90 || datos.Latitud.Value > 90
            || decimal.Round(datos.Latitud.Value, 7) != datos.Latitud.Value))
            throw new ArgumentException("La latitud no es válida.");
        if (datos.Longitud.HasValue && (datos.Longitud.Value < -180 || datos.Longitud.Value > 180
            || decimal.Round(datos.Longitud.Value, 7) != datos.Longitud.Value))
            throw new ArgumentException("La longitud no es válida.");

        if (!await dbContext.Municipalidades.AnyAsync(
            x => x.CodMunicipalidad == datos.CodMunicipalidad, cancellationToken))
            return ResultadoGuardarEstacion.MunicipalidadNoEncontrada;

        var registro = datos.CodEstacion == 0 ? new Estacion()
            : await dbContext.Estaciones.SingleOrDefaultAsync(
                x => x.CodEstacion == datos.CodEstacion, cancellationToken);
        if (registro is null)
            return ResultadoGuardarEstacion.NoEncontrado;

        registro.CodMunicipalidad = datos.CodMunicipalidad;
        registro.NombreEstacion = nombre;
        registro.DireccionEstacion = direccion;
        registro.Latitud = datos.Latitud;
        registro.Longitud = datos.Longitud;
        registro.CapacidadPasajeros = datos.CapacidadPasajeros;
        registro.Estado = datos.Estado;
        if (datos.CodEstacion == 0)
            dbContext.Estaciones.Add(registro);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Estación {Codigo}: {Operacion} realizada.",
            registro.CodEstacion, datos.CodEstacion == 0 ? "creación" : "actualización");
        return ResultadoGuardarEstacion.Guardado;
    }
}

