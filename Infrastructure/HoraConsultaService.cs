using TransMetroUMG.Application;

namespace TransMetroUMG.Infrastructure;

public sealed class HoraConsultaService(MunitransDbContext dbContext) : IHoraConsultaService
{
    // Las mediciones usan SYSDATETIME; leer el mismo reloj evita mezclar zonas horarias.
    public Task<DateTime> ObtenerAsync(CancellationToken cancellationToken)
        => ControlCiclosSql.HoraAsync(dbContext, cancellationToken);
}
