using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public enum ResultadoCrearUsuario
{
    Creado,
    NombreDuplicado,
    MunicipalidadNoEncontrada,
    RolNoEncontrado
}

public sealed class CrearUsuarioDatos
{
    public string NombreUsuario { get; init; } = string.Empty;
    public string NombreCompleto { get; init; } = string.Empty;
    public string CorreoElectronico { get; init; } = string.Empty;
    public string Contrasena { get; init; } = string.Empty;

    public int CodMunicipalidad { get; init; }
    public int CodRol { get; init; }
    public EstadoUsuario Estado { get; init; }
    public DateTime? FechaExpiracionContrasena { get; init; }
}

public interface IUsuariosService
{
    Task<ResultadoCrearUsuario> CrearAsync(
        CrearUsuarioDatos datos,
        CancellationToken cancellationToken);
}