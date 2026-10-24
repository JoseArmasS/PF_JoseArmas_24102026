namespace TransMetroUMG.Domain;

public enum EstadoBus : byte
{
    Inactivo = 0,
    Activo = 1
}

public class Bus
{
    public int CodBus { get; set; }
    public int CodMunicipalidad { get; set; }
    public int? CodParqueoBase { get; set; }
    public string NumeroBus { get; set; } = string.Empty;
    public string NumeroPlaca { get; set; } = string.Empty;
    public int CapacidadPasajeros { get; set; }
    public string? ModeloBus { get; set; }
    public short? AnioBus { get; set; }
    public EstadoBus Estado { get; set; } = EstadoBus.Activo;
}
