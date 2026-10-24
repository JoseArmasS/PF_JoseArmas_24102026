namespace TransMetroUMG.Domain;

public class ControlParqueo
{
    public long CodControlParqueo { get; set; }
    public int CodParqueo { get; set; }
    public string PlacaVehiculo { get; set; } = string.Empty;
    public DateTime FechaHoraEntrada { get; set; }
    public DateTime? FechaHoraSalida { get; set; }
}