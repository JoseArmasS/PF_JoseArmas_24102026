using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoGuardarParqueo
{
    Guardado,
    EstacionNoEncontrada,
    NoEncontrado
}

// La municipalidad se consulta a través de la estación; no se almacena en PARQUEO.
public sealed record EstacionParqueoOpcion(int Codigo, string Nombre, string Municipalidad);

public interface IParqueosService
{
    Task<IReadOnlyList<Parqueo>> ConsultarAsync(CancellationToken cancellationToken);
    Task<Parqueo?> ObtenerAsync(int codigo, CancellationToken cancellationToken);
    Task<IReadOnlyList<EstacionParqueoOpcion>> ObtenerEstacionesAsync(CancellationToken cancellationToken);
    Task<ResultadoGuardarParqueo> GuardarAsync(Parqueo datos, CancellationToken cancellationToken);
}