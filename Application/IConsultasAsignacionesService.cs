namespace TransMetroUMG.Application;

public sealed record FiltroPilotosBuses(int? Municipalidad, int? Piloto, int? Bus, DateOnly? Fecha);
public sealed record FiltroEstacionesParqueos(int? Municipalidad, int? Estacion, int? Parqueo, int? Bus);

public sealed class PilotoBusConsulta
{
    public int CodigoPiloto { get; set; }
    public int CodigoMunicipalidad { get; set; }
    public string Municipalidad { get; set; } = "";
    public string Piloto { get; set; } = "";
    public string Licencia { get; set; } = "";
    public string? Telefono { get; set; }
    public int? CodigoAsignacion { get; set; }
    public DateOnly? Fecha { get; set; }
    public int? CodigoBus { get; set; }
    public string? Bus { get; set; }
    public string? Placa { get; set; }
    public int? Capacidad { get; set; }
    public string? Linea { get; set; }
}

public sealed class EstacionParqueoBusConsulta
{
    public int CodigoEstacion { get; set; }
    public int CodigoMunicipalidad { get; set; }
    public string Municipalidad { get; set; } = "";
    public string Estacion { get; set; } = "";
    public int? CodigoParqueo { get; set; }
    public string? Parqueo { get; set; }
    public int? CapacidadParqueo { get; set; }
    public int? CodigoBus { get; set; }
    public string? Bus { get; set; }
    public string? Placa { get; set; }
    public int? CapacidadBus { get; set; }
}

public sealed record PaginaConsulta<T>(IReadOnlyList<T> Filas, int Total, int Pagina)
{
    public const int Tamano = 50;
    public int TotalPaginas => (int)Math.Max(1, (Total + (long)Tamano - 1) / Tamano);
}

public interface IConsultasAsignacionesService
{
    Task<PaginaConsulta<PilotoBusConsulta>> ConsultarPilotosAsync(FiltroPilotosBuses filtro, int pagina, CancellationToken ct);
    Task<PaginaConsulta<EstacionParqueoBusConsulta>> ConsultarParqueosAsync(FiltroEstacionesParqueos filtro, int pagina, CancellationToken ct);
}
