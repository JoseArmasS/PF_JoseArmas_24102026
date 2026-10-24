using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoGuardarEstacion
{
    Guardado,
    MunicipalidadNoEncontrada,
    NoEncontrado
}

public interface IEstacionesService
{
    Task<IReadOnlyList<Estacion>> ConsultarAsync(CancellationToken cancellationToken);
    Task<Estacion?> ObtenerAsync(int codigo, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerMunicipalidadesAsync(CancellationToken cancellationToken);
    Task<ResultadoGuardarEstacion> GuardarAsync(Estacion datos, CancellationToken cancellationToken);
}

