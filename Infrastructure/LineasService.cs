using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class LineasService(
    MunitransDbContext dbContext,
    ILogger<LineasService> logger) : ILineasService
{
    public async Task<IReadOnlyList<Linea>> ConsultarAsync(CancellationToken cancellationToken)
        => await dbContext.Lineas.AsNoTracking()
            .OrderBy(x => x.NombreLinea).ThenBy(x => x.CodLinea)
            .ToListAsync(cancellationToken);

    public Task<Linea?> ObtenerAsync(int codigo, CancellationToken cancellationToken)
        => dbContext.Lineas.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CodLinea == codigo, cancellationToken);

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerMunicipalidadesAsync(CancellationToken cancellationToken)
        => await dbContext.Municipalidades.AsNoTracking()
            .OrderBy(x => x.NombreMunicipalidad)
            .Select(x => new OpcionCatalogo(x.CodMunicipalidad, x.NombreMunicipalidad))
            .ToListAsync(cancellationToken);

    public async Task<ResultadoGuardarLinea> GuardarAsync(Linea datos, CancellationToken cancellationToken)
    {
        var nombre = datos.NombreLinea.Trim();
        if (datos.CodLinea < 0 || string.IsNullOrWhiteSpace(nombre) || nombre.Length > 100)
            throw new ArgumentException("El nombre de la línea no es válido.");
        if (datos.DescripcionLinea?.Length > 250)
            throw new ArgumentException("La descripción supera 250 caracteres.");
        if (!Enum.IsDefined(datos.Estado))
            throw new ArgumentException("El estado no es válido.");

        if (!await dbContext.Municipalidades.AnyAsync(
            x => x.CodMunicipalidad == datos.CodMunicipalidad, cancellationToken))
            return ResultadoGuardarLinea.MunicipalidadNoEncontrada;

        var registro = datos.CodLinea == 0 ? new Linea()
            : await dbContext.Lineas.SingleOrDefaultAsync(
                x => x.CodLinea == datos.CodLinea, cancellationToken);
        if (registro is null)
            return ResultadoGuardarLinea.NoEncontrado;

        registro.CodMunicipalidad = datos.CodMunicipalidad;
        registro.NombreLinea = nombre;
        registro.DescripcionLinea = string.IsNullOrWhiteSpace(datos.DescripcionLinea)
            ? null : datos.DescripcionLinea.Trim();
        registro.Estado = datos.Estado;
        if (datos.CodLinea == 0)
            dbContext.Lineas.Add(registro);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Línea {Codigo}: {Operacion} realizada.",
            registro.CodLinea, datos.CodLinea == 0 ? "creación" : "actualización");
        return ResultadoGuardarLinea.Guardado;
    }
}
