using TransMetroUMG.Application;

namespace TransMetroUMG.Models;

public sealed class CatalogosViewModel
{
    public IReadOnlyList<OpcionCatalogo> Municipalidades { get; init; } = [];
    public IReadOnlyList<OpcionCatalogo> Roles { get; init; } = [];
    public bool ErrorCarga { get; init; }
}
