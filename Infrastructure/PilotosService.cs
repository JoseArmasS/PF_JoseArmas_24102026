using Microsoft.Data.SqlClient;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Infrastructure;

public sealed class PilotosService(MunitransDbContext dbContext, ILogger<PilotosService> logger)
    : IPilotosService
{
    public async Task<IReadOnlyList<Piloto>> ConsultarAsync(CancellationToken cancellationToken)
        => await dbContext.Pilotos.AsNoTracking()
            .OrderBy(x => x.ApellidosPiloto).ThenBy(x => x.NombresPiloto).ThenBy(x => x.CodPiloto)
            .ToListAsync(cancellationToken);

    public Task<Piloto?> ObtenerAsync(int codigo, CancellationToken cancellationToken)
        => dbContext.Pilotos.AsNoTracking()
            .SingleOrDefaultAsync(x => x.CodPiloto == codigo, cancellationToken);

    public async Task<IReadOnlyList<OpcionCatalogo>> ObtenerMunicipalidadesAsync(CancellationToken cancellationToken)
        => await dbContext.Municipalidades.AsNoTracking().OrderBy(x => x.NombreMunicipalidad)
            .Select(x => new OpcionCatalogo(x.CodMunicipalidad, x.NombreMunicipalidad))
            .ToListAsync(cancellationToken);

    public async Task<ResultadoGuardarPiloto> GuardarAsync(Piloto datos, CancellationToken cancellationToken)
    {
        var nombres = datos.NombresPiloto.Trim();
        var apellidos = datos.ApellidosPiloto.Trim();
        var licencia = datos.NumeroLicencia.Trim();
        var tipoLicencia = datos.TipoLicencia.Trim();
        var telefono = string.IsNullOrWhiteSpace(datos.TelefonoPiloto) ? null : datos.TelefonoPiloto.Trim();
        var historialEducativo = string.IsNullOrWhiteSpace(datos.HistorialEducativo) ? null : datos.HistorialEducativo.Trim();

        if (datos.TelefonoPiloto?.Length > 25 || !new PhoneAttribute().IsValid(telefono)
            || datos.HistorialEducativo?.Length > 2000)
            throw new ArgumentException("El teléfono o el historial educativo del piloto no son válidos.");

        if (datos.CodPiloto < 0 || datos.CodMunicipalidad <= 0
            || string.IsNullOrWhiteSpace(nombres) || nombres.Length > 100
            || string.IsNullOrWhiteSpace(apellidos) || apellidos.Length > 100
            || string.IsNullOrWhiteSpace(licencia) || licencia.Length > 50
            || string.IsNullOrWhiteSpace(tipoLicencia) || tipoLicencia.Length > 20
            || !Enum.IsDefined(datos.Estado))
            throw new ArgumentException("Los datos del piloto no son válidos.");

        if (!await dbContext.Municipalidades.AnyAsync(
            x => x.CodMunicipalidad == datos.CodMunicipalidad, cancellationToken))
            return ResultadoGuardarPiloto.MunicipalidadNoEncontrada;

        if (await dbContext.Pilotos.AnyAsync(
            x => x.NumeroLicencia == licencia && x.CodPiloto != datos.CodPiloto, cancellationToken))
            return ResultadoGuardarPiloto.LicenciaDuplicada;

        var registro = datos.CodPiloto == 0 ? new Piloto()
            : await dbContext.Pilotos.SingleOrDefaultAsync(x => x.CodPiloto == datos.CodPiloto, cancellationToken);
        if (registro is null)
            return ResultadoGuardarPiloto.NoEncontrado;

        registro.CodMunicipalidad = datos.CodMunicipalidad;
        registro.NombresPiloto = nombres;
        registro.ApellidosPiloto = apellidos;
        registro.TelefonoPiloto = telefono;
        registro.HistorialEducativo = historialEducativo;
        registro.NumeroLicencia = licencia;
        registro.TipoLicencia = tipoLicencia;
        registro.FechaVencimientoLicencia = datos.FechaVencimientoLicencia;
        registro.Estado = datos.Estado;

        if (datos.CodPiloto == 0)
            dbContext.Pilotos.Add(registro);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException sql && sql.Number is 2601 or 2627)
        {
            dbContext.Entry(registro).State = EntityState.Detached;
            logger.LogWarning("No se guardó el piloto porque su licencia está duplicada.");
            return ResultadoGuardarPiloto.LicenciaDuplicada;
        }

        logger.LogInformation("Piloto {Codigo}: {Operacion} realizada.",
            registro.CodPiloto, datos.CodPiloto == 0 ? "creación" : "actualización");
        return ResultadoGuardarPiloto.Guardado;
    }
}
