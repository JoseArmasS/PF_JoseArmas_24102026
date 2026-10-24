namespace TransMetroUMG.Domain;

public sealed class ParadaOperacionBus
{
    public long CodOperacionBusParada { get; set; }
    public int CodOperacionBus { get; set; }
    public int Secuencia { get; set; }
    public int CodEstacion { get; set; }
    public DateTime? FechaHoraLlegada { get; set; }
    public int? CodUsuarioLlegada { get; set; }
    public DateTime? FechaHoraSalida { get; set; }
    public int? CodUsuarioSalida { get; set; }
}
