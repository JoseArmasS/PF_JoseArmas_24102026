using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoGuardarLinea
{
    Guardado,
    MunicipalidadNoEncontrada,
    NoEncontrado
}

public interface ILineasService
{
    Task<IReadOnlyList<Linea>> ConsultarAsync(CancellationToken cancellationToken);
    Task<Linea?> ObtenerAsync(int codigo, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerMunicipalidadesAsync(CancellationToken cancellationToken);
    Task<ResultadoGuardarLinea> GuardarAsync(Linea datos, CancellationToken cancellationToken);
}
