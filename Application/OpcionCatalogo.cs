namespace TransMetroUMG.Application;

public sealed record OpcionCatalogo(int Codigo, string Nombre);

public sealed record CatalogosUsuarios(
    IReadOnlyList<OpcionCatalogo> Municipalidades,
    IReadOnlyList<OpcionCatalogo> Roles);

public interface ICatalogosUsuariosService
{
    Task<CatalogosUsuarios> ObtenerAsync(CancellationToken cancellationToken);
}
