namespace TransMetroUMG.Application;

public sealed record BusParqueoBaseConsulta(
    int CodBus, string NumeroBus, string NumeroPlaca, int? CodParqueoBase);

public enum ResultadoAsignarParqueoBase { Guardado, BusNoEncontrado, ParqueoNoEncontrado }

public interface IParqueosBaseService
{
    Task<IReadOnlyList<BusParqueoBaseConsulta>> ConsultarAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerParqueosAsync(CancellationToken cancellationToken);
    Task<ResultadoAsignarParqueoBase> AsignarAsync(int bus, int parqueo, CancellationToken cancellationToken);
}
