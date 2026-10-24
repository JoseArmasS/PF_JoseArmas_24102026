namespace TransMetroUMG.Domain;

public enum EstadoUsuario : byte
{
    Inactivo = 0,
    Activo = 1
}

public sealed class Usuario
{
    public int CodUsuario { get; set; }
    public int CodMunicipalidad { get; set; }
    public int CodRol { get; set; }

    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string CorreoElectronico { get; set; } = string.Empty;

    public string ContrasenaHash { get; set; } = string.Empty;

    public DateTime? FechaExpiracionContrasena { get; set; }
    public EstadoUsuario Estado { get; set; } = EstadoUsuario.Activo;
    public DateTime FechaCreacion { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}