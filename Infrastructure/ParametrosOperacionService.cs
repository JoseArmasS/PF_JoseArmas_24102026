using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class ParametrosOperacionService(
    MunitransDbContext dbContext,
    ILogger<ParametrosOperacionService> logger) : IParametrosOperacionService
{
    public async Task<IReadOnlyList<Parametros>> ConsultarAsync(CancellationToken cancellationToken)
        => await dbContext.ParametrosOperacion.AsNoTracking()
            .OrderBy(x => x.CodParametroOperacion).ToListAsync(cancellationToken);

    public Task<Parametros?> ObtenerAsync(int codigo, CancellationToken cancellationToken)
        => dbContext.ParametrosOperacion.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CodParametroOperacion == codigo, cancellationToken);

    public async Task<ResultadoGuardarParametro> GuardarAsync(
        Parametros datos, CancellationToken cancellationToken)
    {
        var clave = datos.Clave.Trim();
        var descripcion = datos.Descripcion.Trim();
        if (datos.CodParametroOperacion < 0 || string.IsNullOrWhiteSpace(clave) || clave.Length > 100
            || string.IsNullOrWhiteSpace(descripcion) || descripcion.Length > 250)
            throw new ArgumentException("Los datos del parámetro no son válidos.");

        // Respetar DECIMAL(12,4) sin redondear silenciosamente.
        if (datos.Valor.HasValue && (Math.Abs(datos.Valor.Value) > 99999999.9999m
            || decimal.Round(datos.Valor.Value, 4) != datos.Valor.Value))
            throw new ArgumentException("El valor no corresponde a DECIMAL(12,4).");

        var registro = datos.CodParametroOperacion == 0 ? new Parametros()
            : await dbContext.ParametrosOperacion.SingleOrDefaultAsync(
                x => x.CodParametroOperacion == datos.CodParametroOperacion, cancellationToken);
        if (registro is null) return ResultadoGuardarParametro.NoEncontrado;

        if (await dbContext.ParametrosOperacion.AnyAsync(
            x => x.Clave == clave && x.CodParametroOperacion != datos.CodParametroOperacion, cancellationToken))
            return ResultadoGuardarParametro.ClaveDuplicada;

        registro.Clave = clave;
        registro.Valor = datos.Valor;
        registro.Descripcion = descripcion;
        if (datos.CodParametroOperacion == 0) dbContext.ParametrosOperacion.Add(registro);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            dbContext.Entry(registro).State = EntityState.Detached;
            logger.LogWarning("No se guardó el parámetro porque su clave está duplicada.");
            return ResultadoGuardarParametro.ClaveDuplicada;
        }

        logger.LogInformation("Parámetro {Codigo}: {Operacion} realizada.",
            registro.CodParametroOperacion, datos.CodParametroOperacion == 0 ? "creación" : "actualización");
        return ResultadoGuardarParametro.Guardado;
    }
}

