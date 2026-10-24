using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoGuardarParametro { Guardado, ClaveDuplicada, NoEncontrado }

public interface IParametrosOperacionService
{
    Task<IReadOnlyList<Parametros>> ConsultarAsync(CancellationToken cancellationToken);
    Task<Parametros?> ObtenerAsync(int codigo, CancellationToken cancellationToken);
    Task<ResultadoGuardarParametro> GuardarAsync(Parametros datos, CancellationToken cancellationToken);
}

