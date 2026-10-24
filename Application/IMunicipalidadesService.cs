using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public sealed record PaginaMunicipalidades(IReadOnlyList<Municipalidad> Registros, int Total, int Pagina);

public interface IMunicipalidadesService
{
    const int TamanoPagina = 15;
    Task<PaginaMunicipalidades> ConsultarAsync(string? buscar, int pagina, CancellationToken cancellationToken);
    Task<Municipalidad?> ObtenerAsync(int codigo, CancellationToken cancellationToken);
    Task<bool> GuardarAsync(Municipalidad datos, CancellationToken cancellationToken);
}
