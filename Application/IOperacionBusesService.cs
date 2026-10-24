namespace TransMetroUMG.Application;

public sealed class AsignacionOperacionConsulta
{
    public int Codigo { get; set; }
    public int CodigoLinea { get; set; }
    public DateOnly Fecha { get; set; }
    public string Municipalidad { get; set; } = string.Empty;
    public string NumeroBus { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public int Capacidad { get; set; }
    public byte EstadoBus { get; set; }
    public string Piloto { get; set; } = string.Empty;
    public string Linea { get; set; } = string.Empty;
}

public interface IOperacionBusesService
{
    Task<IReadOnlyList<AsignacionOperacionConsulta>> ConsultarAsync(
        DateOnly? fecha, CancellationToken cancellationToken, int? codigoLinea = null);
    Task<IReadOnlyList<OpcionCatalogo>> ObtenerLineasAsync(CancellationToken cancellationToken);
}
