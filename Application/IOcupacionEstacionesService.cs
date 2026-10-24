namespace TransMetroUMG.Application;

public sealed class EstacionOcupacionConsulta
{
    public int Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Municipalidad { get; set; } = string.Empty;
    public int? Capacidad { get; set; }
}

public sealed class MedicionEstacionConsulta
{
    public long Codigo { get; set; }
    public DateTime Fecha { get; set; }
    public int Pasajeros { get; set; }
    public int Capacidad { get; set; }
    public string Operador { get; set; } = string.Empty;
    public decimal Porcentaje => Domain.OcupacionEstacion.CalcularPorcentaje(Pasajeros, Capacidad);
    public bool RequiereApoyo => Domain.OcupacionEstacion.RequiereApoyo(Pasajeros, Capacidad);
}

public enum ResultadoRegistrarOcupacion { Guardado, EstacionNoEncontrada, SinCapacidad, OperadorNoValido }

public interface IOcupacionEstacionesService
{
    Task<IReadOnlyList<EstacionOcupacionConsulta>> ConsultarEstacionesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MedicionEstacionConsulta>> ConsultarHistorialAsync(int estacion, int pagina, CancellationToken cancellationToken);
    Task<ResultadoRegistrarOcupacion> RegistrarAsync(int estacion, int pasajeros, int operador, CancellationToken cancellationToken);
}
