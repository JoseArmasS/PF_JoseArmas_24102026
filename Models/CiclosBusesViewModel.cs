using System.ComponentModel.DataAnnotations;
using TransMetroUMG.Application;
using TransMetroUMG.Domain;

namespace TransMetroUMG.Models;

public sealed class CiclosBusesViewModel
{
    public DateTime? HoraReferencia { get; set; }
    public IReadOnlyDictionary<long, EstadoEsperaBus> Esperas { get; set; } = new Dictionary<long, EstadoEsperaBus>();
    public int? Estacion { get; set; }
    public int? Linea { get; set; }
    public int? Asignacion { get; set; }
    public IReadOnlyList<EstacionOcupacionConsulta> Estaciones { get; set; } = [];
    public EstacionOcupacionConsulta? EstacionSeleccionada { get; set; }
    public MedicionEstacionConsulta? UltimaEstacion { get; set; }
    public IReadOnlyList<OpcionCatalogo> Lineas { get; set; } = [];
    public IReadOnlyList<AsignacionOperacionConsulta> Asignaciones { get; set; } = [];
    public IReadOnlyList<EstacionSalidaOpcion> Recorrido { get; set; } = [];
    public IReadOnlyList<CicloBusConsulta> Ciclos { get; set; } = [];
    public CicloBusConsulta? Seleccionado { get; set; }
    public IReadOnlyList<MedicionBusConsulta> Mediciones { get; set; } = [];
    public bool ErrorCarga { get; set; }
}

public sealed class InicioCicloFormulario
{
    [Required, Range(1, int.MaxValue)] public int? Estacion { get; set; }
    [Required, Range(1, int.MaxValue)] public int? Linea { get; set; }
    [Required(ErrorMessage = "Seleccione un bus y su asignación."), Range(1, int.MaxValue)] public int? Asignacion { get; set; }
    [Required(ErrorMessage = "Seleccione el sentido inicial.")] public short? Sentido { get; set; }
}

public class MovimientoCicloFormulario
{
    [Range(1, int.MaxValue)] public int Operacion { get; set; }
    [Range(1, long.MaxValue)] public long Visita { get; set; }
    [Range(1, int.MaxValue)] public int Estacion { get; set; }
}

public sealed class MedicionCicloFormulario : MovimientoCicloFormulario
{
    [Required(ErrorMessage = "Ingrese la cantidad de pasajeros.")]
    [Range(0, int.MaxValue, ErrorMessage = "Ingrese un valor mayor a cero.")]
    public int? Pasajeros { get; set; }
}
