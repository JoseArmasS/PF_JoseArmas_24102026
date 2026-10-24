namespace TransMetroUMG.Application;

public static class EstadosConsultaBuses
{
    public const string EnEstacion = "En estación";
    public const string EnTrayecto = "En trayecto";
    public const string SinMovimientos = "Sin movimientos registrados";
    public static IReadOnlyList<string> Valores { get; } = [EnEstacion, EnTrayecto, SinMovimientos];
}

public sealed record FiltroConsultaBuses(int? Estacion, int? Linea, int? Bus, int? Piloto, string? Estado, int? Municipalidad = null);

public sealed class BusEnOperacionConsulta
{
    public int CodigoOperacion { get; set; }
    public int CodigoBus { get; set; }
    public int CodigoMunicipalidad { get; set; }
    public string Bus { get; set; } = string.Empty;
    public string Placa { get; set; } = string.Empty;
    public int CodigoLinea { get; set; }
    public string Linea { get; set; } = string.Empty;
    public int CodigoPiloto { get; set; }
    public string Piloto { get; set; } = string.Empty;
    public int CapacidadMaxima { get; set; }
    public int? CodigoEstacion { get; set; }
    public string? Estacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? Pasajeros { get; set; }
    public DateTime? UltimaMedicion { get; set; }
    public string? EstacionMedicion { get; set; }
    public DateTime InicioOperacion { get; set; }
    public decimal? Porcentaje => Pasajeros.HasValue && CapacidadMaxima > 0
        ? Pasajeros.Value * 100m / CapacidadMaxima : null;
}

public sealed record CatalogosConsultaBuses(IReadOnlyList<OpcionCatalogo> Estaciones,
    IReadOnlyList<OpcionCatalogo> Lineas, IReadOnlyList<OpcionCatalogo> Buses, IReadOnlyList<OpcionCatalogo> Pilotos,
    IReadOnlyList<OpcionCatalogo> Municipalidades);

public sealed record ResultadoConsultaBuses(IReadOnlyList<BusEnOperacionConsulta> Filas, int Total, int Pagina)
{
    public const int TamanoPagina = 50;
    public int TotalPaginas => (int)Math.Max(1, (Total + (long)TamanoPagina - 1) / TamanoPagina);
}

public interface IConsultaBusesService
{
    Task<CatalogosConsultaBuses> ObtenerCatalogosAsync(CancellationToken cancellationToken);
    Task<ResultadoConsultaBuses> ConsultarAsync(FiltroConsultaBuses filtro, int pagina, CancellationToken cancellationToken);
}
