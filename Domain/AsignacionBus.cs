namespace TransMetroUMG.Domain;

public class AsignacionBus
{
    public int CodAsignacionBus { get; set; }
    public DateOnly FechaAsignacion { get; set; }
    public int CodBus { get; set; }
    public int CodPiloto { get; set; }
    public int CodLinea { get; set; }
}