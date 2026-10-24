using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoGuardarAsignacion
{
    Guardado,
    BusNoEncontrado,
    PilotoNoEncontrado,
    LineaNoEncontrada,
    NoEncontrado,
    AsignacionUtilizada
}

public interface IAsignacionesService
{
    Task<IReadOnlyList<AsignacionBus>> ConsultarAsync(CancellationToken cancellationToken);
    Task<AsignacionBus?> ObtenerAsync(int codigo, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerBusesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerPilotosAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerLineasAsync(CancellationToken cancellationToken);
    Task<ResultadoGuardarAsignacion> GuardarAsync(AsignacionBus datos, CancellationToken cancellationToken);
}
