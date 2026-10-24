using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoGuardarRecorrido
{
    Guardado,
    LineaNoEncontrada,
    EstacionNoEncontrada,
    EstacionDuplicada,
    OrdenDuplicado,
    NoEncontrado,
    DistanciaNoValida,
    SinEstacionSiguiente,
    TramosConDistanciaAfectados
}

public interface IRecorridosService
{
    Task<IReadOnlyList<LineaEstacion>> ConsultarAsync(CancellationToken cancellationToken);
    Task<LineaEstacion?> ObtenerAsync(int codigo, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerLineasAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerEstacionesAsync(CancellationToken cancellationToken);
    Task<ResultadoGuardarRecorrido> GuardarAsync(LineaEstacion datos, CancellationToken cancellationToken);
}
