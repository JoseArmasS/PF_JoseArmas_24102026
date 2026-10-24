namespace TransMetroUMG.Domain;

public enum EstadoParqueo : byte
{
    Inactivo = 0,
    Activo = 1
}

public class Parqueo
{
    public int CodParqueo { get; set; }
    public int CodEstacion { get; set; }
    public string NombreParqueo { get; set; } = string.Empty;
    public string DireccionParqueo { get; set; } = string.Empty;
    public int CapacidadTotal { get; set; }
    public EstadoParqueo Estado { get; set; } = EstadoParqueo.Activo;
}