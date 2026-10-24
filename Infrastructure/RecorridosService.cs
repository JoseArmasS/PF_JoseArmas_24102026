using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class RecorridosService(
    MunitransDbContext dbContext,
    ILogger<RecorridosService> logger) : IRecorridosService
{
    public async Task<IReadOnlyList<LineaEstacion>> ConsultarAsync(CancellationToken cancellationToken)
        => await (from recorrido in dbContext.LineasEstaciones.AsNoTracking()
                  join linea in dbContext.Lineas on recorrido.CodLinea equals linea.CodLinea
                  orderby linea.NombreLinea, linea.CodLinea, recorrido.OrdenEstacion
                  select recorrido).ToListAsync(cancellationToken);

    public Task<LineaEstacion?> ObtenerAsync(int codigo, CancellationToken cancellationToken)
        => dbContext.LineasEstaciones.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CodLineaEstacion == codigo, cancellationToken);

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerLineasAsync(CancellationToken cancellationToken)
        => await dbContext.Lineas.AsNoTracking().OrderBy(x => x.NombreLinea).ThenBy(x => x.CodLinea)
            .Select(x => new OpcionCatalogo(x.CodLinea, x.NombreLinea))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerEstacionesAsync(CancellationToken cancellationToken)
        => await dbContext.Estaciones.AsNoTracking().OrderBy(x => x.NombreEstacion).ThenBy(x => x.CodEstacion)
            .Select(x => new OpcionCatalogo(x.CodEstacion, x.NombreEstacion))
            .ToListAsync(cancellationToken);

    public async Task<ResultadoGuardarRecorrido> GuardarAsync(
        LineaEstacion datos, CancellationToken cancellationToken)
    {
        if (datos.CodLineaEstacion < 0 || datos.OrdenEstacion <= 0)
            throw new ArgumentException("El código o el orden del recorrido no es válido.");

        if (datos.DistanciaSiguienteKm is { } distancia
            && (distancia <= 0 || distancia > 99999.999m || decimal.Round(distancia, 3) != distancia))
            return ResultadoGuardarRecorrido.DistanciaNoValida;

        await using var transaccion = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        if (!await dbContext.Lineas.AnyAsync(x => x.CodLinea == datos.CodLinea, cancellationToken))
            return ResultadoGuardarRecorrido.LineaNoEncontrada;
        if (!await dbContext.Estaciones.AnyAsync(x => x.CodEstacion == datos.CodEstacion, cancellationToken))
            return ResultadoGuardarRecorrido.EstacionNoEncontrada;

        var registro = datos.CodLineaEstacion == 0 ? new LineaEstacion()
            : await dbContext.LineasEstaciones.SingleOrDefaultAsync(
                x => x.CodLineaEstacion == datos.CodLineaEstacion, cancellationToken);
        if (registro is null)
            return ResultadoGuardarRecorrido.NoEncontrado;

        var conflicto = await ComprobarDuplicadosAsync(datos, cancellationToken);
        if (conflicto.HasValue)
            return conflicto.Value;

        // Leer ambas líneas cuando una estación se traslada de un recorrido a otro.
        var anteriores = await dbContext.LineasEstaciones.AsNoTracking()
            .Where(x => x.CodLinea == datos.CodLinea || x.CodLinea == registro.CodLinea)
            .ToListAsync(cancellationToken);
        var propuestos = anteriores.Where(x => x.CodLineaEstacion != datos.CodLineaEstacion).Append(datos).ToList();
        if (datos.DistanciaSiguienteKm.HasValue && Siguiente(propuestos, datos) is null)
            return ResultadoGuardarRecorrido.SinEstacionSiguiente;

        // No borrar ni trasladar automáticamente una distancia de una fila distinta.
        foreach (var anterior in anteriores.Where(x => x.DistanciaSiguienteKm.HasValue
            && x.CodLineaEstacion != datos.CodLineaEstacion))
        {
            if (Siguiente(anteriores, anterior)?.CodEstacion != Siguiente(propuestos, anterior)?.CodEstacion)
                return ResultadoGuardarRecorrido.TramosConDistanciaAfectados;
        }

        registro.CodLinea = datos.CodLinea;
        registro.CodEstacion = datos.CodEstacion;
        registro.OrdenEstacion = datos.OrdenEstacion;
        registro.DistanciaSiguienteKm = datos.DistanciaSiguienteKm;
        if (datos.CodLineaEstacion == 0)
            dbContext.LineasEstaciones.Add(registro);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaccion.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            // Las restricciones UNIQUE protegen también los guardados simultáneos.
            await transaccion.RollbackAsync(cancellationToken);
            dbContext.Entry(registro).State = EntityState.Detached;
            conflicto = await ComprobarDuplicadosAsync(datos, cancellationToken);
            if (!conflicto.HasValue)
                throw;

            logger.LogWarning("No se guardó el recorrido de la línea {Codigo} por un duplicado.", datos.CodLinea);
            return conflicto.Value;
        }

        logger.LogInformation("Recorrido {Codigo}: {Operacion} realizada.",
            registro.CodLineaEstacion, datos.CodLineaEstacion == 0 ? "creación" : "actualización");
        return ResultadoGuardarRecorrido.Guardado;
    }

    private static LineaEstacion? Siguiente(IEnumerable<LineaEstacion> registros, LineaEstacion origen)
        => registros.Where(x => x.CodLinea == origen.CodLinea && x.OrdenEstacion > origen.OrdenEstacion)
            .MinBy(x => x.OrdenEstacion);

    private async Task<ResultadoGuardarRecorrido?> ComprobarDuplicadosAsync(
        LineaEstacion datos, CancellationToken cancellationToken)
    {
        if (await dbContext.LineasEstaciones.AnyAsync(
            x => x.CodLinea == datos.CodLinea && x.CodEstacion == datos.CodEstacion
                && x.CodLineaEstacion != datos.CodLineaEstacion, cancellationToken))
            return ResultadoGuardarRecorrido.EstacionDuplicada;

        if (await dbContext.LineasEstaciones.AnyAsync(
            x => x.CodLinea == datos.CodLinea && x.OrdenEstacion == datos.OrdenEstacion
                && x.CodLineaEstacion != datos.CodLineaEstacion, cancellationToken))
            return ResultadoGuardarRecorrido.OrdenDuplicado;

        return null;
    }
}
