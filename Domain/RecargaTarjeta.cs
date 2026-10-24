namespace TransMetroUMG.Domain;

public class RecargaTarjeta
{
    public int CodRecargaTarjeta { get; set; }
    public int CodTarjeta { get; set; }
    public string Nit { get; set; } = "CF";
    public string NombreCliente { get; set; } = "";
    public string? DireccionCliente { get; set; }
    public int CantidadViajesComprados { get; set; }
    public decimal MontoTotal { get; set; }
    public DateTime FechaHoraRecarga { get; set; }
}