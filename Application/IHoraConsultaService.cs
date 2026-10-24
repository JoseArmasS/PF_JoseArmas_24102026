namespace TransMetroUMG.Application;

public interface IHoraConsultaService
{
    Task<DateTime> ObtenerAsync(CancellationToken cancellationToken);
}
