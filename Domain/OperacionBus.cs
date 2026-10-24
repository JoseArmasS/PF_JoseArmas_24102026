namespace TransMetroUMG.Domain;

public sealed class OperacionBus
{
    public int CodOperacionBus { get; set; }
    public int CodAsignacionBus { get; set; }
    public int CodEstacionSalida { get; set; }
    public int CodUsuarioOperador { get; set; }
    public DateTime FechaHoraRegistro { get; set; }
    public DateTime FechaHoraInicioEspera { get; set; }
    public DateTime? FechaHoraSalida { get; set; }
    public int CapacidadRegistrada { get; set; }
    public short? SentidoInicial { get; set; }
    public DateTime? FechaHoraCierre { get; set; }
    public int? CodUsuarioCierre { get; set; }
}
