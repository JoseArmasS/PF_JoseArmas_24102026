using Microsoft.EntityFrameworkCore;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;
using Microsoft.Data.SqlClient;


namespace TransMetroUMG.Infrastructure;

public sealed class BusesService : IBusesService
{
    private readonly MunitransDbContext _dbContext;
    private readonly ILogger<BusesService> _logger;

    public BusesService(
        MunitransDbContext dbContext,
        ILogger<BusesService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Bus>> ConsultarAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Buses
            .AsNoTracking()
            .OrderBy(bus => bus.NumeroBus)
            .ThenBy(bus => bus.CodBus)
            .ToListAsync(cancellationToken);
    }

    public Task<Bus?> ObtenerAsync(
        int codigo,
        CancellationToken cancellationToken)
    {
        return _dbContext.Buses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                bus => bus.CodBus == codigo,
                cancellationToken);
    }

    public async  Task<IReadOnlyList<OpcionCatalogo>> ObtenerMunicipalidadesAsync(
        CancellationToken cancellationToken)
    {
        return await _dbContext.Municipalidades
       .AsNoTracking()
       .OrderBy(municipalidad => municipalidad.NombreMunicipalidad)
       .Select(municipalidad => new OpcionCatalogo(
           municipalidad.CodMunicipalidad,
           municipalidad.NombreMunicipalidad))
       .ToListAsync(cancellationToken);
    }

    public async Task<ResultadoGuardarBus> GuardarAsync(
     Bus datos,
     CancellationToken cancellationToken)
    {
        var numeroBus = datos.NumeroBus.Trim();
        var placa = datos.NumeroPlaca.Trim();

        if (datos.CodBus < 0)
            throw new ArgumentException("codigo del bus inválido.");

        if (string.IsNullOrWhiteSpace(numeroBus) || numeroBus.Length > 30)
            throw new ArgumentException("número bus inválido.");

        if (string.IsNullOrWhiteSpace(placa) || placa.Length > 20)
            throw new ArgumentException("placa inválida");

        if (datos.CapacidadPasajeros <= 0)
            throw new ArgumentException("La capacidad debe ser mayor que 0");

        if (datos.ModeloBus?.Length > 100)
            throw new ArgumentException("tamaño maximo 100 caracteres");

        if (!Enum.IsDefined(datos.Estado))
            throw new ArgumentException("estado inválido");

        var municipalidadExiste = await _dbContext.Municipalidades
            .AnyAsync(
                municipalidad =>
                    municipalidad.CodMunicipalidad == datos.CodMunicipalidad,
                cancellationToken);

        if (!municipalidadExiste)
            return ResultadoGuardarBus.MunicipalidadNoEncontrada;

        var placaDuplicada = await _dbContext.Buses
            .AnyAsync(
                bus => bus.NumeroPlaca == placa
                    && bus.CodBus != datos.CodBus,
                cancellationToken);

        if (placaDuplicada)
            return ResultadoGuardarBus.PlacaDuplicada;

        // guardamos
        var registro = datos.CodBus == 0
        ? new Bus()
        : await _dbContext.Buses.SingleOrDefaultAsync(
            bus => bus.CodBus == datos.CodBus,
            cancellationToken);

            if (registro is null)
                return ResultadoGuardarBus.NoEncontrado;

            registro.CodMunicipalidad = datos.CodMunicipalidad;
            registro.NumeroBus = numeroBus;
            registro.NumeroPlaca = placa;
            registro.CapacidadPasajeros = datos.CapacidadPasajeros;
            registro.ModeloBus = string.IsNullOrWhiteSpace(datos.ModeloBus)
                ? null
                : datos.ModeloBus.Trim();
            registro.AnioBus = datos.AnioBus;
            registro.Estado = datos.Estado;

            if (datos.CodBus == 0)
                _dbContext.Buses.Add(registro);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is SqlException sql
                    && (sql.Number == 2601 || sql.Number == 2627))
            {
                _dbContext.Entry(registro).State = EntityState.Detached;

                _logger.LogWarning(
                    "No se guardó el bus porque su placa está duplicada.");

                return ResultadoGuardarBus.PlacaDuplicada;
            }

            _logger.LogInformation(
                "Bus {Codigo}: {Operacion} realizada.",
                registro.CodBus,
                datos.CodBus == 0 ? "creación" : "actualización");

            return ResultadoGuardarBus.Guardado;

    }


}