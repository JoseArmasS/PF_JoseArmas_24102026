namespace TransMetroUMG.Domain;

public enum EstadoTarjeta : byte
{
    Inactiva = 0,
    Activa = 1
}

public class Tarjeta
{
    public int CodTarjeta { get; set; }
    public string NumeroTarjeta { get; set; } = string.Empty;
    public int CantidadViajesComprados { get; set; }
    public int CantidadViajesUtilizados { get; set; }
    public EstadoTarjeta Estado { get; set; } = EstadoTarjeta.Activa;
}