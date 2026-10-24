using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoGuardarBus
{
    Guardado,
    PlacaDuplicada,
    MunicipalidadNoEncontrada,
    NoEncontrado
}

public interface IBusesService
{
    Task<IReadOnlyList<Bus>> ConsultarAsync(
        CancellationToken cancellationToken);

    Task<Bus?> ObtenerAsync(
        int codigo,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OpcionCatalogo>> ObtenerMunicipalidadesAsync(
        CancellationToken cancellationToken);

    Task<ResultadoGuardarBus> GuardarAsync(
        Bus datos,
        CancellationToken cancellationToken);
}
