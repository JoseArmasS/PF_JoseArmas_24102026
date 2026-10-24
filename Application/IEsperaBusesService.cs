using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public interface IEsperaBusesService
{
    Task<IReadOnlyDictionary<long, EstadoEsperaBus>> ConsultarAsync(
        IReadOnlyList<CicloBusConsulta> ciclos, DateTime ahora, CancellationToken cancellationToken);
}
