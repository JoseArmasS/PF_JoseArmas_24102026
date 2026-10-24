using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoGuardarPiloto
{
    Guardado,
    LicenciaDuplicada,
    MunicipalidadNoEncontrada,
    NoEncontrado
}

public interface IPilotosService
{
    Task<IReadOnlyList<Piloto>> ConsultarAsync(CancellationToken cancellationToken);
    Task<Piloto?> ObtenerAsync(int codigo, CancellationToken cancellationToken);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerMunicipalidadesAsync(CancellationToken cancellationToken);
    Task<ResultadoGuardarPiloto> GuardarAsync(Piloto datos, CancellationToken cancellationToken);
}
