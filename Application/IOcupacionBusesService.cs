namespace TransMetroUMG.Application;

public sealed class MedicionBusConsulta
{
    public long Codigo { get; set; }
    public DateTime Fecha { get; set; }
    public string Estacion { get; set; } = string.Empty;
    public int Pasajeros { get; set; }
    public string Operador { get; set; } = string.Empty;
}

public enum ResultadoMedicionBus { Guardado, OperacionNoValida, EstacionFueraDeLinea, OperadorNoValido, CapacidadExcedida, VisitaNoValida }

public interface IOcupacionBusesService
{
    Task<OperacionBusConsulta?> ObtenerOperacionAsync(int operacion, CancellationToken cancellationToken);
    Task<IReadOnlyList<MedicionBusConsulta>> ConsultarAsync(int operacion, int pagina, CancellationToken cancellationToken);
    Task<ResultadoMedicionBus> RegistrarAsync(int operacion, int estacion, int pasajeros, int operador, CancellationToken cancellationToken);
}
