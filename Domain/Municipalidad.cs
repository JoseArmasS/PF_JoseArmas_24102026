namespace TransMetroUMG.Domain;

public enum EstadoMunicipalidad : byte
{
    Inactivo = 0,
    Activo = 1
}

public sealed class Municipalidad
{
    public int CodMunicipalidad { get; set; }
    public string NombreMunicipalidad { get; set; } = string.Empty;
    public string? DireccionMunicipalidad { get; set; }
    public string? TelefonoMunicipalidad { get; set; }
    public string? CorreoElectronico { get; set; }
    public EstadoMunicipalidad Estado { get; set; } = EstadoMunicipalidad.Activo;
}
