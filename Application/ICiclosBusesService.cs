using TransMetroUMG.Domain;

namespace TransMetroUMG.Application;

public sealed record ParadaCicloConsulta(ParadaOperacionBus Datos, string Estacion, string? OperadorLlegada, string? OperadorSalida);

public sealed class CicloBusConsulta
{
    public OperacionBus Operacion { get; set; } = new();
    public string Bus { get; set; } = string.Empty;
    public string Linea { get; set; } = string.Empty;
    public string Piloto { get; set; } = string.Empty;
    public IReadOnlyList<ParadaCicloConsulta> Paradas { get; set; } = [];
    public ParadaCicloConsulta? Actual => Paradas.LastOrDefault(x => x.Datos.FechaHoraLlegada.HasValue);
    public ParadaCicloConsulta? Siguiente => Actual is { } actual
        ? Paradas.SingleOrDefault(x => x.Datos.Secuencia == actual.Datos.Secuencia + 1) : null;
    public bool EnTrayecto => Actual?.Datos.FechaHoraSalida.HasValue == true && !Operacion.FechaHoraCierre.HasValue;
    public string Situacion => Operacion.FechaHoraCierre.HasValue ? "Ciclo cerrado"
        : EnTrayecto ? "En trayecto" : Siguiente is null ? "Ciclo completo" : "En estación";
    public ParadaCicloConsulta? VisitaAtencion => EnTrayecto ? Siguiente : Actual;
}

public sealed record ResultadoCiclo(bool Guardado, string Mensaje, int? Operacion = null);

public interface ICiclosBusesService
{
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerLineasAsync(int estacion, CancellationToken cancellationToken);
    Task<IReadOnlyList<AsignacionOperacionConsulta>> ObtenerAsignacionesAsync(int estacion, int linea, CancellationToken cancellationToken);
    Task<IReadOnlyList<EstacionSalidaOpcion>> ObtenerRecorridoAsync(int linea, CancellationToken cancellationToken);
    Task<IReadOnlyList<CicloBusConsulta>> ConsultarActivosAsync(int estacion, CancellationToken cancellationToken);
    Task<CicloBusConsulta?> ObtenerAsync(int operacion, CancellationToken cancellationToken);
    Task<ResultadoCiclo> IniciarAsync(int estacion, int linea, int asignacion, short sentido, int operador, CancellationToken cancellationToken);
    Task<ResultadoCiclo> ConfirmarAsync(int operacion, long visita, int estacion, AccionCiclo accion, int operador, CancellationToken cancellationToken);
    Task<ResultadoCiclo> MedirAsync(int operacion, long visita, int estacion, int pasajeros, int operador, CancellationToken cancellationToken);
}
