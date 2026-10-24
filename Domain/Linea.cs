namespace TransMetroUMG.Domain;

public enum EstadoLinea : byte
{
    Inactivo = 0,
    Activo = 1
}

public class Linea
{
    public int CodLinea { get; set; }
    public int CodMunicipalidad { get; set; }
    public string NombreLinea { get; set; } = string.Empty;
    public string? DescripcionLinea { get; set; }
    public EstadoLinea Estado { get; set; } = EstadoLinea.Activo;
}