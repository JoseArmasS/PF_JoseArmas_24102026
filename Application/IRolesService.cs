using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoGuardarRol
{
    Guardado,
    NombreDuplicado,
    NoEncontrado
}

public interface IRolesService
{
    Task<IReadOnlyList<Rol>> ConsultarAsync(
        CancellationToken cancellationToken);

    Task<Rol?> ObtenerAsync(
        int codigo,
        CancellationToken cancellationToken);

    Task<ResultadoGuardarRol> GuardarAsync(
        Rol datos,
        CancellationToken cancellationToken);
}