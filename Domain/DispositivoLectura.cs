namespace TransMetroUMG.Domain;

public enum EstadoDispositivoLectura : byte
{
    Inactivo = 0,
    Activo = 1
}

public static class TiposLector
{
    public const string Qr = "QR";
    public const string Rfid = "RFID";
}

public class DispositivoLectura
{
    public int CodDispositivoLectura { get; set; }
    public int CodEstacion { get; set; }
    public string TipoLector { get; set; } = string.Empty;

    public EstadoDispositivoLectura Estado { get; set; }
        = EstadoDispositivoLectura.Activo;
}