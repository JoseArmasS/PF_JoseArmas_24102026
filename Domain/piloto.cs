namespace TransMetroUMG.Domain;

public enum EstadoPiloto : byte
{
    Inactivo = 0,
    Activo = 1
}

public class Piloto
{
    public int CodPiloto { get; set; }
    public int CodMunicipalidad { get; set; }
    public string NombresPiloto { get; set; } = string.Empty;
    public string ApellidosPiloto { get; set; } = string.Empty;
    public string? TelefonoPiloto { get; set; }
    public string? HistorialEducativo { get; set; }
    public string NumeroLicencia { get; set; } = string.Empty;
    public string TipoLicencia { get; set; } = string.Empty;
    public DateOnly FechaVencimientoLicencia { get; set; }
    public EstadoPiloto Estado { get; set; } = EstadoPiloto.Activo;
}
