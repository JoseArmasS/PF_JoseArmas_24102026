namespace TransMetroUMG.Application;

public sealed class OperacionBusConsulta
{
    public int Codigo { get; set; }
    public int Asignacion { get; set; }
    public string Bus { get; set; } = string.Empty;
    public string Linea { get; set; } = string.Empty;
    public string EstacionSalida { get; set; } = string.Empty;
    public string Operador { get; set; } = string.Empty;
    public DateTime Registro { get; set; }
    public DateTime InicioEspera { get; set; }
    public DateTime? Salida { get; set; }
    public int Capacidad { get; set; }
    public short? SentidoInicial { get; set; }
    public int? UltimosPasajeros { get; set; }
    public DateTime? UltimaMedicion { get; set; }
    public string? UltimaEstacion { get; set; }
}

public sealed record EstacionSalidaOpcion(int Codigo, string Nombre, int Orden);
public sealed record EstacionOperacionOpcion(int Asignacion, int Codigo, string Nombre, int Orden);
public enum ResultadoIniciarOperacion { Guardado, AsignacionNoValida, EstacionFueraDeLinea, OperadorNoValido }

public interface IRegistroOperacionBusService
{
    Task<IReadOnlyList<EstacionOperacionOpcion>> ObtenerRecorridosAsync(int[] asignaciones, CancellationToken cancellationToken);
    Task<IReadOnlyList<OperacionBusConsulta>> ConsultarAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OperacionBusConsulta>> ConsultarEnEstacionAsync(int estacion, CancellationToken cancellationToken);
    Task<IReadOnlyList<EstacionSalidaOpcion>> ObtenerRecorridoAsync(int asignacion, CancellationToken cancellationToken);
    Task<ResultadoIniciarOperacion> IniciarAsync(int asignacion, int estacion, int operador, CancellationToken cancellationToken);
}
